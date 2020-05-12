import Metal
import QuartzCore.CAMetalLayer
import simd
import CoreEngineCommonInterop

class Shader {
    let shaderId: UInt
    let vertexShaderFunction: MTLFunction?
    let pixelShaderFunction: MTLFunction?
    let computeShaderFunction: MTLFunction?
    let argumentEncoder: MTLArgumentEncoder?
    var argumentBuffers: [MTLBuffer]
    let argumentBuffersMaxCount = 1000
    var argumentBufferCurrentIndex = 0
    var currentArgumentBuffer: MTLBuffer?

    init(_ shaderId: UInt, _ device: MTLDevice, _ vertexShaderFunction: MTLFunction?, _ pixelShaderFunction: MTLFunction?, _ computeShaderFunction: MTLFunction?, _ argumentEncoder: MTLArgumentEncoder?, _ debugName: String?) {
        self.shaderId = shaderId
        self.vertexShaderFunction = vertexShaderFunction
        self.pixelShaderFunction = pixelShaderFunction
        self.computeShaderFunction = computeShaderFunction
        self.argumentEncoder = argumentEncoder

        self.argumentBuffers = []
        self.currentArgumentBuffer = nil

        if (argumentEncoder != nil) {
            // TODO: Use another allocation strategie
            for i in 0..<argumentBuffersMaxCount {
                let argumentBuffer = device.makeBuffer(length: argumentEncoder!.encodedLength)!
                argumentBuffer.label = (debugName != nil) ? "\(debugName!)Buffer\(i)" : "ShaderBuffer\(i)"
                self.argumentBuffers.append(argumentBuffer)
            }

            self.currentArgumentBuffer = argumentBuffers[0]
            argumentEncoder!.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
        }
    }

    func setupArgumentBuffer() {
        if (argumentEncoder != nil) {
            argumentBufferCurrentIndex += 1

            if (argumentBufferCurrentIndex == argumentBuffersMaxCount) {
                argumentBufferCurrentIndex = 0
            }

            self.currentArgumentBuffer = argumentBuffers[argumentBufferCurrentIndex]
            argumentEncoder!.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
        }
    }
}

public class MetalGraphicsService: GraphicsServiceProtocol {
    let device: MTLDevice
    let metalLayer: CAMetalLayer
    var currentMetalDrawable: CAMetalDrawable?
    var currentIndexBuffer: MTLBuffer!
    var currentShader: Shader? = nil
    var currentComputePipelineState: MTLComputePipelineState? = nil
    var currentFrameNumber = 0
    let useHdrRenderTarget: Bool = false

    var renderWidth: Int
    var renderHeight: Int

    var frameSemaphore: DispatchSemaphore

    var commandQueue: MTLCommandQueue!
    var globalHeap: MTLHeap!
    var staticHeap: MTLHeap!

    var depthCompareEqualState: MTLDepthStencilState!
    var depthCompareLessState: MTLDepthStencilState!
    var depthWriteOperationState: MTLDepthStencilState!
    var depthNoneOperationState: MTLDepthStencilState!

    var shaders: [UInt: Shader]
    var renderPipelineStates: [UInt: MTLRenderPipelineState]
    var computePipelineStates: [UInt: MTLComputePipelineState]

    var commandListFences: [UInt: MTLFence]

    var commandBuffers: [UInt: MTLCommandBuffer]
    var commandBuffersStatus: [UInt: GraphicsCommandBufferStatus]

    var copyCommandEncoders: [UInt: MTLBlitCommandEncoder]
    var computeCommandEncoders: [UInt: MTLComputeCommandEncoder]
    var renderCommandEncoders: [UInt: MTLRenderCommandEncoder]

    var graphicsBuffers: [UInt: MTLBuffer]
    var cpuGraphicsBuffers: [UInt: MTLBuffer]
    var readCpuGraphicsBuffers: [UInt: MTLBuffer]
    var textures: [UInt: MTLTexture]
    var indirectCommandBuffers: [UInt: MTLIndirectCommandBuffer]

    let commandBufferStatusConcurrentQueue: DispatchQueue

    public init(view: MetalView, renderWidth: Int, renderHeight: Int) {
        let defaultDevice = MTLCreateSystemDefaultDevice()!
        print(defaultDevice.name)
        print(renderWidth)

        self.device = defaultDevice
        self.renderWidth = renderWidth
        self.renderHeight = renderHeight

        // Create color metal layer
        self.metalLayer = view.metalLayer
        self.metalLayer.device = device

        self.metalLayer.pixelFormat = (self.useHdrRenderTarget) ? .rgba16Float : .bgra8Unorm_srgb
        self.metalLayer.framebufferOnly = true
        self.metalLayer.allowsNextDrawableTimeout = true
        self.metalLayer.displaySyncEnabled = true
        self.metalLayer.maximumDrawableCount = 3
        self.metalLayer.drawableSize = CGSize(width: renderWidth, height: renderHeight)

        if (self.useHdrRenderTarget) {
            self.metalLayer.wantsExtendedDynamicRangeContent = true
            let name =  CGColorSpace.extendedLinearDisplayP3
            let colorSpace = CGColorSpace(name: name)
            self.metalLayer.colorspace = colorSpace
        }

        self.shaders = [:]
        self.renderPipelineStates = [:]
        self.computePipelineStates = [:]
        self.commandListFences = [:]
        self.commandBuffers = [:]
        self.copyCommandEncoders = [:]
        self.computeCommandEncoders = [:]
        self.renderCommandEncoders = [:]
        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]
        self.readCpuGraphicsBuffers = [:]
        self.textures = [:]
        self.indirectCommandBuffers = [:]
        self.commandBuffersStatus = [:]

        self.frameSemaphore = DispatchSemaphore.init(value: 1);

        var depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .equal
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthCompareEqualState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .lessEqual
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthCompareLessState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthWriteOperationState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .always
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthNoneOperationState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()

        // TODO: Implement aliasing for render targets
        var heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .private
        heapDescriptor.type = .automatic // TODO: Switch to placement mode for manual memory management
        heapDescriptor.size = 1024 * 1024 * 512 // Allocate 512MB for now
        self.globalHeap = self.device.makeHeap(descriptor: heapDescriptor)!

        heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .private
        heapDescriptor.type = .automatic // TODO: Switch to placement mode for manual memory management
        heapDescriptor.size = 1024 * 1024 * 2048 // Allocate 512MB for now
        heapDescriptor.cpuCacheMode = .writeCombined
        self.staticHeap = self.device.makeHeap(descriptor: heapDescriptor)!

        self.commandBufferStatusConcurrentQueue = DispatchQueue(label: "MetalGraphicsService.resetCommandBuffer")
    }

    public func getRenderSize() -> Vector2 {
        return Vector2(X: Float(self.renderWidth), Y: Float(self.renderHeight))
    }

    public func getGraphicsAdapterName() -> String {
        return self.metalLayer.device!.name + " (Metal 3)"
    }

    public func changeRenderSize(renderWidth: Int, renderHeight: Int) {
        self.renderWidth = renderWidth
        self.renderHeight = renderHeight
        
        self.metalLayer.drawableSize = CGSize(width: renderWidth, height: renderHeight)
    }

    public func createGraphicsBuffer(_ graphicsBufferId: UInt, _ length: Int, _ isWriteOnly: Bool, _ label: String) -> Bool {
        // TODO: Page Align the length to avoid the copy of the buffer later
        // TODO: Check for errors

        if (isWriteOnly) {
            // Create a the metal buffer on the CPU
            let cpuBuffer = self.device.makeBuffer(length: length, options: [.storageModeShared, .cpuCacheModeWriteCombined, .hazardTrackingModeUntracked])!
            cpuBuffer.label = "\(label)Cpu"
            self.cpuGraphicsBuffers[graphicsBufferId] = cpuBuffer
        } else {
            // Create a the metal buffer on the CPU
            let cpuBuffer = self.device.makeBuffer(length: length, options: [.storageModeShared, .hazardTrackingModeUntracked])!
            cpuBuffer.label = "\(label)Cpu"
            self.cpuGraphicsBuffers[graphicsBufferId] = cpuBuffer

            let readCpuBuffer = self.device.makeBuffer(length: length, options: [.storageModeShared, .hazardTrackingModeUntracked])!
            readCpuBuffer.label = "\(label)ReadCpu"
            self.readCpuGraphicsBuffers[graphicsBufferId] = readCpuBuffer
        }

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: length, options: [.storageModePrivate, .hazardTrackingModeUntracked])!
        gpuBuffer.label = "\(label)Gpu"
        self.graphicsBuffers[graphicsBufferId] = gpuBuffer

        return true
    }

    private func convertTextureFormat(_ textureFormat: GraphicsTextureFormat) -> MTLPixelFormat {
        if (textureFormat == Bgra8UnormSrgb) {
            return .bgra8Unorm_srgb
        } else if (textureFormat == Depth32Float) {
            return .depth32Float
        } else if (textureFormat == Rgba16Float) {
            return .rgba16Float
        } else if (textureFormat == R16Float) {
            return .r16Float
        } else if (textureFormat == BC1Srgb) {
            return .bc1_rgba_srgb
        } else if (textureFormat == BC2Srgb) {
            return .bc2_rgba_srgb
        } else if (textureFormat == BC3Srgb) {
            return .bc3_rgba_srgb
        } else if (textureFormat == BC4) {
            return .bc4_rUnorm
        } else if (textureFormat == BC5) {
            return .bc5_rgUnorm
        } else if (textureFormat == BC6) {
            return .bc6H_rgbuFloat
        } else if (textureFormat == BC7Srgb) {
            return .bc7_rgbaUnorm_srgb
        } else if (textureFormat == Rgba32Float) {
            return .rgba32Float
        } else if (textureFormat == Rgba16Unorm) {
            return .rgba16Unorm
        }
        
        return .rgba8Unorm_srgb
    }

    public func createTexture(_ textureId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int, _ isRenderTarget: Bool, _ label: String) -> Bool {
        // TODO: Check for errors
        let descriptor = MTLTextureDescriptor()

        descriptor.width = width
        descriptor.height = height
        descriptor.depth = 1
        descriptor.mipmapLevelCount = mipLevels
        descriptor.arrayLength = 1
        descriptor.sampleCount = multisampleCount
        descriptor.storageMode = .private
        descriptor.hazardTrackingMode = .untracked
        descriptor.pixelFormat = convertTextureFormat(textureFormat)

        if (isRenderTarget) {
            descriptor.usage = [.renderTarget, .shaderRead, .shaderWrite]
        } else {
            descriptor.cpuCacheMode = .writeCombined
        }

        if (multisampleCount > 1) {
            descriptor.textureType = .type2DMultisample
        } else if (faceCount > 1) {
            descriptor.textureType = .typeCube
        } else {
            descriptor.textureType = .type2D
        }

        if (isRenderTarget) {
            guard let gpuTexture = self.device.makeTexture(descriptor: descriptor) else {
                print("createTexture: Creation failed.")
                return false
            }

            gpuTexture.label = label
            self.textures[textureId] = gpuTexture
        } else {
            guard let gpuTexture = self.staticHeap.makeTexture(descriptor: descriptor) else {
                print("createTexture: Creation failed.")
                return false
            }

            gpuTexture.label = label
            self.textures[textureId] = gpuTexture
        }

        return true
    }

    public func deleteTexture(_ textureId: UInt) {
        self.textures[textureId] = nil
    }

    public func createIndirectCommandBuffer(_ indirectCommandBufferId: UInt, _ maxCommandCount: Int, _ label: String) -> Bool {
        let indirectCommandBufferDescriptor = MTLIndirectCommandBufferDescriptor()
        
        indirectCommandBufferDescriptor.commandTypes = [.drawIndexed]
        indirectCommandBufferDescriptor.inheritBuffers = false
        indirectCommandBufferDescriptor.maxVertexBufferBindCount = 5
        indirectCommandBufferDescriptor.maxFragmentBufferBindCount = 5
        indirectCommandBufferDescriptor.inheritPipelineState = true

        let indirectCommandBuffer = self.device.makeIndirectCommandBuffer(descriptor: indirectCommandBufferDescriptor,
                                                                          maxCommandCount: maxCommandCount,
                                                                          options: .storageModePrivate)!

        indirectCommandBuffer.label = label

        self.indirectCommandBuffers[indirectCommandBufferId] = indirectCommandBuffer

        return true
    }

    public func createShader(_ shaderId: UInt, _ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int, _ label: String) -> Bool {
        let dispatchData = DispatchData(bytes: UnsafeRawBufferPointer(start: shaderByteCode, count: shaderByteCodeLength))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        if (computeShaderFunction == nil) {
            let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")!
            let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

            // TODO: Remove that hack
            if (label != "RenderMeshInstanceDepthShader" && label != "RenderMeshInstanceDepthMomentShader" && label != "RenderMeshInstanceShader" && label != "RenderMeshInstanceTransparentShader" && label != "RenderMeshInstanceTransparentDepthShader") {
                let argumentEncoder = vertexFunction.makeArgumentEncoder(bufferIndex: 0)
                self.shaders[shaderId] = Shader(shaderId, self.device, vertexFunction, fragmentFunction, nil, argumentEncoder, label)
            } else {
                self.shaders[shaderId] = Shader(shaderId, self.device, vertexFunction, fragmentFunction, nil, nil, label)
            }

            return true
        } else {
            let computeFunction = defaultLibrary.makeFunction(name: computeShaderFunction!)!

            let argumentEncoder = computeFunction.makeArgumentEncoder(bufferIndex: 0)
            self.shaders[shaderId] = Shader(shaderId, self.device, nil, nil, computeFunction, argumentEncoder, label)
            return true
        }
    }

    public func deleteShader(_ shaderId: UInt) {
        self.shaders[shaderId] = nil

        if (self.currentShader != nil && self.currentShader!.shaderId == shaderId) {
            self.currentShader = nil
        }
    }

    private func initBlendState(_ colorAttachmentDescriptor: MTLRenderPipelineColorAttachmentDescriptor, _ blendOperation: GraphicsBlendOperation) {
        if (blendOperation == AlphaBlending) {
            colorAttachmentDescriptor.isBlendingEnabled = true
            colorAttachmentDescriptor.rgbBlendOperation = .add
            colorAttachmentDescriptor.alphaBlendOperation = .add
            colorAttachmentDescriptor.sourceRGBBlendFactor = .sourceAlpha
            colorAttachmentDescriptor.sourceAlphaBlendFactor = .sourceAlpha;
            colorAttachmentDescriptor.destinationRGBBlendFactor = .oneMinusSourceAlpha
            colorAttachmentDescriptor.destinationAlphaBlendFactor = .oneMinusSourceAlpha
        } else if (blendOperation == AddOneOne) {
            colorAttachmentDescriptor.isBlendingEnabled = true
            colorAttachmentDescriptor.rgbBlendOperation = .add
            colorAttachmentDescriptor.alphaBlendOperation = .add
            colorAttachmentDescriptor.sourceRGBBlendFactor = .one
            colorAttachmentDescriptor.sourceAlphaBlendFactor = .one;
            colorAttachmentDescriptor.destinationRGBBlendFactor = .one
            colorAttachmentDescriptor.destinationAlphaBlendFactor = .one
        } else if (blendOperation == AddOneMinusSourceColor) {
            colorAttachmentDescriptor.isBlendingEnabled = true
            colorAttachmentDescriptor.rgbBlendOperation = .add
            colorAttachmentDescriptor.sourceRGBBlendFactor = .zero
            colorAttachmentDescriptor.destinationRGBBlendFactor = .oneMinusSourceColor
        }
    }

    public func createPipelineState(_ pipelineStateId: UInt, _ shaderId: UInt, _ renderPassDescriptor: GraphicsRenderPassDescriptor, _ label: String) -> Bool {
        guard let shader = self.shaders[shaderId] else {
            return false
        }

        if (renderPassDescriptor.IsRenderShader == 1) {
            let pipelineStateDescriptor = MTLRenderPipelineDescriptor()
            pipelineStateDescriptor.label = label

            pipelineStateDescriptor.vertexFunction = shader.vertexShaderFunction!

            if (shader.pixelShaderFunction != nil)
            {
                pipelineStateDescriptor.fragmentFunction = shader.pixelShaderFunction!
            }

            pipelineStateDescriptor.supportIndirectCommandBuffers = true
            pipelineStateDescriptor.sampleCount = (renderPassDescriptor.MultiSampleCount.HasValue == 1) ? Int(renderPassDescriptor.MultiSampleCount.Value) : 1

            // TODO: Use the correct render target format
            if (renderPassDescriptor.RenderTarget1TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[0].pixelFormat = convertTextureFormat(renderPassDescriptor.RenderTarget1TextureFormat.Value)
            } else if(renderPassDescriptor.DepthBufferOperation == DepthNone) {
                pipelineStateDescriptor.colorAttachments[0].pixelFormat = (self.useHdrRenderTarget) ? .rgba16Float : .bgra8Unorm_srgb
            }

            if (renderPassDescriptor.RenderTarget2TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[1].pixelFormat = convertTextureFormat(renderPassDescriptor.RenderTarget2TextureFormat.Value)
            }

            if (renderPassDescriptor.RenderTarget3TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[2].pixelFormat = convertTextureFormat(renderPassDescriptor.RenderTarget3TextureFormat.Value)
            }

            if (renderPassDescriptor.RenderTarget4TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[3].pixelFormat = convertTextureFormat(renderPassDescriptor.RenderTarget4TextureFormat.Value)
            }

            if (renderPassDescriptor.DepthTextureId.HasValue == 1) {
                pipelineStateDescriptor.depthAttachmentPixelFormat = .depth32Float
            } 

            if (renderPassDescriptor.RenderTarget1BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[0]!, renderPassDescriptor.RenderTarget1BlendOperation.Value)
            }

            if (renderPassDescriptor.RenderTarget2BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[1]!, renderPassDescriptor.RenderTarget2BlendOperation.Value)
            }

            if (renderPassDescriptor.RenderTarget3BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[2]!, renderPassDescriptor.RenderTarget3BlendOperation.Value)
            }

            if (renderPassDescriptor.RenderTarget4BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[3]!, renderPassDescriptor.RenderTarget4BlendOperation.Value)
            }

            do {
                let pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
                self.renderPipelineStates[pipelineStateId] = pipelineState
                return true
            } catch {
                print("Failed to created pipeline state, \(error)")
            }
        } else {
            do {
                let pipelineState = try self.device.makeComputePipelineState(function: shader.computeShaderFunction!)
                self.computePipelineStates[pipelineStateId] = pipelineState
                return true
            }
            catch {
                print("Failed to created pipeline state, \(error)")
            }
        }

        return false
    }

    public func deletePipelineState(_ pipelineStateId: UInt) {
        self.renderPipelineStates[pipelineStateId] = nil
        self.computePipelineStates[pipelineStateId] = nil
    }

    public func createCommandBuffer(_ commandBufferId: UInt, _ label: String) -> Bool {
        return true
    }

    public func deleteCommandBuffer(_ commandBufferId: UInt) {
        self.commandBuffersStatus[commandBufferId] = nil
    }

    public func resetCommandBuffer(_ commandBufferId: UInt) {
        guard let commandBuffer = self.commandQueue.makeCommandBuffer() else {
            print("ERROR creating command buffer.")
            return
        }

        var status = GraphicsCommandBufferStatus()
        status.State = Created

        commandBufferStatusConcurrentQueue.sync() {
            self.commandBuffersStatus[commandBufferId] = status
        }

        commandBuffer.label = "CommandBuffer\(self.currentFrameNumber)_"
        self.commandBuffers[commandBufferId] = commandBuffer

        let localCommandBufferId = commandBufferId

        commandBuffer.addCompletedHandler { cb in
            self.commandBufferCompleted(cb, localCommandBufferId)
        }
    }

    public func executeCommandBuffer(_ commandBufferId: UInt) {
        if (self.commandBuffers[commandBufferId] != nil) {
            let commandBuffer = self.commandBuffers[commandBufferId]!
            commandBuffer.commit()

            self.commandBuffers[commandBufferId] = nil
        }

        guard var commandBufferStatus = self.commandBuffersStatus[commandBufferId] else {
            return
        }

        commandBufferStatus.State = Committed
        
        commandBufferStatusConcurrentQueue.sync() {
            self.commandBuffersStatus[commandBufferId] = commandBufferStatus
        }
    }

    public func getCommandBufferStatus(_ commandBufferId: UInt) -> NullableGraphicsCommandBufferStatus {
        guard let status = self.commandBuffersStatus[commandBufferId] else {
            return NullableGraphicsCommandBufferStatus(HasValue: 0, Value: GraphicsCommandBufferStatus())
        }

        return NullableGraphicsCommandBufferStatus(HasValue: 1, Value: status)
    }

    public func createCopyCommandList(_ commandListId: UInt, _ commandBufferId: UInt, _ label: String) -> Bool {
        guard let commandBuffer = self.commandBuffers[commandBufferId] else {
            print("createCopyCommandList: Command buffer is nil.")
            return false
        }

        guard let copyCommandEncoder = commandBuffer.makeBlitCommandEncoder() else {
            print("ERROR creating copy command encoder.")
            return false
        }

        copyCommandEncoder.label = label
        self.copyCommandEncoders[commandListId] = copyCommandEncoder
        self.commandListFences[commandListId] = self.device.makeFence()

        return true
    }

    public func commitCopyCommandList(_ commandListId: UInt) {
        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("executeCopyCommandList: Copy command encoder is nil.")
            return
        }

        guard let fence = self.commandListFences[commandListId] else {
            print("executeCopyCommandList: Fence was not found")
            return
        }

        copyCommandEncoder.updateFence(fence)
        copyCommandEncoder.endEncoding()

        self.copyCommandEncoders[commandListId] = nil
    }

    public func uploadDataToGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ length: Int) {
        guard let gpuBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("ERROR: GPU graphics buffer was not found")
            return
        }

        guard let cpuBuffer = self.cpuGraphicsBuffers[graphicsBufferId] else {
            print("ERROR: CPU graphics buffer was not found")
            return
        }

        // TODO: Try to avoid the copy
        cpuBuffer.contents().copyMemory(from: data.assumingMemoryBound(to: UInt8.self), byteCount: (length * MemoryLayout<UInt8>.stride))

        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("uploadDataToGraphicsBuffer: Copy command encoder is nil.")
            return
        }

        // TODO: Add parameters to be able to update partially the buffer
        copyCommandEncoder.copy(from: cpuBuffer, sourceOffset: 0, to: gpuBuffer, destinationOffset: 0, size: length)
    }

    public func copyGraphicsBufferDataToCpu(_ commandListId: UInt, _ graphicsBufferId: UInt, _ length: Int) {
        guard let gpuBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("ERROR: GPU graphics buffer was not found")
            return
        }

        guard let cpuBuffer = self.readCpuGraphicsBuffers[graphicsBufferId] else {
            print("ERROR: CPU graphics buffer was not found")
            return
        }

        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("copyGraphicsBufferDataToCpu: Copy command encoder is nil.")
            return
        }

        // TODO: Add parameters to be able to update partially the buffer
        copyCommandEncoder.copy(from: gpuBuffer, sourceOffset: 0, to: cpuBuffer, destinationOffset: 0, size: length)
    }

    public func readGraphicsBufferData(_ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int) {
        guard let cpuBuffer = self.readCpuGraphicsBuffers[graphicsBufferId] else {
            print("ERROR: CPU graphics buffer was not found")
            return
        }

        // Dump
        // let urbp = UnsafeRawBufferPointer(start: cpuBuffer.contents(), count: dataLength)
        // print(urbp.map{String(format: "%02X", $0)}.joined(separator: " "))

        // TODO: Try to avoid the copy
        data.copyMemory(from: cpuBuffer.contents().assumingMemoryBound(to: UInt8.self), byteCount: (dataLength * MemoryLayout<UInt8>.stride))
    }

    public func uploadDataToTexture(_ commandListId: UInt, _ textureId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int, _ data: UnsafeMutableRawPointer, _ length: Int) {
        guard let gpuTexture = self.textures[textureId] else {
            print("ERROR: GPU texture was not found")
            return
        }

        // Create a the metal buffer on the CPU
        // TODO: Create a pool of cpu buffers
        let cpuTexture = self.device.makeBuffer(length: length, options: .cpuCacheModeWriteCombined)!
        cpuTexture.label = "Texture - CPU buffer"

        // TODO: Try to avoid the copy
        cpuTexture.contents().copyMemory(from: data.assumingMemoryBound(to: UInt8.self), byteCount: (length * MemoryLayout<UInt8>.stride))

        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("uploadDataToTexture: Copy command encoder is nil.")
            return
        }

        var sourceBytesPerRow = 4 * width
        var sourceBytesPerImage = 4 * width * height

        if (textureFormat == BC2Srgb || textureFormat == BC3Srgb || textureFormat == BC5 || textureFormat == BC6 || textureFormat == BC7Srgb) {
            sourceBytesPerRow = 16 * Int(ceil(Double(width) / 4.0))
            sourceBytesPerImage = 16 * Int(ceil(Double(width) / 4.0)) * Int(ceil(Double(height) / 4.0))
        } else if (textureFormat == BC1Srgb || textureFormat == BC4) {
            sourceBytesPerRow = 8 * Int(ceil(Double(width) / 4.0))
            sourceBytesPerImage = 8 * Int(ceil(Double(width) / 4.0)) * Int(ceil(Double(height) / 4.0))
        } else if (textureFormat == Rgba16Float) {
            sourceBytesPerRow = 8 * width
            sourceBytesPerImage = 8 * width * height
        } else if (textureFormat == Rgba32Float) {
            sourceBytesPerRow = 16 * width
            sourceBytesPerImage = 16 * width * height
        }

        // TODO: Add parameters to be able to update partially the buffer
        copyCommandEncoder.copy(from: cpuTexture, 
                                sourceOffset: 0, 
                                sourceBytesPerRow: sourceBytesPerRow,
                                sourceBytesPerImage: sourceBytesPerImage,
                                sourceSize: MTLSize(width: width, height: height , depth: 1),
                                to: gpuTexture, 
                                destinationSlice: slice,
                                destinationLevel: mipLevel,
                                destinationOrigin: MTLOrigin(x: 0, y: 0, z: 0))
    }

    public func resetIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int) {
        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("resetIndirectCommandList: Copy command encoder is nil.")
            return
        }

        guard let indirectBuffer = self.indirectCommandBuffers[indirectCommandListId] else {
            print("setShaderIndirectCommandList: Indirect buffer is nil.")
            return
        }

        copyCommandEncoder.resetCommandsInBuffer(indirectBuffer, range: 0..<maxCommandCount)
    }

    public func optimizeIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int) {
        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("resetIndirectCommandList: Copy command encoder is nil.")
            return
        }

        guard let indirectBuffer = self.indirectCommandBuffers[indirectCommandListId] else {
            print("setShaderIndirectCommandList: Indirect buffer is nil.")
            return
        }

        copyCommandEncoder.optimizeIndirectCommandBuffer(indirectBuffer, range: 0..<maxCommandCount)
    }

    public func createComputeCommandList(_ commandListId: UInt, _ commandBufferId: UInt, _ label: String) -> Bool {
        guard let commandBuffer = self.commandBuffers[commandBufferId] else {
            print("createComputeCommandList: Command buffer is nil.")
            return false
        }

        guard let computeCommandEncoder = commandBuffer.makeComputeCommandEncoder() else {
            print("ERROR creating compute command encoder.")
            return false
        }

        computeCommandEncoder.label = label
        self.computeCommandEncoders[commandListId] = computeCommandEncoder

        self.commandListFences[commandListId] = self.device.makeFence()

        computeCommandEncoder.useHeap(self.globalHeap)
        computeCommandEncoder.useHeap(self.staticHeap)

        return true
    }

    public func commitComputeCommandList(_ commandListId: UInt) {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            print("executeComputeCommandList: Compute command encoder is nil.")
            return
        }

        guard let fence = self.commandListFences[commandListId] else {
            print("executeCopyCommandList: Fence was not found")
            return
        }

        computeCommandEncoder.updateFence(fence)
        computeCommandEncoder.endEncoding()
        
        self.computeCommandEncoders[commandListId] = nil
    }

    public func dispatchThreads(_ commandListId: UInt, _ threadCountX: UInt, _ threadCountY: UInt, _ threadCountZ: UInt) -> Vector3 {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            print("dispatchThreads: Compute command encoder is nil.")
            return Vector3(X: Float(0), Y: Float(0), Z: Float(0))
        }

        guard let currentShader = self.currentShader else {
            print("dispatchThreads: Current Shader is nil.")
            return Vector3(X: Float(0), Y: Float(0), Z: Float(0))
        }

        guard let computePipelineState = self.currentComputePipelineState else {
            print("dispatchThreads: Current Pipeline state is nil.")
            return Vector3(X: Float(0), Y: Float(0), Z: Float(0))
        }

        computeCommandEncoder.setBuffer(currentShader.currentArgumentBuffer, offset: 0, index: 0)

        let w = computePipelineState.threadExecutionWidth
        let h = (threadCountY > 1) ? computePipelineState.maxTotalThreadsPerThreadgroup / w : 1
        let threadsPerGroup = MTLSizeMake(w, h, 1)

        computeCommandEncoder.dispatchThreads(MTLSize(width: Int(threadCountX), height: Int(threadCountY), depth: Int(threadCountZ)), threadsPerThreadgroup: threadsPerGroup)
        currentShader.setupArgumentBuffer()

        return Vector3(X: Float(w), Y: Float(h), Z: Float(1))
    }

    public func createRenderCommandList(_ commandListId: UInt, _ commandBufferId: UInt, _ renderDescriptor: GraphicsRenderPassDescriptor, _ label: String) -> Bool {
        guard let commandBuffer = self.commandBuffers[commandBufferId] else {
            print("createRenderCommandList: Command buffer is nil.")
            return false
        }

        // Create render command encoder        
        let renderPassDescriptor = MTLRenderPassDescriptor()
        
        if (renderDescriptor.RenderTarget1ClearColor.HasValue == 1) {
            renderPassDescriptor.colorAttachments[0].loadAction = .clear
            renderPassDescriptor.colorAttachments[0].clearColor = MTLClearColor.init(red: Double(renderDescriptor.RenderTarget1ClearColor.Value.X), green: Double(renderDescriptor.RenderTarget1ClearColor.Value.Y), blue: Double(renderDescriptor.RenderTarget1ClearColor.Value.Z), alpha: Double(renderDescriptor.RenderTarget1ClearColor.Value.W))
        } else {
            renderPassDescriptor.colorAttachments[0].loadAction = .load
        }

        if (renderDescriptor.RenderTarget1TextureId.HasValue == 0 && renderDescriptor.DepthTextureId.HasValue == 0) {
            guard let nextCurrentMetalDrawable = self.metalLayer.nextDrawable() else {
                print("Next drawable timeout")
                return false
            }

            self.currentMetalDrawable = nextCurrentMetalDrawable
            renderPassDescriptor.colorAttachments[0].texture = nextCurrentMetalDrawable.texture
            renderPassDescriptor.colorAttachments[0].storeAction = .dontCare
        } else if (renderDescriptor.RenderTarget1TextureId.HasValue == 1) {
            guard let colorTexture = self.textures[UInt(renderDescriptor.RenderTarget1TextureId.Value)] else {
                print("createRenderCommandList: Render Target 1 Texture is nil.")
                return false
            }

            renderPassDescriptor.colorAttachments[0].texture = colorTexture
            renderPassDescriptor.colorAttachments[0].storeAction = .store
        }

        if (renderDescriptor.RenderTarget2TextureId.HasValue == 1) {
            guard let colorTexture = self.textures[UInt(renderDescriptor.RenderTarget2TextureId.Value)] else {
                print("createRenderCommandList: Render Target 2 is nil.")
                return false
            }

            renderPassDescriptor.colorAttachments[1].texture = colorTexture
            renderPassDescriptor.colorAttachments[1].storeAction = .store

            if (renderDescriptor.RenderTarget2ClearColor.HasValue == 1) {
                renderPassDescriptor.colorAttachments[1].loadAction = .clear
                renderPassDescriptor.colorAttachments[1].clearColor = MTLClearColor.init(red: Double(renderDescriptor.RenderTarget2ClearColor.Value.X), green: Double(renderDescriptor.RenderTarget2ClearColor.Value.Y), blue: Double(renderDescriptor.RenderTarget2ClearColor.Value.Z), alpha: Double(renderDescriptor.RenderTarget2ClearColor.Value.W))
            } else {
                renderPassDescriptor.colorAttachments[1].loadAction = .load
            }
        }

        if (renderDescriptor.RenderTarget3TextureId.HasValue == 1) {
            guard let colorTexture = self.textures[UInt(renderDescriptor.RenderTarget3TextureId.Value)] else {
                print("createRenderCommandList: Render Target 3 is nil.")
                return false
            }

            renderPassDescriptor.colorAttachments[2].texture = colorTexture
            renderPassDescriptor.colorAttachments[2].storeAction = .store

            if (renderDescriptor.RenderTarget3ClearColor.HasValue == 1) {
                renderPassDescriptor.colorAttachments[2].loadAction = .clear
                renderPassDescriptor.colorAttachments[2].clearColor = MTLClearColor.init(red: Double(renderDescriptor.RenderTarget3ClearColor.Value.X), green: Double(renderDescriptor.RenderTarget3ClearColor.Value.Y), blue: Double(renderDescriptor.RenderTarget3ClearColor.Value.Z), alpha: Double(renderDescriptor.RenderTarget3ClearColor.Value.W))
            } else {
                renderPassDescriptor.colorAttachments[2].loadAction = .load
            }
        }

        if (renderDescriptor.RenderTarget4TextureId.HasValue == 1) {
            guard let colorTexture = self.textures[UInt(renderDescriptor.RenderTarget4TextureId.Value)] else {
                print("createRenderCommandList: Render Target 4 is nil.")
                return false
            }

            renderPassDescriptor.colorAttachments[3].texture = colorTexture
            renderPassDescriptor.colorAttachments[3].storeAction = .store

            if (renderDescriptor.RenderTarget4ClearColor.HasValue == 1) {
                renderPassDescriptor.colorAttachments[3].loadAction = .clear
                renderPassDescriptor.colorAttachments[3].clearColor = MTLClearColor.init(red: Double(renderDescriptor.RenderTarget4ClearColor.Value.X), green: Double(renderDescriptor.RenderTarget4ClearColor.Value.Y), blue: Double(renderDescriptor.RenderTarget4ClearColor.Value.Z), alpha: Double(renderDescriptor.RenderTarget4ClearColor.Value.W))
            } else {
                renderPassDescriptor.colorAttachments[3].loadAction = .load
            }
        }

        if (renderDescriptor.DepthTextureId.HasValue == 1) {
            guard let depthTexture = self.textures[UInt(renderDescriptor.DepthTextureId.Value)] else {
                print("createRenderCommandList: Depth Texture is nil.")
                return false
            }
            renderPassDescriptor.depthAttachment.texture = depthTexture
        }

        if (renderDescriptor.DepthBufferOperation != DepthNone) {
            if (renderDescriptor.DepthBufferOperation == ClearWrite) {
                renderPassDescriptor.depthAttachment.loadAction = .clear
                renderPassDescriptor.depthAttachment.clearDepth = 1.0
            } else {
                renderPassDescriptor.depthAttachment.loadAction = .load
            }

            if (renderDescriptor.DepthBufferOperation == Write || renderDescriptor.DepthBufferOperation == ClearWrite) {
                renderPassDescriptor.depthAttachment.storeAction = .store
            }
        } else {
            renderPassDescriptor.depthAttachment.storeAction = .dontCare
        }
        
        guard let renderCommandEncoder = commandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor) else {
            print("createRenderCommandList: Render command encoder creation failed.")
            return false
        }

        renderCommandEncoder.label = label
        self.renderCommandEncoders[commandListId] = renderCommandEncoder

        if (renderDescriptor.DepthBufferOperation == Write || renderDescriptor.DepthBufferOperation == ClearWrite) {
            renderCommandEncoder.setDepthStencilState(self.depthWriteOperationState)
        } else if (renderDescriptor.DepthBufferOperation == CompareEqual) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareEqualState)
        } else if (renderDescriptor.DepthBufferOperation == CompareLess) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareLessState)
        } else {
            renderCommandEncoder.setDepthStencilState(self.depthNoneOperationState)
        }

        if (renderDescriptor.BackfaceCulling == 1) {
            renderCommandEncoder.setCullMode(.back)
        } else {
            renderCommandEncoder.setCullMode(.none)
        }

        if (renderPassDescriptor.colorAttachments[0].texture != nil) {
            renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderPassDescriptor.colorAttachments[0].texture!.width), height: Double(renderPassDescriptor.colorAttachments[0].texture!.height), znear: -1.0, zfar: 1.0))
        } else if (renderPassDescriptor.depthAttachment.texture != nil) {
            renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderPassDescriptor.depthAttachment.texture!.width), height: Double(renderPassDescriptor.depthAttachment.texture!.height), znear: -1.0, zfar: 1.0))
        }

        let fence = self.device.makeFence()!
        fence.label = "Fence" + label
        self.commandListFences[commandListId] = fence

        renderCommandEncoder.useHeap(self.globalHeap)
        renderCommandEncoder.useHeap(self.staticHeap)

        return true
    }

    public func commitRenderCommandList(_ commandListId: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("execureRenderCommandList: Render command encoder is nil.")
            return
        }

        guard let fence = self.commandListFences[commandListId] else {
            print("executeCopyCommandList: Fence was not found")
            return
        }

        renderCommandEncoder.updateFence(fence, after: .fragment)
        renderCommandEncoder.endEncoding()
        self.renderCommandEncoders[commandListId] = nil
    }

    public func setPipelineState(_ commandListId: UInt, _ pipelineStateId: UInt) {
        if (self.renderCommandEncoders[commandListId] != nil) {
            guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
                print("setPipelineState: Render command encoder is nil.")
                return
            }

            guard let pipelineState = self.renderPipelineStates[pipelineStateId] else {
                print("setPipelineState: PipelineState is nil.")
                return
            }

            renderCommandEncoder.setRenderPipelineState(pipelineState)
        } else if (self.computeCommandEncoders[commandListId] != nil) {
            guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
                print("setPipelineState: Compute command encoder is nil.")
                return
            }

            guard let pipelineState = self.computePipelineStates[pipelineStateId] else {
                print("setPipelineState: PipelineState is nil.")
                return
            }

            computeCommandEncoder.setComputePipelineState(pipelineState)
            currentComputePipelineState = pipelineState
        }
    }

    public func setShader(_ commandListId: UInt, _ shaderId: UInt) {
        guard let shader = self.shaders[shaderId] else {
            print("setShader: Shader is nil.")
            return
        }

        self.currentShader = shader
    }

    public func setShaderBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ slot: Int, _ isReadOnly: Bool, _ index: Int) {
        guard let shader = self.currentShader else {
            return
        }

        guard let graphicsBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("setShaderBuffer: Graphics buffer is nil.")
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        argumentEncoder.setBuffer(graphicsBuffer, offset: index, index: slot)
    }

    public func setShaderBuffers(_ commandListId: UInt, _ graphicsBufferIdList: [UInt32], _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var bufferList: [MTLBuffer] = []
        var offsets: [Int] = []

        for i in 0..<graphicsBufferIdList.count {
            guard let buffer = self.graphicsBuffers[UInt(graphicsBufferIdList[i])] else {
                return
            }

            bufferList.append(buffer)
            offsets.append(0)
        }

        argumentEncoder.setBuffers(bufferList, offsets: offsets, range: (slot + index)..<(slot + index) + graphicsBufferIdList.count)
    }

    public func setShaderTexture(_ commandListId: UInt, _ textureId: UInt, _ slot: Int, _ isReadOnly: Bool, _ index: Int) {
        guard let shader = self.currentShader else {
            return
        }

        guard let texture = self.textures[textureId] else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        argumentEncoder.setTexture(texture, index: slot)        

        // TODO: This code is needed because render targets are not allocated on the heap for the moment
        if (texture.usage == [.renderTarget, .shaderRead, .shaderWrite]) {
            if (self.computeCommandEncoders[commandListId] != nil) {
                let computeCommandEncoder = self.computeCommandEncoders[commandListId]!

                if (isReadOnly) {
                computeCommandEncoder.useResource(texture, usage: .read)
                } else {
                computeCommandEncoder.useResource(texture, usage: .write)

                }
            } else if (self.renderCommandEncoders[commandListId] != nil) {
                let computeCommandEncoder = self.renderCommandEncoders[commandListId]!
                computeCommandEncoder.useResource(texture, usage: .read)
            }
        }
    }

    public func setShaderTextures(_ commandListId: UInt, _ textureIdList: [UInt32], _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var textureList: [MTLTexture] = []

        for i in 0..<textureIdList.count {
            guard let texture = self.textures[UInt(textureIdList[i])] else {
                print("TEXTURE ERROR \(UInt(textureIdList[i]))")
                return
            }

            textureList.append(texture)

            // TODO: This code is needed because render targets are not allocated on the heap for the moment
            if (texture.usage == [.renderTarget, .shaderRead, .shaderWrite]) {
                if (self.computeCommandEncoders[commandListId] != nil) {
                    let computeCommandEncoder = self.computeCommandEncoders[commandListId]!
                    computeCommandEncoder.useResource(texture, usage: .read)
                } else if (self.renderCommandEncoders[commandListId] != nil) {
                    let computeCommandEncoder = self.renderCommandEncoders[commandListId]!
                    computeCommandEncoder.useResource(texture, usage: .read)
                }
            }
        }

        argumentEncoder.setTextures(textureList, range: (slot + index)..<(slot + index) + textureIdList.count)
    }

    public func setShaderIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ slot: Int, _ index: Int) {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            return
        }

        guard let shader = self.currentShader else {
            return
        }

        guard let indirectBuffer = self.indirectCommandBuffers[indirectCommandListId] else {
            print("setShaderIndirectCommandList: Indirect buffer is nil.")
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        argumentEncoder.setIndirectCommandBuffer(indirectBuffer, index: slot)
        computeCommandEncoder.useResource(indirectBuffer, usage: .write)
    }

    public func setShaderIndirectCommandLists(_ commandListId: UInt, _ indirectCommandListIdList: [UInt32], _ slot: Int, _ index: Int) {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            return
        }

        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var commandBufferList: [MTLIndirectCommandBuffer] = []

        for i in 0..<indirectCommandListIdList.count {
            guard let commandBuffer = self.indirectCommandBuffers[UInt(indirectCommandListIdList[i])] else {
                return
            }

            commandBufferList.append(commandBuffer)
            computeCommandEncoder.useResource(commandBuffer, usage: .write)
        }

        argumentEncoder.setIndirectCommandBuffers(commandBufferList, range: (slot + index)..<(slot + index) + indirectCommandListIdList.count)
    }

    public func executeIndirectCommandBuffer(_ commandListId: UInt, _ indirectCommandBufferId: UInt, _ maxCommandCount: Int) {
        if (self.currentShader == nil) {
            return
        }

        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("executeIndirectCommandList: Render command encoder is nil.")
            return
        }

        guard let indirectBuffer = self.indirectCommandBuffers[indirectCommandBufferId] else {
            print("setShaderIndirectCommandList: Indirect buffer is nil.")
            return
        }

        renderCommandEncoder.executeCommandsInBuffer(indirectBuffer, range: 0..<maxCommandCount)
    }

    public func setIndexBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt) {
        self.currentIndexBuffer = self.graphicsBuffers[graphicsBufferId]
    }

    public func drawIndexedPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ instanceCount: Int, _ baseInstanceId: Int) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("drawPrimitives: Render command encoder is nil.")
            return
        }

        guard let indexBuffer = self.currentIndexBuffer else {
            print("drawPrimitives: Index Buffer is nil.")
            return
        }
        
        if (self.currentShader != nil) {
            renderCommandEncoder.setVertexBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
            renderCommandEncoder.setFragmentBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
        }

        let startIndexOffset = Int(startIndex * 4)
        var primitiveTypeMetal = MTLPrimitiveType.triangle

        if (primitiveType == TriangleStrip) {
            primitiveTypeMetal = MTLPrimitiveType.triangleStrip
        } else if (primitiveType == Line) {
            primitiveTypeMetal = MTLPrimitiveType.line
        }

        renderCommandEncoder.drawIndexedPrimitives(type: primitiveTypeMetal, 
                                                   indexCount: Int(indexCount), 
                                                   indexType: .uint32, 
                                                   indexBuffer: indexBuffer, 
                                                   indexBufferOffset: startIndexOffset, 
                                                   instanceCount: instanceCount, 
                                                   baseVertex: 0, 
                                                   baseInstance: Int(baseInstanceId))

        if (self.currentShader != nil) {
            self.currentShader!.setupArgumentBuffer()
        }
    }

    public func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int, _ vertexCount: Int) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("drawPrimitives: Render command encoder is nil.")
            return
        }

        if (self.currentShader != nil) {
            renderCommandEncoder.setVertexBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
            renderCommandEncoder.setFragmentBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
        }

        var primitiveTypeMetal = MTLPrimitiveType.triangle

        if (primitiveType == TriangleStrip) {
            primitiveTypeMetal = MTLPrimitiveType.triangleStrip
        } else if (primitiveType == Line) {
            primitiveTypeMetal = MTLPrimitiveType.line
        }

        renderCommandEncoder.drawPrimitives(type: primitiveTypeMetal, 
                                                   vertexStart: startVertex, 
                                                   vertexCount: vertexCount)

        if (self.currentShader != nil) {
            self.currentShader!.setupArgumentBuffer()
        }
    }

    public func waitForCommandList(_ commandListId: UInt, _ commandListToWaitId: UInt) {
        guard let fence = self.commandListFences[commandListToWaitId] else {
            print("waitForCommandList: Fence was not found")
            return
        }

        if (self.renderCommandEncoders[commandListId] != nil) {
            guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
                print("waitForCommandList: Render command encoder is nil.")
                return
            }
            
            renderCommandEncoder.waitForFence(fence, before: .vertex)
        } else if (self.computeCommandEncoders[commandListId] != nil) {
            guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
                print("waitForCommandList: Compute command encoder is nil.")
                return
            }

            computeCommandEncoder.waitForFence(fence)
        } else if (self.copyCommandEncoders[commandListId] != nil) {
            guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
                print("waitForCommandList: Compute command encoder is nil.")
                return
            }
            copyCommandEncoder.waitForFence(fence)
        }
    }

    public func presentScreenBuffer(_ commandBufferId: UInt) {
        guard let commandBuffer = self.commandBuffers[commandBufferId] else {
            print("presentScreenBuffer: Command buffer is nil.")
            return
        }

        guard let currentMetalDrawable = self.currentMetalDrawable else {
            print("Error: Current Metal Drawable is null.")
            return
        }

        let handlerSemaphore = self.frameSemaphore

        commandBuffer.addCompletedHandler { cb in
            handlerSemaphore.signal()
        }

        // let duration = 33.0 / 1000.0 // Duration of 33 ms
        // let duration = 16.0 / 1000.0 // Duration of 16 ms
        // self.commandBuffer.present(currentMetalDrawable, afterMinimumDuration: duration)
        
        commandBuffer.present(currentMetalDrawable)
        self.currentMetalDrawable = nil
    }

    public func waitForAvailableScreenBuffer() {
        self.frameSemaphore.wait()

        self.currentFrameNumber += 1
        self.commandListFences = [:]
    }

    private func commandBufferCompleted(_ commandBuffer: MTLCommandBuffer, _ commandBufferId: UInt) {
        if (commandBuffer.error != nil) {
            //self.gpuError = true
            print("GPU ERROR: \(commandBuffer.error)")
        }

        commandBufferStatusConcurrentQueue.sync() {
            guard var commandBufferStatus = self.commandBuffersStatus[commandBufferId] else {
                return
            }

            commandBufferStatus.State = Completed
            commandBufferStatus.ExecutionStartTime = commandBuffer.gpuStartTime * 1000.0
            commandBufferStatus.ExecutionEndTime = commandBuffer.gpuEndTime * 1000.0

            self.commandBuffersStatus[commandBufferId] = commandBufferStatus
        }
    }
}

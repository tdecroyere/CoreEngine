import Metal
import QuartzCore.CAMetalLayer
import simd
import CoreEngineCommonInterop

class MetalHeap {
    let heapType: GraphicsServiceHeapType
    let length: UInt
    let heapObject: MTLHeap

    init (_ heapType: GraphicsServiceHeapType, _ length: UInt, _ heapObject: MTLHeap) {
        self.heapType = heapType
        self.length = length
        self.heapObject = heapObject
    }
}

class Shader {
    let shaderId: UInt
    let vertexShaderFunction: MTLFunction?
    let pixelShaderFunction: MTLFunction?
    let computeShaderFunction: MTLFunction?
    var argumentEncoder: MTLArgumentEncoder?
    var argumentBuffers: [MTLBuffer]
    let argumentBuffersMaxCount = 1000
    var argumentBufferCurrentIndex = 0
    var currentArgumentBuffer: MTLBuffer?

    init(_ shaderId: UInt, _ device: MTLDevice, _ vertexShaderFunction: MTLFunction?, _ pixelShaderFunction: MTLFunction?, _ computeShaderFunction: MTLFunction?, _ argumentEncoder: MTLArgumentEncoder?, _ debugName: String?) {
        self.shaderId = shaderId
        self.vertexShaderFunction = vertexShaderFunction
        self.pixelShaderFunction = pixelShaderFunction
        self.computeShaderFunction = computeShaderFunction
        self.argumentEncoder = nil
        self.argumentBuffers = []
        self.currentArgumentBuffer = nil
    }

    func setArgumentEncoder(_ device: MTLDevice, _ argumentEncoder: MTLArgumentEncoder?) {
        if (argumentEncoder != nil) {
            self.argumentEncoder = argumentEncoder
            // TODO: Use another allocation strategie
            for _ in 0..<argumentBuffersMaxCount {
                let argumentBuffer = device.makeBuffer(length: argumentEncoder!.encodedLength)!
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

    var depthCompareEqualState: MTLDepthStencilState!
    var depthCompareGreaterState: MTLDepthStencilState!
    var depthWriteOperationState: MTLDepthStencilState!
    var depthNoneOperationState: MTLDepthStencilState!

    var graphicsHeaps: [UInt: MetalHeap]

    var shaders: [UInt: Shader]
    var renderPipelineStates: [UInt: MTLRenderPipelineState]
    var computePipelineStates: [UInt: MTLComputePipelineState]

    var commandListFences: [UInt: MTLFence]
    var commandBuffers: [UInt: MTLCommandBuffer]

    var copyCommandEncoders: [UInt: MTLBlitCommandEncoder]
    var computeCommandEncoders: [UInt: MTLComputeCommandEncoder]
    var renderCommandEncoders: [UInt: MTLRenderCommandEncoder]
    var renderCommandRTs: [UInt: [MTLTexture]]

    var graphicsBuffers: [UInt: MTLBuffer]
    var textures: [UInt: MTLTexture]
    var indirectCommandBuffers: [UInt: MTLIndirectCommandBuffer]
    var queryBuffers: [UInt: MTLCounterSampleBuffer]

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

        self.graphicsHeaps = [:]
        self.shaders = [:]
        self.renderPipelineStates = [:]
        self.computePipelineStates = [:]
        self.commandListFences = [:]
        self.commandBuffers = [:]
        self.copyCommandEncoders = [:]
        self.computeCommandEncoders = [:]
        self.renderCommandEncoders = [:]
        self.graphicsBuffers = [:]
        self.textures = [:]
        self.indirectCommandBuffers = [:]
        self.queryBuffers = [:]
        self.renderCommandRTs = [:]

        self.frameSemaphore = DispatchSemaphore.init(value: 1);

        var depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .equal
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthCompareEqualState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .greaterEqual
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthCompareGreaterState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .greater
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthWriteOperationState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .always
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthNoneOperationState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue(maxCommandBufferCount: 1000)
    }

    public func getRenderSize() -> Vector2 {
        return Vector2(X: Float(self.renderWidth), Y: Float(self.renderHeight))
    }

    public func getTextureAllocationInfos(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> GraphicsAllocationInfos {
        let descriptor = createTextureDescriptor(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount)
        let sizeAndAlign = self.device.heapTextureSizeAndAlign(descriptor: descriptor)

        var result = GraphicsAllocationInfos()
        result.SizeInBytes = Int32(sizeAndAlign.size)
        result.Alignment = Int32(sizeAndAlign.align)

        return result
    }

    public func getGraphicsAdapterName(_ output: UnsafeMutablePointer<Int8>?) {
        let result = self.metalLayer.device!.name + " (Metal 3)";

        // let buffer = UnsafeMutableBufferPointer(start: output, count: result.utf16.count)
        // buffer.initialize(from: result.utf16)

        memcpy(output, strdup(result), result.count)
        output![result.count] = 0
    }

    public func changeRenderSize(renderWidth: Int, renderHeight: Int) {
        self.renderWidth = renderWidth
        self.renderHeight = renderHeight
        
        self.metalLayer.drawableSize = CGSize(width: renderWidth, height: renderHeight)
    }

    public func createGraphicsHeap(_ graphicsHeapId: UInt, _ type: GraphicsServiceHeapType, _ length: UInt) -> Bool {
        let heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .private
        heapDescriptor.type = .placement
        heapDescriptor.size = Int(length)
        heapDescriptor.hazardTrackingMode = .untracked

        if (type == Upload || type == ReadBack)
        {
            heapDescriptor.storageMode = .managed
            // TODO: Check why metal doesn't allow for upload heaps
            return true
        }

        if (type == Upload)
        {
            heapDescriptor.cpuCacheMode = .writeCombined
        }

        guard let graphicsHeap = self.device.makeHeap(descriptor: heapDescriptor) else {
            print("createGraphicsHeap: Creation failed.")
            return false
        }

        let metalHeap = MetalHeap(type, length, graphicsHeap)
        self.graphicsHeaps[graphicsHeapId] = metalHeap

        return true
    }

    public func setGraphicsHeapLabel(_ graphicsHeapId: UInt, _ label: String) {
        guard let graphicsHeap = self.graphicsHeaps[graphicsHeapId] else {
            print("setGraphicsHeapLabel: Graphics heap was not found")
            return
        }

        graphicsHeap.heapObject.label = label
    }

    public func deleteGraphicsHeap(_ graphicsHeapId: UInt) {
        self.graphicsHeaps[graphicsHeapId] = nil
    }

    public func createGraphicsBuffer(_ graphicsBufferId: UInt, _ graphicsHeapId: UInt, _ heapOffset: UInt, _ isAliasable: Bool, _ sizeInBytes: Int) -> Bool {
        let graphicsHeap = self.graphicsHeaps[graphicsHeapId]

        var options: MTLResourceOptions = [.storageModePrivate, .hazardTrackingModeUntracked]

        // if (graphicsHeap.heapType == ReadBack) {
        //     options = [.storageModeShared, .hazardTrackingModeUntracked]
        // }

        if (graphicsHeap == nil) {
            // TODO: CPU cache mode write combined seems to be slower on eGPU
            options = [.storageModeShared]//, .cpuCacheModeWriteCombined]
        }

        // Apple Metal doesn't support creating shared memory heaps so we create related
        // buffers via the Device
        if (graphicsHeap == nil) {
            guard let gpuBuffer = self.device.makeBuffer(length: sizeInBytes, options: options) else {
                print("createGraphicsBuffer: Creation failed.")
                return false
            }

            self.graphicsBuffers[graphicsBufferId] = gpuBuffer
        }
        else {
            guard let gpuBuffer = graphicsHeap!.heapObject.makeBuffer(length: sizeInBytes, options: options, offset: Int(heapOffset)) else {
                print("createGraphicsBuffer: Creation failed.")
                return false
            }

            self.graphicsBuffers[graphicsBufferId] = gpuBuffer

            if (isAliasable)
            {
                gpuBuffer.makeAliasable()
            }
        }

        return true
    }

    public func setGraphicsBufferLabel(_ graphicsBufferId: UInt, _ label: String) {
        guard let graphicsBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("setGraphicsBufferLabel: Graphics buffer was not found")
            return
        }

        graphicsBuffer.label = label
    }

    public func deleteGraphicsBuffer(_ graphicsBufferId: UInt) {
        self.graphicsBuffers[graphicsBufferId] = nil
    }

    public func getGraphicsBufferCpuPointer(_ graphicsBufferId: UInt) -> UnsafeMutableRawPointer? {
        guard let graphicsBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("ERROR: Graphics buffer was not found")
            return nil
        }

        return graphicsBuffer.contents()
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

    private func createTextureDescriptor(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> MTLTextureDescriptor {
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

        if (usage == RenderTarget) {
            descriptor.usage = [.shaderRead, .renderTarget]
        } else if (usage == ShaderWrite) {
            descriptor.usage = [.shaderRead, .shaderWrite]
        } else {
            descriptor.usage = [.shaderRead]
        }

        if (multisampleCount > 1) {
            descriptor.textureType = .type2DMultisample
        } else if (faceCount > 1) {
            descriptor.textureType = .typeCube
        } else {
            descriptor.textureType = .type2D
        }

        return descriptor
    }

    public func createTexture(_ textureId: UInt, _ graphicsHeapId: UInt, _ heapOffset: UInt, _ isAliasable: Bool, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> Bool {
        guard let graphicsHeap = self.graphicsHeaps[graphicsHeapId] else {
            print("createGraphicsBuffer: Graphics heap was not found")
            return false
        }

        let descriptor = createTextureDescriptor(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount)

        guard let gpuTexture = graphicsHeap.heapObject.makeTexture(descriptor: descriptor, offset: Int(heapOffset)) else {
            print("createTexture: Creation failed.")
            return false
        }

        self.textures[textureId] = gpuTexture

        if (isAliasable)
        {
            gpuTexture.makeAliasable()
        }

        return true
    }

    public func setTextureLabel(_ textureId: UInt, _ label: String) {
        guard let texture = self.textures[textureId] else {
            print("setTextureLabel: Texture was not found")
            return
        }

        texture.label = label
    }

    public func deleteTexture(_ textureId: UInt) {
        self.textures[textureId] = nil
    }

    public func createIndirectCommandBuffer(_ indirectCommandBufferId: UInt, _ maxCommandCount: Int) -> Bool {
        let indirectCommandBufferDescriptor = MTLIndirectCommandBufferDescriptor()
        
        indirectCommandBufferDescriptor.commandTypes = [.drawIndexed]
        indirectCommandBufferDescriptor.inheritBuffers = false
        indirectCommandBufferDescriptor.maxVertexBufferBindCount = 5
        indirectCommandBufferDescriptor.maxFragmentBufferBindCount = 5
        indirectCommandBufferDescriptor.inheritPipelineState = true

        let indirectCommandBuffer = self.device.makeIndirectCommandBuffer(descriptor: indirectCommandBufferDescriptor,
                                                                          maxCommandCount: maxCommandCount,
                                                                          options: .storageModePrivate)!

        self.indirectCommandBuffers[indirectCommandBufferId] = indirectCommandBuffer

        return true
    }

    public func setIndirectCommandBufferLabel(_ indirectCommandBufferId: UInt, _ label: String) {
        guard let indirectCommandBuffer = self.indirectCommandBuffers[indirectCommandBufferId] else {
            print("setIndirectCommandBuffer: Indirect Command Buffer was not found")
            return
        }

        indirectCommandBuffer.label = label
    }

    public func deleteIndirectCommandBuffer(_ indirectCommandBufferId: UInt) {
        self.indirectCommandBuffers[indirectCommandBufferId] = nil
    }

    public func createShader(_ shaderId: UInt, _ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> Bool {
        let dispatchData = DispatchData(bytes: UnsafeRawBufferPointer(start: shaderByteCode, count: shaderByteCodeLength))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        if (computeShaderFunction == nil) {
            let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")!
            let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

            self.shaders[shaderId] = Shader(shaderId, self.device, vertexFunction, fragmentFunction, nil, nil, "Shader")

            return true
        } else {
            let computeFunction = defaultLibrary.makeFunction(name: computeShaderFunction!)!

            let argumentEncoder = computeFunction.makeArgumentEncoder(bufferIndex: 0)
            self.shaders[shaderId] = Shader(shaderId, self.device, nil, nil, computeFunction, argumentEncoder, "Shader")
            return true
        }
    }

    public func setShaderLabel(_ shaderId: UInt, _ label: String) {
        if (self.shaders[shaderId]!.computeShaderFunction != nil) {
            let argumentEncoder = self.shaders[shaderId]!.computeShaderFunction!.makeArgumentEncoder(bufferIndex: 0)
            self.shaders[shaderId]!.setArgumentEncoder(self.device, argumentEncoder)
        } else if (label != "RenderMeshInstanceDepthShader" && label != "RenderMeshInstanceDepthMomentShader" && label != "RenderMeshInstanceShader" && label != "RenderMeshInstanceTransparentShader" && label != "RenderMeshInstanceTransparentDepthShader") {
            if (self.shaders[shaderId]!.vertexShaderFunction != nil) {
                let argumentEncoder = self.shaders[shaderId]!.vertexShaderFunction!.makeArgumentEncoder(bufferIndex: 0)
                self.shaders[shaderId]!.setArgumentEncoder(self.device, argumentEncoder)
            } 
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

    public func createPipelineState(_ pipelineStateId: UInt, _ shaderId: UInt, _ renderPassDescriptor: GraphicsRenderPassDescriptor) -> Bool {
        guard let shader = self.shaders[shaderId] else {
            return false
        }

        if (renderPassDescriptor.IsRenderShader == 1) {
            let pipelineStateDescriptor = MTLRenderPipelineDescriptor()

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

    public func setPipelineStateLabel(_ pipelineStateId: UInt, _ label: String) {
    }

    public func deletePipelineState(_ pipelineStateId: UInt) {
        self.renderPipelineStates[pipelineStateId] = nil
        self.computePipelineStates[pipelineStateId] = nil
    }

    public func createQueryBuffer(_ queryBufferId: UInt, _ queryBufferType: GraphicsQueryBufferType, _ length: Int) -> Bool {
        guard let counterSets = self.device.counterSets else {
            print("createQueryBuffer: Counter sets are not available.")
            return false
        }

        var foundCounterSet: MTLCounterSet? = nil
        
        for systemCounterSet in counterSets {
            if (systemCounterSet.name == "timestamp" as String && queryBufferType == Timestamp) {
                foundCounterSet = systemCounterSet
                break
            }
        }

        guard let counterSet = foundCounterSet else {
            print("createQueryBuffer: Counter was not found.")
            return false
        }

        let descriptor = MTLCounterSampleBufferDescriptor()
        descriptor.counterSet = counterSet
        descriptor.sampleCount = length
        descriptor.storageMode = .shared

        do {
            let queryBuffer = try self.device.makeCounterSampleBuffer(descriptor: descriptor)
            self.queryBuffers[queryBufferId] = queryBuffer
        } catch {
            print("Failed to create query buffer, \(error)")
            return false
        }

        return true
    }

    public func setQueryBufferLabel(_ queryBufferId: UInt, _ label: String) {
        
    }

    public func deleteQueryBuffer(_ queryBufferId: UInt) {
        self.queryBuffers[queryBufferId] = nil
    }

    public func createCommandBuffer(_ commandBufferId: UInt, _ commandBufferType: GraphicsCommandBufferType, _ label: String) -> Bool {
        return true
    }

    public func deleteCommandBuffer(_ commandBufferId: UInt) {
    }

    public func resetCommandBuffer(_ commandBufferId: UInt) {
        guard let commandBuffer = self.commandQueue.makeCommandBuffer() else {
            print("ERROR creating command buffer.")
            return
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

        createFence(commandListId, commandBufferId, label)

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

    public func copyDataToGraphicsBuffer(_ commandListId: UInt, _ destinationGraphicsBufferId: UInt, _ sourceGraphicsBufferId: UInt, _ length: Int) {
        guard let destinationBuffer = self.graphicsBuffers[destinationGraphicsBufferId] else {
            print("copyDataToGraphicsBuffer: Destination graphics buffer was not found")
            return
        }

        guard let sourceBuffer = self.graphicsBuffers[sourceGraphicsBufferId] else {
            print("copyDataToGraphicsBuffer: Source graphics buffer was not found")
            return
        }

        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("copyDataToGraphicsBuffer: Copy command encoder is nil.")
            return
        }

        // TODO: Add parameters to be able to update partially the buffer
        copyCommandEncoder.copy(from: sourceBuffer, sourceOffset: 0, to: destinationBuffer, destinationOffset: 0, size: length)
    }

    let performanceTimer = PerformanceTimer()

    public func copyDataToTexture(_ commandListId: UInt, _ destinationTextureId: UInt, _ sourceGraphicsBufferId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int) {
        guard let destinationTexture = self.textures[destinationTextureId] else {
            print("copyDataToTexture: Destination texture was not found")
            return
        }

        guard let sourceGraphicsBuffer = self.graphicsBuffers[sourceGraphicsBufferId] else {
            print("copyDataToTexture: Source graphics buffer was not found")
            return
        }

        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("copyDataToTexture: Copy command encoder is nil.")
            return
        }

        // TODO: Try to get the texture footprint from the device
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
        copyCommandEncoder.copy(from: sourceGraphicsBuffer, 
                                sourceOffset: 0, 
                                sourceBytesPerRow: sourceBytesPerRow,
                                sourceBytesPerImage: sourceBytesPerImage,
                                sourceSize: MTLSize(width: width, height: height , depth: 1),
                                to: destinationTexture, 
                                destinationSlice: slice,
                                destinationLevel: mipLevel,
                                destinationOrigin: MTLOrigin(x: 0, y: 0, z: 0))
    }

    public func copyTexture(_ commandListId: UInt, _ destinationTextureId: UInt, _ sourceTextureId: UInt) {
        guard let destinationTexture = self.textures[destinationTextureId] else {
            print("copyTexture: Destination texture was not found")
            return
        }

        guard let sourceTexture = self.textures[sourceTextureId] else {
            print("copyTexture: Source texture was not found")
            return
        }

        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("copyTexture: Copy command encoder is nil.")
            return
        }

        copyCommandEncoder.copy(from: sourceTexture, to: destinationTexture)
        // copyCommandEncoder.copy(from: sourceTexture, sourceSlice: 0, sourceLevel: 0, to: destinationTexture, destinationSlice: 0, destinationLevel: 0, sliceCount: 1, levelCount: 1)
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

        createFence(commandListId, commandBufferId, label)
        bindGraphicsHeaps(commandListId);

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

        computeCommandEncoder.memoryBarrier(scope: [.textures])
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

        var renderTargetList: [MTLTexture] = []

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

            renderTargetList.append(colorTexture)
            renderPassDescriptor.colorAttachments[0].texture = colorTexture
            renderPassDescriptor.colorAttachments[0].storeAction = .store
        }

        if (renderDescriptor.RenderTarget2TextureId.HasValue == 1) {
            guard let colorTexture = self.textures[UInt(renderDescriptor.RenderTarget2TextureId.Value)] else {
                print("createRenderCommandList: Render Target 2 is nil.")
                return false
            }

            renderTargetList.append(colorTexture)
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

            renderTargetList.append(colorTexture)
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

            renderTargetList.append(colorTexture)
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

            renderTargetList.append(depthTexture)
            renderPassDescriptor.depthAttachment.texture = depthTexture
        }

        if (renderDescriptor.DepthBufferOperation != DepthNone) {
            if (renderDescriptor.DepthBufferOperation == ClearWrite) {
                renderPassDescriptor.depthAttachment.loadAction = .clear
                renderPassDescriptor.depthAttachment.clearDepth = 0.0
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
        } else if (renderDescriptor.DepthBufferOperation == CompareGreater) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareGreaterState)
        } else {
            renderCommandEncoder.setDepthStencilState(self.depthNoneOperationState)
        }

        if (renderDescriptor.BackfaceCulling == 1) {
            renderCommandEncoder.setCullMode(.back)
        } else {
            renderCommandEncoder.setCullMode(.none)
        }

        if (renderPassDescriptor.colorAttachments[0].texture != nil) {
            renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderPassDescriptor.colorAttachments[0].texture!.width), height: Double(renderPassDescriptor.colorAttachments[0].texture!.height), znear: 0.0, zfar: 1.0))
        } else if (renderPassDescriptor.depthAttachment.texture != nil) {
            renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderPassDescriptor.depthAttachment.texture!.width), height: Double(renderPassDescriptor.depthAttachment.texture!.height), znear: 0.0, zfar: 1.0))
        }

        createFence(commandListId, commandBufferId, label)
        self.renderCommandRTs[commandListId] = renderTargetList

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
        self.renderCommandRTs[commandListId] = nil
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

            bindGraphicsHeaps(commandListId);
            renderCommandEncoder.useResources(self.renderCommandRTs[commandListId]!, usage: [.read, .write])
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

    private func bindGraphicsHeaps(_ commandListId: UInt) {
        var heapList: [MTLHeap] = []

        for (_, value) in self.graphicsHeaps {
            heapList.append(value.heapObject)
        }

        if (self.renderCommandEncoders[commandListId] != nil) {
            guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
                print("useGraphicsHeap: Render command encoder is nil.")
                return
            }
            
            renderCommandEncoder.useHeaps(heapList)
        } else if (self.computeCommandEncoders[commandListId] != nil) {
            guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
                print("useGraphicsHeap: Compute command encoder is nil.")
                return
            }
            computeCommandEncoder.useHeaps(heapList)
        }
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

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        guard let texture = self.textures[textureId] else {
            return
        }

        if (texture.usage == [.shaderRead, .renderTarget] || texture.usage == [.shaderRead, .shaderWrite]) {
            if (self.computeCommandEncoders[commandListId] != nil) {
                let computeCommandEncoder = self.computeCommandEncoders[commandListId]!

                if (isReadOnly) {
                    computeCommandEncoder.useResource(texture, usage: .read)
                } else {
                    computeCommandEncoder.useResource(texture, usage: .write)
                }
            } else if (self.renderCommandEncoders[commandListId] != nil) {
                let renderCommandEncoder = self.renderCommandEncoders[commandListId]!

                if (isReadOnly) {
                    renderCommandEncoder.useResource(texture, usage: .read)
                } else {
                    renderCommandEncoder.useResource(texture, usage: .write)
                }
            }
        }

        argumentEncoder.setTexture(texture, index: slot)        
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
            let textureId = UInt(textureIdList[i])

            guard let texture = self.textures[textureId] else {
                print("TEXTURE ERROR \(textureId)")
                return
            }

            if (texture.usage == [.shaderRead, .renderTarget] || texture.usage == [.shaderRead, .shaderWrite]) {
                if (self.computeCommandEncoders[commandListId] != nil) {
                    let computeCommandEncoder = self.computeCommandEncoders[commandListId]!
                    computeCommandEncoder.useResource(texture, usage: .read)
                } else if (self.renderCommandEncoders[commandListId] != nil) {
                    let renderCommandEncoder = self.renderCommandEncoders[commandListId]!
                    renderCommandEncoder.useResource(texture, usage: .read)
                }
            }

            textureList.append(texture)
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

    public func queryTimestamp(_ commandListId: UInt, _ queryBufferId: UInt, _ index: Int) {
        guard let queryBuffer = self.queryBuffers[queryBufferId] else {
            print("queryTimestamp: Query buffer was not found")
            return
        }

        if (self.renderCommandEncoders[commandListId] != nil) {
            guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
                print("queryTimestamp: Render command encoder is nil.")
                return
            }
            
            renderCommandEncoder.sampleCounters(sampleBuffer: queryBuffer, sampleIndex: index, barrier: false)
        } else if (self.computeCommandEncoders[commandListId] != nil) {
            guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
                print("queryTimestamp: Compute command encoder is nil.")
                return
            }

            computeCommandEncoder.sampleCounters(sampleBuffer: queryBuffer, sampleIndex: index, barrier: false)
        } else if (self.copyCommandEncoders[commandListId] != nil) {
            guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
                print("queryTimestamp: Compute command encoder is nil.")
                return
            }
            copyCommandEncoder.sampleCounters(sampleBuffer: queryBuffer, sampleIndex: index, barrier: false)
        }
    }

    public func resolveQueryData(_ commandListId: UInt, _ queryBufferId: UInt, _ destinationBufferId: UInt, _ startIndex: Int, _ endIndex: Int) {
        guard let queryBuffer = self.queryBuffers[queryBufferId] else {
            print("resolveQueryData: Query buffer is nil.")
            return
        }

        guard let destinationBuffer = self.graphicsBuffers[destinationBufferId] else {
            print("resolveQueryData: Query buffer is nil.")
            return
        }

        do {
            let data = try queryBuffer.resolveCounterRange(0..<queryBuffer.sampleCount)!
            data.copyBytes(to: destinationBuffer.contents().assumingMemoryBound(to: UInt8.self), from: 0..<data.count)
        } catch {
            print("resolveQueryData: Unable to resolve query buffer, \(error)")
            return
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
        let duration = 16.0 / 1000.0 // Duration of 16 ms
        commandBuffer.present(currentMetalDrawable, afterMinimumDuration: duration)
        
        //commandBuffer.present(currentMetalDrawable)
        self.currentMetalDrawable = nil
    }

    public func waitForAvailableScreenBuffer() {
        self.frameSemaphore.wait()

        self.currentFrameNumber += 1
        self.commandListFences = [:]
    }

    private func createFence(_ commandListId: UInt, _ commandBufferId: UInt, _ label: String) {
        let fence = self.device.makeFence()!
        fence.label = "Fence" + label
        self.commandListFences[commandListId] = fence
    }

    private func commandBufferCompleted(_ commandBuffer: MTLCommandBuffer, _ commandBufferId: UInt) {
        if (commandBuffer.error != nil) {
            //self.gpuError = true
            print("GPU ERROR: \(commandBuffer.error!)")
        }
    }
}

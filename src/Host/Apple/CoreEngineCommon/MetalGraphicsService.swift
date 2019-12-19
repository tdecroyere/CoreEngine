import Metal
import QuartzCore.CAMetalLayer
import simd
import CoreEngineCommonInterop

class Shader {
    var pipelineState: MTLRenderPipelineState!
    var computePipelineState: MTLComputePipelineState!
    let argumentEncoder: MTLArgumentEncoder?
    var argumentBuffers: [MTLBuffer]
    let argumentBuffersMaxCount = 100
    var argumentBufferCurrentIndex = 0
    var currentArgumentBuffer: MTLBuffer?

    init(_ device: MTLDevice, _ pipelineState: MTLRenderPipelineState, _ argumentEncoder: MTLArgumentEncoder?) {
        self.pipelineState = pipelineState
        self.argumentEncoder = argumentEncoder

        self.argumentBuffers = []
        self.currentArgumentBuffer = nil

        if (argumentEncoder != nil) {
            // TODO: Use another allocation strategie
            for i in 0..<argumentBuffersMaxCount {
                let argumentBuffer = device.makeBuffer(length: argumentEncoder!.encodedLength)!
                argumentBuffer.label = (pipelineState.label != nil) ? "\(pipelineState.label!)Buffer\(i)" : "ShaderBuffer\(i)"
                self.argumentBuffers.append(argumentBuffer)
            }

            self.currentArgumentBuffer = argumentBuffers[0]
            argumentEncoder!.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
        }
    }

    init(_ device: MTLDevice, _ pipelineState: MTLComputePipelineState, _ argumentEncoder: MTLArgumentEncoder?) {
        self.computePipelineState = pipelineState
        self.argumentEncoder = argumentEncoder

        self.argumentBuffers = []
        self.currentArgumentBuffer = nil

        if (argumentEncoder != nil) {
            // TODO: Use another allocation strategie
            for i in 0..<argumentBuffersMaxCount {
                let argumentBuffer = device.makeBuffer(length: argumentEncoder!.encodedLength)!
                argumentBuffer.label = (pipelineState.label != nil) ? "\(pipelineState.label!)Buffer\(i)" : "ComputeShaderBuffer\(i)"
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

    var renderWidth: Int
    var renderHeight: Int
    var multisampleCount: Int = 4

    var commandQueue: MTLCommandQueue!
    var commandBuffer: MTLCommandBuffer!
    var globalHeap: MTLHeap!

    var depthCompareWriteState: MTLDepthStencilState!
    var depthCompareNoWriteState: MTLDepthStencilState!
    var depthNoCompareWriteState: MTLDepthStencilState!
    var depthNoCompareNoWriteState: MTLDepthStencilState!

    var shaders: [UInt: Shader]

    var copyCommandBuffers: [UInt: MTLCommandBuffer]
    var copyCommandEncoders: [UInt: MTLBlitCommandEncoder]

    var computeCommandBuffers: [UInt: MTLCommandBuffer]
    var computeCommandEncoders: [UInt: MTLComputeCommandEncoder]

    var renderCommandBuffers: [UInt: MTLCommandBuffer]
    var renderCommandEncoders: [UInt: MTLRenderCommandEncoder]

    var graphicsBuffers: [UInt: MTLBuffer]
    var cpuGraphicsBuffers: [UInt: MTLBuffer]
    var textures: [UInt: MTLTexture]
    var indirectCommandBuffers: [UInt: MTLIndirectCommandBuffer]

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
        self.metalLayer.pixelFormat = .bgra8Unorm_srgb
        self.metalLayer.framebufferOnly = true
        self.metalLayer.allowsNextDrawableTimeout = false
        self.metalLayer.displaySyncEnabled = true
        self.metalLayer.maximumDrawableCount = 3
        self.metalLayer.drawableSize = CGSize(width: renderWidth, height: renderHeight)

        self.shaders = [:]
        self.copyCommandBuffers = [:]
        self.copyCommandEncoders = [:]
        self.computeCommandBuffers = [:]
        self.computeCommandEncoders = [:]
        self.renderCommandBuffers = [:]
        self.renderCommandEncoders = [:]
        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]
        self.textures = [:]
        self.indirectCommandBuffers = [:]

        var depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthCompareWriteState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthCompareNoWriteState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .always
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthNoCompareWriteState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .always
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthNoCompareNoWriteState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()
        self.commandBuffer = self.commandQueue.makeCommandBuffer()

        let heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .private
        heapDescriptor.type = .automatic // TODO: Switch to placement mode for manual memory management
        heapDescriptor.size = 1024 * 1024 * 1024; // Allocate 1GB for now
        self.globalHeap = self.device.makeHeap(descriptor: heapDescriptor)!
    }

    public func getRenderSize() -> Vector2 {
        return Vector2(X: Float(self.renderWidth), Y: Float(self.renderHeight))
    }

    public func changeRenderSize(renderWidth: Int, renderHeight: Int) {
        self.renderWidth = renderWidth
        self.renderHeight = renderHeight
        
        self.metalLayer.drawableSize = CGSize(width: renderWidth, height: renderHeight)
    }

    public func createGraphicsBuffer(_ graphicsBufferId: UInt, _ length: Int, _ debugName: String?) -> Bool {
        // TODO: Page Align the length to avoid the copy of the buffer later
        // TODO: Check for errors

        // Create a the metal buffer on the CPU
        let cpuBuffer = self.device.makeBuffer(length: length, options: .cpuCacheModeWriteCombined)!
        cpuBuffer.label = (debugName != nil) ? "\(debugName!)Cpu" : "GraphicsBuffer\(graphicsBufferId)Cpu"

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: length, options: .storageModePrivate)!
        gpuBuffer.label = (debugName != nil) ? "\(debugName!)Gpu" : "GraphicsBuffer\(graphicsBufferId)Gpu"

        self.graphicsBuffers[graphicsBufferId] = gpuBuffer
        self.cpuGraphicsBuffers[graphicsBufferId] = cpuBuffer

        return true
    }

    public func createTexture(_ textureId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ isRenderTarget: Bool, _ debugName: String?) -> Bool {
        // TODO: Check for errors
        let descriptor = MTLTextureDescriptor()

        descriptor.width = width
        descriptor.height = height
        descriptor.depth = 1
        descriptor.mipmapLevelCount = 1
        descriptor.arrayLength = 1
        descriptor.storageMode = .private

        if (textureFormat == Bgra8UnormSrgb) {
            descriptor.pixelFormat = .bgra8Unorm_srgb
        } else if (textureFormat == Depth32Float) {
            descriptor.pixelFormat = .depth32Float
        } else {
            descriptor.pixelFormat = .rgba8Unorm_srgb
        }

        if (isRenderTarget) {
            descriptor.textureType = .type2DMultisample
            descriptor.sampleCount = self.multisampleCount
            descriptor.usage = [.renderTarget, .shaderRead]
        } else {
            descriptor.textureType = .type2D
        }

        guard let gpuTexture = self.globalHeap.makeTexture(descriptor: descriptor) else {
            print("createTexture: Creation failed.")
            return false
        }

        gpuTexture.label = (debugName != nil) ? debugName! : "Texture\(textureId)"

        self.textures[textureId] = gpuTexture
        return true
    }

    public func removeTexture(_ textureId: UInt) {
        self.textures[textureId] = nil
    }

    public func createShader(_ shaderId: UInt, _ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int, _ useDepthBuffer: Bool, _ debugName: String?) -> Bool {
        let dispatchData = DispatchData(bytes: UnsafeRawBufferPointer(start: shaderByteCode, count: shaderByteCodeLength))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        if (computeShaderFunction == nil) {
            let pipelineStateDescriptor = MTLRenderPipelineDescriptor()
            pipelineStateDescriptor.label = (debugName != nil) ? debugName! : "RenderShader\(shaderId)"

            let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")!
            let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")!
            
            pipelineStateDescriptor.vertexFunction = vertexFunction
            pipelineStateDescriptor.fragmentFunction = fragmentFunction
            pipelineStateDescriptor.supportIndirectCommandBuffers = true
            pipelineStateDescriptor.sampleCount = self.multisampleCount

            // TODO: Use the correct render target format
            pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.metalLayer.pixelFormat

            // TODO: Add an option to disable blending
            pipelineStateDescriptor.colorAttachments[0].isBlendingEnabled = true
            pipelineStateDescriptor.colorAttachments[0].rgbBlendOperation = .add
            pipelineStateDescriptor.colorAttachments[0].alphaBlendOperation = .add
            pipelineStateDescriptor.colorAttachments[0].sourceRGBBlendFactor = .sourceAlpha
            pipelineStateDescriptor.colorAttachments[0].sourceAlphaBlendFactor = .sourceAlpha;
            pipelineStateDescriptor.colorAttachments[0].destinationRGBBlendFactor = .oneMinusSourceAlpha
            pipelineStateDescriptor.colorAttachments[0].destinationAlphaBlendFactor = .oneMinusSourceAlpha

            // TODO
            if (useDepthBuffer) {
                pipelineStateDescriptor.depthAttachmentPixelFormat = .depth32Float
            }

            do {
                let pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)

                // TODO: Remove that hack
                if (debugName != nil && debugName != "RenderMeshInstanceShader") {
                    let argumentEncoder = vertexFunction.makeArgumentEncoder(bufferIndex: 0)
                    self.shaders[shaderId] = Shader(self.device, pipelineState, argumentEncoder)
                } else {
                    self.shaders[shaderId] = Shader(self.device, pipelineState, nil)
                }

                return true
            } catch {
                print("Failed to created pipeline state, \(error)")
            }
        } else {
            let computeFunction = defaultLibrary.makeFunction(name: computeShaderFunction!)!

            do {
                let pipelineState = try self.device.makeComputePipelineState(function: computeFunction)
                let argumentEncoder = computeFunction.makeArgumentEncoder(bufferIndex: 0)

                self.shaders[shaderId] = Shader(self.device, pipelineState, argumentEncoder)
                return true
            }
            catch {
                print("Failed to created pipeline state, \(error)")
            }
        }

        return false
    }

    public func removeShader(_ shaderId: UInt) {
        self.shaders[shaderId] = nil
    }

    public func createCopyCommandList(_ commandListId: UInt, _ debugName: String?, _ createNewCommandBuffer: Bool) -> Bool {
        var commandBuffer = self.commandBuffer!

        if (createNewCommandBuffer) {
            guard let copyCommandBuffer = self.commandQueue.makeCommandBuffer() else {
                print("ERROR creating copy command buffer.")
                return false
            }

            commandBuffer = copyCommandBuffer
            commandBuffer.label = "Copy Command Buffer \(commandListId)"
            self.copyCommandBuffers[commandListId] = commandBuffer
        }

        guard let copyCommandEncoder = commandBuffer.makeBlitCommandEncoder() else {
            print("ERROR creating copy command encoder.")
            return false
        }

        copyCommandEncoder.label = (debugName != nil) ? debugName! : "Copy Command Encoder\(commandListId)"
        self.copyCommandEncoders[commandListId] = copyCommandEncoder

        return true
    }

    public func executeCopyCommandList(_ commandListId: UInt) {
        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("executeCopyCommandList: Copy command encoder is nil.")
            return
        }

        copyCommandEncoder.endEncoding()
        self.copyCommandEncoders[commandListId] = nil

        if (self.copyCommandBuffers[commandListId] != nil) {
            let commandBuffer = self.copyCommandBuffers[commandListId]!
            commandBuffer.commit()
            self.copyCommandBuffers[commandListId] = nil
        }
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

    public func uploadDataToTexture(_ commandListId: UInt, _ textureId: UInt, _ width: Int, _ height: Int, _ data: UnsafeMutableRawPointer, _ length: Int) {
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

        // TODO: Remove the hardcoding here
        let pixelSize = 4

        // TODO: Add parameters to be able to update partially the buffer
        copyCommandEncoder.copy(from: cpuTexture, 
                                sourceOffset: 0, 
                                sourceBytesPerRow: pixelSize * width,
                                sourceBytesPerImage: pixelSize * width * height,
                                sourceSize: MTLSize(width: width, height: height , depth: 1),
                                to: gpuTexture, 
                                destinationSlice: 0,
                                destinationLevel: 0,
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

    public func createComputeCommandList(_ commandListId: UInt, _ debugName: String?, _ createNewCommandBuffer: Bool) -> Bool {
        var commandBuffer = self.commandBuffer!

        if (createNewCommandBuffer) {
            guard let computeCommandBuffer = self.commandQueue.makeCommandBuffer() else {
                print("ERROR creating compute command buffer.")
                return false
            }

            commandBuffer = computeCommandBuffer
            commandBuffer.label = "Compute Command Buffer \(commandListId)"
            self.computeCommandBuffers[commandListId] = commandBuffer
        }

        guard let computeCommandEncoder = commandBuffer.makeComputeCommandEncoder() else {
            print("ERROR creating compute command encoder.")
            return false
        }

        computeCommandEncoder.label = (debugName != nil) ? debugName! : "ComputeCommandEncoder\(commandListId)"
        self.computeCommandEncoders[commandListId] = computeCommandEncoder

        return true
    }

    public func executeComputeCommandList(_ commandListId: UInt) {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            print("executeComputeCommandList: Compute command encoder is nil.")
            return
        }

        computeCommandEncoder.endEncoding()
        self.computeCommandEncoders[commandListId] = nil

        if (self.computeCommandBuffers[commandListId] != nil) {
            let commandBuffer = self.computeCommandBuffers[commandListId]!
            commandBuffer.commit()
            self.computeCommandBuffers[commandListId] = nil
        }
    }

    public func dispatchThreadGroups(_ commandListId: UInt, _ threadGroupCountX: UInt, _ threadGroupCountY: UInt, _ threadGroupCountZ: UInt) {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            print("dispatchThreadGroups: Compute command encoder is nil.")
            return
        }

        if (self.currentShader != nil) {
            computeCommandEncoder.setBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
        }

        var w = self.currentShader!.computePipelineState.threadExecutionWidth
        var h = (threadGroupCountY > 1) ? self.currentShader!.computePipelineState.maxTotalThreadsPerThreadgroup / w : 1
        let threadsPerGroup = MTLSizeMake(w, h, 1)

        computeCommandEncoder.dispatchThreads(MTLSize(width: Int(threadGroupCountX), height: Int(threadGroupCountY), depth: Int(threadGroupCountZ)), threadsPerThreadgroup: threadsPerGroup)

        if (self.currentShader != nil) {
            self.currentShader!.setupArgumentBuffer()
        }
    }

    public func createRenderCommandList(_ commandListId: UInt, _ renderDescriptor: GraphicsRenderPassDescriptor, _ debugName: String?, _ createNewCommandBuffer: Bool) -> Bool {
        var commandBuffer = self.commandBuffer!

        if (createNewCommandBuffer) {
            guard let renderCommandBuffer = self.commandQueue.makeCommandBuffer() else {
                print("ERROR creating render command buffer.")
                return false
            }

            commandBuffer = renderCommandBuffer
            commandBuffer.label = "Render Command Buffer\(commandListId)"
            self.renderCommandBuffers[commandListId] = commandBuffer
        }

        // Create render command encoder        
        guard let colorTexture = self.textures[UInt(renderDescriptor.ColorTextureId.Value)] else {
            print("createRenderCommandList: Color Texture is nil.")
            return false
        }

        let renderPassDescriptor = MTLRenderPassDescriptor()
        renderPassDescriptor.colorAttachments[0].texture = colorTexture
        
        if (renderDescriptor.ClearColor.HasValue == 1) {
            renderPassDescriptor.colorAttachments[0].loadAction = .clear
            renderPassDescriptor.colorAttachments[0].clearColor = MTLClearColor.init(red: Double(renderDescriptor.ClearColor.Value.X), green: Double(renderDescriptor.ClearColor.Value.Y), blue: Double(renderDescriptor.ClearColor.Value.Z), alpha: Double(renderDescriptor.ClearColor.Value.W))
        } else {
            renderPassDescriptor.colorAttachments[0].loadAction = .load
        }

        if (renderDescriptor.WriteToHardwareRenderTarget == 1) {
            renderPassDescriptor.colorAttachments[0].storeAction = .multisampleResolve
        } else {
            renderPassDescriptor.colorAttachments[0].storeAction = .store
        }

        if (renderDescriptor.WriteToHardwareRenderTarget == 1) {
            guard let nextCurrentMetalDrawable = self.metalLayer.nextDrawable() else {
                return false
            }
            
            self.currentMetalDrawable = nextCurrentMetalDrawable
            renderPassDescriptor.colorAttachments[0].resolveTexture = nextCurrentMetalDrawable.texture
        }

        // TODO
        if (renderDescriptor.DepthTextureId.HasValue == 1) {
            guard let depthTexture = self.textures[UInt(renderDescriptor.DepthTextureId.Value)] else {
                print("createRenderCommandList: Depth Texture is nil.")
                return false
            }
            renderPassDescriptor.depthAttachment.texture = depthTexture
        }

        if (renderDescriptor.DepthCompare == 1 || renderDescriptor.DepthWrite == 1) {
            if (renderDescriptor.DepthWrite == 1) {
                renderPassDescriptor.depthAttachment.loadAction = .clear // TODO: Use a separate pass for depth buffer
                renderPassDescriptor.depthAttachment.clearDepth = 1.0
            } else {
                renderPassDescriptor.depthAttachment.loadAction = .load // TODO: Use a separate pass for depth buffer
            }

            if (renderDescriptor.DepthWrite == 1) {
                renderPassDescriptor.depthAttachment.storeAction = .store
            }
        } else {
            renderPassDescriptor.depthAttachment.storeAction = .dontCare
        }
        
        guard let renderCommandEncoder = commandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor) else {
            print("createRenderCommandList: Render command encoder creation failed.")
            return false
        }

        renderCommandEncoder.label = (debugName != nil) ? debugName! : "RenderCommandEncoder\(commandListId)"
        self.renderCommandEncoders[commandListId] = renderCommandEncoder

        if (renderDescriptor.DepthCompare == 1 && renderDescriptor.DepthWrite == 1) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareWriteState)
        } else if (renderDescriptor.DepthCompare == 1 && renderDescriptor.DepthWrite == 0) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareNoWriteState)
        } else if (renderDescriptor.DepthCompare == 0 && renderDescriptor.DepthWrite == 1) {
            renderCommandEncoder.setDepthStencilState(self.depthNoCompareWriteState)
        } else {
            renderCommandEncoder.setDepthStencilState(self.depthNoCompareNoWriteState)
        }

        renderCommandEncoder.setCullMode(.back)

        let renderSize = getRenderSize()
        renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderSize.X), height: Double(renderSize.Y), znear: -1.0, zfar: 1.0))
        
        renderCommandEncoder.useHeap(self.globalHeap)

        return true
    }

    public func executeRenderCommandList(_ commandListId: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("execureRenderCommandList: Render command encoder is nil.")
            return
        }

        renderCommandEncoder.endEncoding()
        self.renderCommandEncoders[commandListId] = nil

        if (self.renderCommandBuffers[commandListId] != nil) {
            let commandBuffer = self.renderCommandBuffers[commandListId]!
            commandBuffer.commit()
            self.renderCommandBuffers[commandListId] = nil
        }
    }

    public func createIndirectCommandList(_ commandListId: UInt, _ maxCommandCount: Int, _ debugName: String?) -> Bool {
        let indirectCommandBufferDescriptor = MTLIndirectCommandBufferDescriptor()
        
        indirectCommandBufferDescriptor.commandTypes = [.drawIndexed]

        // Indicate that buffers will be set for each command in the indirect command buffer.
        indirectCommandBufferDescriptor.inheritBuffers = false

        // Indicate that a maximum of 3 buffers will be set for each command.
        indirectCommandBufferDescriptor.maxVertexBufferBindCount = 5
        indirectCommandBufferDescriptor.maxFragmentBufferBindCount = 5

        // TODO: Change that when we will set material in the compute shader
        indirectCommandBufferDescriptor.inheritPipelineState = true

        // Create indirect command buffer using private storage mode; since only the GPU will
        // write to and read from the indirect command buffer, the CPU never needs to access the
        // memory
        var indirectCommandBuffer = self.device.makeIndirectCommandBuffer(descriptor: indirectCommandBufferDescriptor,
                                                                          maxCommandCount: maxCommandCount,
                                                                          options: .storageModePrivate)!

        indirectCommandBuffer.label = (debugName != nil) ? debugName! : "IndirectCommandBuffer\(commandListId)"

        self.indirectCommandBuffers[commandListId] = indirectCommandBuffer

        return true
    }

    public func setShader(_ commandListId: UInt, _ shaderId: UInt) {
        if (self.renderCommandEncoders[commandListId] != nil) {
            guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
                print("setShader: Render command encoder is nil.")
                return
            }

            guard let shader = self.shaders[shaderId] else {
                print("setShader: Shader is nil.")
                return
            }

            self.currentShader = shader
            renderCommandEncoder.setRenderPipelineState(shader.pipelineState)
        } else if (self.computeCommandEncoders[commandListId] != nil) {
            guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
                print("setShader: Compute command encoder is nil.")
                return
            }

            guard let shader = self.shaders[shaderId] else {
                print("setShader: Shader is nil.")
                return
            }

            self.currentShader = shader
            computeCommandEncoder.setComputePipelineState(shader.computePipelineState)
        }
    }

    public func setShaderBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            print("setShaderBuffer: Shader is nil.")
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
            print("setShaderBuffers: Shader is nil.")
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var bufferList: [MTLBuffer] = []
        var offsets: [Int] = []

        for i in 0..<graphicsBufferIdList.count {
            bufferList.append(self.graphicsBuffers[UInt(graphicsBufferIdList[i])]!)
            offsets.append(0)
        }

        argumentEncoder.setBuffers(bufferList, offsets: offsets, range: (slot + index)..<(slot + index) + graphicsBufferIdList.count)
    }

    public func setShaderTexture(_ commandListId: UInt, _ textureId: UInt, _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            print("setShaderTexture: Shader is nil.")
            return
        }

        guard let texture = self.textures[textureId] else {
            print("setShaderTexture: Texture is nil.")
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        argumentEncoder.setTexture(texture, index: slot)
    }

    public func setShaderTextures(_ commandListId: UInt, _ textureIdList: [UInt32], _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            print("setShaderTextures: Shader is nil.")
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var textureList: [MTLTexture] = []

        for i in 0..<textureIdList.count {
            textureList.append(self.textures[UInt(textureIdList[i])]!)
        }

        argumentEncoder.setTextures(textureList, range: (slot + index)..<(slot + index) + textureIdList.count)
    }

    public func setShaderIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ slot: Int, _ index: Int) {
        guard let computeCommandEncoder = self.computeCommandEncoders[commandListId] else {
            print("setShader: Compute command encoder is nil.")
            return
        }

        guard let shader = self.currentShader else {
            print("setShaderIndirectCommandList: Shader is nil.")
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

    public func executeIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int) {
        if (self.currentShader == nil) {
            return
        }

        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("executeIndirectCommandList: Render command encoder is nil.")
            return
        }

        guard let indirectBuffer = self.indirectCommandBuffers[indirectCommandListId] else {
            print("setShaderIndirectCommandList: Indirect buffer is nil.")
            return
        }

        renderCommandEncoder.setVertexBuffer(nil, offset: 0, index: 0)
        renderCommandEncoder.setFragmentBuffer(nil, offset: 0, index: 0)
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

        if (primitiveType.rawValue == 1) {
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

    public func presentScreenBuffer() {
        if (self.currentMetalDrawable != nil) {
            guard let currentMetalDrawable = self.currentMetalDrawable else {
                print("Error: Current Metal Drawable is null.")
                return
            }

            self.commandBuffer.present(currentMetalDrawable)
            self.commandBuffer.commit()

            // TODO: Can we reuse the same command buffer?
            self.commandBuffer = self.commandQueue.makeCommandBuffer()
        }
    }
}

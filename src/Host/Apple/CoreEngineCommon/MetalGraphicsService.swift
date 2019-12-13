import Metal
import QuartzCore.CAMetalLayer
import simd
import CoreEngineCommonInterop

class Shader {
    let pipelineState: MTLRenderPipelineState
    let argumentEncoder: MTLArgumentEncoder
    var argumentBuffers: [MTLBuffer]
    let argumentBuffersMaxCount = 1000
    var argumentBufferCurrentIndex = 0
    var currentArgumentBuffer: MTLBuffer

    init(_ device: MTLDevice, _ pipelineState: MTLRenderPipelineState, _ argumentEncoder: MTLArgumentEncoder) {
        self.pipelineState = pipelineState
        self.argumentEncoder = argumentEncoder

        self.argumentBuffers = []

        for i in 0..<argumentBuffersMaxCount {
            let argumentBuffer = device.makeBuffer(length: argumentEncoder.encodedLength)!
            argumentBuffer.label = (pipelineState.label != nil) ? "\(pipelineState.label!)Buffer\(i)" : "ShaderBuffer\(i)"
            self.argumentBuffers.append(argumentBuffer)
        }

        self.currentArgumentBuffer = argumentBuffers[0]
        argumentEncoder.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
    }

    func setupArgumentBuffer()
    {
        argumentBufferCurrentIndex += 1

        if (argumentBufferCurrentIndex == argumentBuffersMaxCount) {
            argumentBufferCurrentIndex = 0
        }

        self.currentArgumentBuffer = argumentBuffers[argumentBufferCurrentIndex]
        argumentEncoder.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
    }
}

public class MetalGraphicsService: GraphicsServiceProtocol {
    let device: MTLDevice
    let metalLayer: CAMetalLayer
    var currentMetalDrawable: CAMetalDrawable?
    var depthTextures: [MTLTexture]
    var currentDepthTextureIndex: Int
    var currentIndexBuffer: MTLBuffer!
    var currentShader: Shader? = nil

    var renderWidth: Int
    var renderHeight: Int

    var commandQueue: MTLCommandQueue!
    var commandBuffer: MTLCommandBuffer!
    var globalHeap: MTLHeap!

    var depthStencilState: MTLDepthStencilState!

    var shaders: [UInt: Shader]

    var copyCommandBuffers: [UInt: MTLCommandBuffer]
    var copyCommandEncoders: [UInt: MTLBlitCommandEncoder]

    var renderCommandBuffers: [UInt: MTLCommandBuffer]
    var renderCommandEncoders: [UInt: MTLRenderCommandEncoder]

    var graphicsBuffers: [UInt: MTLBuffer]
    var cpuGraphicsBuffers: [UInt: MTLBuffer]
    var graphicsBufferEncoders: [UInt: MTLArgumentEncoder]

    var textures: [UInt: MTLTexture]

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

        self.depthTextures = []
        self.currentDepthTextureIndex = 0

        self.shaders = [:]
        self.copyCommandBuffers = [:]
        self.copyCommandEncoders = [:]
        self.renderCommandBuffers = [:]
        self.renderCommandEncoders = [:]
        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]
        self.graphicsBufferEncoders = [:]
        self.textures = [:]

        let depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthStencilState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()
        self.commandBuffer = self.commandQueue.makeCommandBuffer()

        let heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .private
        heapDescriptor.type = .automatic // TODO: Switch to placement mode for manual memory management
        heapDescriptor.size = 1024 * 1024 * 1024; // Allocate 1GB for now
        self.globalHeap = self.device.makeHeap(descriptor: heapDescriptor)!

        createDepthBuffers()
    }

    public func getRenderSize() -> Vector2 {
        return Vector2(X: Float(self.renderWidth), Y: Float(self.renderHeight))
    }

    public func changeRenderSize(renderWidth: Int, renderHeight: Int) {
        self.renderWidth = renderWidth
        self.renderHeight = renderHeight
        
        self.metalLayer.drawableSize = CGSize(width: renderWidth, height: renderHeight)
        createDepthBuffers()
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

    public func createTexture(_ textureId: UInt, _ width: Int, _ height: Int, _ debugName: String?) -> Bool {
        // TODO: Check for errors
        let descriptor = MTLTextureDescriptor()

        descriptor.textureType = .type2D
        descriptor.pixelFormat = .rgba8Unorm_srgb
        descriptor.width = width
        descriptor.height = height
        descriptor.depth = 1
        descriptor.mipmapLevelCount = 1
        descriptor.arrayLength = 1
        descriptor.storageMode = .private

        let gpuTexture = self.globalHeap.makeTexture(descriptor: descriptor)!
        gpuTexture.label = (debugName != nil) ? debugName! : "Texture\(textureId)"

        self.textures[textureId] = gpuTexture
        return true
    }

    public func createShader(_ shaderId: UInt, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int, _ debugName: String?) -> Bool {
        let dispatchData = DispatchData(bytes: UnsafeRawBufferPointer(start: shaderByteCode, count: shaderByteCodeLength))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")!
        let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")!

        // Configure a pipeline descriptor that is used to create a pipeline state
        // TODO: Pass values from the function parameters
        let pipelineStateDescriptor = MTLRenderPipelineDescriptor()
        pipelineStateDescriptor.label = (debugName != nil) ? debugName! : "Shader\(shaderId)"
        pipelineStateDescriptor.vertexFunction = vertexFunction
        pipelineStateDescriptor.fragmentFunction = fragmentFunction
        pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.metalLayer.pixelFormat
        pipelineStateDescriptor.depthAttachmentPixelFormat = .depth32Float

        do {
            let pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
            let argumentEncoder = vertexFunction.makeArgumentEncoder(bufferIndex: 0)

            self.shaders[shaderId] = Shader(self.device, pipelineState, argumentEncoder)
            return true
        } catch {
            print("Failed to created pipeline state, \(error)")
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

    public func createRenderCommandList(_ commandListId: UInt, _ debugName: String?, _ createNewCommandBuffer: Bool) -> Bool {
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
        // TODO: Move that static declarations to other functions
        guard let currentMetalDrawable = self.currentMetalDrawable else {
            return false
        }

        let depthTexture = self.depthTextures[0]

        let renderPassDescriptor = MTLRenderPassDescriptor()
        renderPassDescriptor.colorAttachments[0].texture = currentMetalDrawable.texture
        renderPassDescriptor.colorAttachments[0].loadAction = .load // TODO: Use don't care for the final render target
        renderPassDescriptor.colorAttachments[0].storeAction = .store
        renderPassDescriptor.colorAttachments[0].clearColor = MTLClearColor.init(red: 0.0, green: 0.215, blue: 1.0, alpha: 1.0)
        renderPassDescriptor.depthAttachment.texture = depthTexture
        renderPassDescriptor.depthAttachment.loadAction = .clear // TODO: Use a separate pass for depth buffer
        renderPassDescriptor.depthAttachment.storeAction = .dontCare
        renderPassDescriptor.depthAttachment.clearDepth = 1.0
        
        let renderCommandEncoder = commandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        renderCommandEncoder.label = (debugName != nil) ? debugName! : "Render Command Encoder \(commandListId)"
        self.renderCommandEncoders[commandListId] = renderCommandEncoder

        renderCommandEncoder.setDepthStencilState(self.depthStencilState)
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

    public func setShader(_ commandListId: UInt, _ shaderId: UInt) {
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

        shader.argumentEncoder.setBuffer(graphicsBuffer, offset: index, index: slot)
    }

    public func setShaderBuffers(_ commandListId: UInt, _ graphicsBufferIdList: [UInt32], _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            print("setShaderBuffers: Shader is nil.")
            return
        }

        var bufferList: [MTLBuffer] = []
        var offsets: [Int] = []

        for i in 0..<graphicsBufferIdList.count {
            bufferList.append(self.graphicsBuffers[UInt(graphicsBufferIdList[i])]!)
            offsets.append(0)
        }

        shader.argumentEncoder.setBuffers(bufferList, offsets: offsets, range: (slot + index)..<(slot + index) + graphicsBufferIdList.count)
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

        shader.argumentEncoder.setTexture(texture, index: slot)
    }

    public func setShaderTextures(_ commandListId: UInt, _ textureIdList: [UInt32], _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            print("setShaderTextures: Shader is nil.")
            return
        }

        var textureList: [MTLTexture] = []

        for i in 0..<textureIdList.count {
            textureList.append(self.textures[UInt(textureIdList[i])]!)
        }

        shader.argumentEncoder.setTextures(textureList, range: (slot + index)..<(slot + index) + textureIdList.count)
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

            self.commandBuffer = self.commandQueue.makeCommandBuffer()
        }

        guard let nextCurrentMetalDrawable = self.metalLayer.nextDrawable() else {
            return
        }

        self.currentMetalDrawable = nextCurrentMetalDrawable
    }

    private func createDepthBuffers() {
        // TODO: Create an array per render buffer count

        let depthTextureDescriptor = MTLTextureDescriptor.texture2DDescriptor(pixelFormat: .depth32Float, width: self.renderWidth, height: self.renderHeight, mipmapped: false)
        depthTextureDescriptor.storageMode = .private
        depthTextureDescriptor.usage = .renderTarget
        let depthTexture = self.globalHeap.makeTexture(descriptor: depthTextureDescriptor)!
        self.depthTextures = []
        self.depthTextures.append(depthTexture)
    }
}

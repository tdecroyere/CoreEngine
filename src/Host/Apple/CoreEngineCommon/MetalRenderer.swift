import Metal
import QuartzCore.CAMetalLayer
import simd
import CoreEngineCommonInterop

public class MetalRenderer: GraphicsServiceProtocol {
    let device: MTLDevice
    let metalLayer: CAMetalLayer
    var currentMetalDrawable: CAMetalDrawable?
    var depthTextures: [MTLTexture]
    var currentDepthTextureIndex: Int

    var renderWidth: Int
    var renderHeight: Int

    var serialQueue: DispatchQueue
    var commandQueue: MTLCommandQueue!
    var globalHeap: MTLHeap!

    var depthStencilState: MTLDepthStencilState!

    var currentPipelineStateId: UInt
    var pipelineStates: [UInt: MTLRenderPipelineState]

    var argumentBuffer: MTLBuffer!

    var currentCopyCommandListId: UInt
    var copyCommandBuffers: [UInt: MTLCommandBuffer]
    var copyCommandEncoders: [UInt: MTLBlitCommandEncoder]

    var currentRenderCommandListId: UInt
    var renderCommandBuffers: [UInt: MTLCommandBuffer]
    var renderCommandEncoders: [UInt: MTLRenderCommandEncoder]

    var graphicsBuffers: [UInt: MTLBuffer]
    var cpuGraphicsBuffers: [UInt: MTLBuffer]

    var textures: [UInt: MTLTexture]

    public init(view: MetalView, renderWidth: Int, renderHeight: Int) {
        let defaultDevice = MTLCreateSystemDefaultDevice()!
        print(defaultDevice.name)
        print(renderWidth)

        self.device = defaultDevice
        self.renderWidth = renderWidth
        self.renderHeight = renderHeight

        self.serialQueue = DispatchQueue(label: "SerialQueue")

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

        self.pipelineStates = [:]
        self.currentPipelineStateId = 0

        self.copyCommandBuffers = [:]
        self.copyCommandEncoders = [:]
        self.currentCopyCommandListId = 0

        self.renderCommandBuffers = [:]
        self.renderCommandEncoders = [:]
        self.currentRenderCommandListId = 0

        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]

        self.textures = [:]

        let depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthStencilState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()

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

    var vertexFunction: MTLFunction?

    public func createPipelineState(_ shaderByteCodeData: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UInt {
        var currentPipelineStateId: UInt = 0

        serialQueue.sync {
            self.currentPipelineStateId += 1
            currentPipelineStateId = self.currentPipelineStateId
        }

        let dispatchData = DispatchData(bytes: UnsafeRawBufferPointer(start: shaderByteCodeData, count: shaderByteCodeLength))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")
        let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

        if (self.vertexFunction == nil) {
            self.vertexFunction = vertexFunction
        }

        // Init vertex layout
        // TODO: Pass the layout to the create method
        let vertexDescriptor = MTLVertexDescriptor()
        vertexDescriptor.attributes[0].format = .float3
        vertexDescriptor.attributes[0].offset = 0
        vertexDescriptor.attributes[0].bufferIndex = 0

        vertexDescriptor.attributes[1].format = .float3
        vertexDescriptor.attributes[1].offset = 12
        vertexDescriptor.attributes[1].bufferIndex = 0

        vertexDescriptor.layouts[0].stride = 24

        // Configure a pipeline descriptor that is used to create a pipeline state
        // TODO: Pass values from the function parameters
        let pipelineStateDescriptor = MTLRenderPipelineDescriptor()
        pipelineStateDescriptor.label = "Shader Pipeline"
        pipelineStateDescriptor.vertexDescriptor = vertexDescriptor
        pipelineStateDescriptor.vertexFunction = vertexFunction
        pipelineStateDescriptor.fragmentFunction = fragmentFunction
        pipelineStateDescriptor.vertexBuffers[0].mutability = .immutable
        pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.metalLayer.pixelFormat
        pipelineStateDescriptor.depthAttachmentPixelFormat = .depth32Float

        do {
            let pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
            self.pipelineStates[currentPipelineStateId] = pipelineState
            return currentPipelineStateId
        } catch {
            print("Failed to created pipeline state, \(error)")
        }

        return 0
    }

    public func removePipelineState(_ pipelineStateId: UInt) {
        self.pipelineStates[self.currentPipelineStateId] = nil
    }

    // TODO: Use a more precise structure to define buffer layouts
    public func createShaderParameters(_ graphicsResourceId: UInt, _ pipelineStateId: UInt, _ graphicsBuffer1: UInt, _ graphicsBuffer2: UInt, _ graphicsBuffer3: UInt) -> Bool {
        // TODO: Check for errors
        // TODO: Use the correct vertex function associated with the pipeline state

        //let pipelineState = self.pipelineStates[self.currentPipelineStateId];
        guard let vertexFunction = self.vertexFunction else {
            return false
        }

        let graphicsBufferIdList = [UInt(graphicsBuffer1), UInt(graphicsBuffer2), UInt(graphicsBuffer3)]
        let argumentEncoder = vertexFunction.makeArgumentEncoder(bufferIndex: 1)
        self.argumentBuffer = self.device.makeBuffer(length: argumentEncoder.encodedLength)!
        self.argumentBuffer.label = "Vertex Argument Buffer"

        argumentEncoder.setArgumentBuffer(argumentBuffer, offset: 0)

        for i in 0..<graphicsBufferIdList.count {
            let graphicsBuffer = self.graphicsBuffers[graphicsBufferIdList[i]]
            argumentEncoder.setBuffer(graphicsBuffer, offset: 0, index: i)
        }

        self.graphicsBuffers[graphicsResourceId] = argumentBuffer
        return true
    }

    public func createGraphicsBuffer(_ graphicsResourceId: UInt, _ length: Int) -> Bool {
        // TODO: Page Align the length to avoid the copy of the buffer later
        // TODO: Check for errors

        // Create a the metal buffer on the CPU
        let cpuBuffer = self.device.makeBuffer(length: length, options: .cpuCacheModeWriteCombined)!
        cpuBuffer.label = "Dynamic Graphics Buffer - CPU buffer"

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: length, options: .storageModePrivate)!
        gpuBuffer.label = "Dynamic Graphics Buffer"

        self.graphicsBuffers[graphicsResourceId] = gpuBuffer
        self.cpuGraphicsBuffers[graphicsResourceId] = cpuBuffer

        return true
    }

    public func createTexture(_ graphicsResourceId: UInt, _ width: Int, _ height: Int) -> Bool {
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
        gpuTexture.label = "Texture Graphics Buffer"

        self.textures[graphicsResourceId] = gpuTexture
        return true
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

    public func createCopyCommandList() -> UInt {
        var currentCopyCommandListId: UInt = 0

        serialQueue.sync {
            self.currentCopyCommandListId += 1
            currentCopyCommandListId = self.currentCopyCommandListId
        }

        guard let copyCommandBuffer = self.commandQueue.makeCommandBuffer() else {
            print("ERROR creating copy command buffer.")
            return 0
        }

        copyCommandBuffer.label = "Copy Command Buffer \(currentCopyCommandListId)"
        self.copyCommandBuffers[currentCopyCommandListId] = copyCommandBuffer

        guard let copyCommandEncoder = copyCommandBuffer.makeBlitCommandEncoder() else {
            print("ERROR creating copy command encoder.")
            return 0
        }

        copyCommandEncoder.label = "Copy Command Encoder \(currentCopyCommandListId)"
        self.copyCommandEncoders[currentCopyCommandListId] = copyCommandEncoder

        return currentCopyCommandListId
    }

    public func executeCopyCommandList(_ commandListId: UInt) {
        guard let copyCommandEncoder = self.copyCommandEncoders[commandListId] else {
            print("executeCopyCommandList: Copy command encoder is nil.")
            return
        }

        copyCommandEncoder.endEncoding()
        self.copyCommandEncoders[commandListId] = nil

        guard let copyCommandBuffer = self.copyCommandBuffers[commandListId] else {
            print("executeCopyCommandList: Copy command buffer is nil.")
            return
        }

        copyCommandBuffer.commit()
        self.copyCommandBuffers[commandListId] = nil
    }

    public func createRenderCommandList() -> UInt {
        var currentRenderCommandListId: UInt = 0

        serialQueue.sync {
            self.currentRenderCommandListId += 1
            currentRenderCommandListId = self.currentRenderCommandListId
        }

        // Create command buffer
        let renderCommandBuffer = self.commandQueue.makeCommandBuffer()!
        renderCommandBuffer.label = "Render Command Buffer \(currentRenderCommandListId)"
        self.renderCommandBuffers[currentRenderCommandListId] = renderCommandBuffer

        // Create render command encoder
        // TODO: Move that static declarations to other functions
        guard let currentMetalDrawable = self.currentMetalDrawable else {
            return 0
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
        
        let renderCommandEncoder = renderCommandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        renderCommandEncoder.label = "Render Command Encoder \(self.currentRenderCommandListId)"
        self.renderCommandEncoders[currentRenderCommandListId] = renderCommandEncoder

        renderCommandEncoder.setDepthStencilState(self.depthStencilState)
        renderCommandEncoder.setCullMode(.back)

        let renderSize = getRenderSize()
        renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderSize.X), height: Double(renderSize.Y), znear: -1.0, zfar: 1.0))
        
        renderCommandEncoder.useHeap(self.globalHeap)

        return currentRenderCommandListId
    }

    public func executeRenderCommandList(_ commandListId: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("execureRenderCommandList: Render command encoder is nil.")
            return
        }

        renderCommandEncoder.endEncoding()
        self.renderCommandEncoders[commandListId] = nil

        guard let renderCommandBuffer = self.renderCommandBuffers[commandListId] else {
            print("execureRenderCommandList: Render command buffer is nil.")
            return
        }

        renderCommandBuffer.commit()
        self.renderCommandBuffers[commandListId] = nil
    }

    public func presentScreenBuffer() {
        if (self.currentMetalDrawable != nil) {
            guard let currentMetalDrawable = self.currentMetalDrawable else {
                print("Error: Current Metal Drawable is null.")
                return
            }

            let presentCommandBuffer = self.commandQueue.makeCommandBuffer()!
            presentCommandBuffer.label = "Present Render Command Buffer"
            presentCommandBuffer.present(currentMetalDrawable)
            presentCommandBuffer.commit()
        }

        guard let nextCurrentMetalDrawable = self.metalLayer.nextDrawable() else {
            return
        }

        self.currentMetalDrawable = nextCurrentMetalDrawable
    }

    public func setPipelineState(_ commandListId: UInt, _ pipelineStateId: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("drawPrimitives: Render command encoder is nil.")
            return
        }

        guard let pipelineState = self.pipelineStates[pipelineStateId] else {
            print("drawPrimitives: Pipelinestate is nil.")
            return
        }

        renderCommandEncoder.setRenderPipelineState(pipelineState)
    }

    public func setGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("setGraphicsBuffer: Render command encoder is nil.")
            return
        }

        guard let graphicsBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("setGraphicsBuffer: Graphics buffer is nil.")
            return
        }

        if (graphicsBindStage.rawValue == 0) {
            renderCommandEncoder.setVertexBuffer(graphicsBuffer, offset: 0, index: Int(slot))
        } else if (graphicsBindStage.rawValue == 1) {
            renderCommandEncoder.setFragmentBuffer(graphicsBuffer, offset: 0, index: Int(slot))
        }
    }

    public func setTexture(_ commandListId: UInt, _ textureId: UInt, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("setTexture: Render command encoder is nil.")
            return
        }

        guard let texture = self.textures[textureId] else {
            print("setTexture: Texture is nil.")
            return
        }

        if (graphicsBindStage.rawValue == 0) {
            renderCommandEncoder.setVertexTexture(texture, index: Int(slot))
        } else if (graphicsBindStage.rawValue == 1) {
            renderCommandEncoder.setFragmentTexture(texture, index: Int(slot))
        }
    }

    public func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ vertexBufferId: UInt, _ indexBufferId: UInt, _ instanceCount: Int, _ baseInstanceId: Int) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("drawPrimitives: Render command encoder is nil.")
            return
        }

        let vertexGraphicsBuffer = self.graphicsBuffers[vertexBufferId]
        let indexGraphicsBuffer = self.graphicsBuffers[indexBufferId]
        
        let startIndexOffset = Int(startIndex * 4)

        renderCommandEncoder.setVertexBuffer(vertexGraphicsBuffer!, offset: 0, index: 0)

        var primitiveTypeMetal = MTLPrimitiveType.triangle

        if (primitiveType.rawValue == 1) {
            primitiveTypeMetal = MTLPrimitiveType.line
        }

        renderCommandEncoder.drawIndexedPrimitives(type: primitiveTypeMetal, 
                                                   indexCount: Int(indexCount), 
                                                   indexType: .uint32, 
                                                   indexBuffer: indexGraphicsBuffer!, 
                                                   indexBufferOffset: startIndexOffset, 
                                                   instanceCount: instanceCount, 
                                                   baseVertex: 0, 
                                                   baseInstance: Int(baseInstanceId))
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

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
    
    var commandQueue: MTLCommandQueue!
    var pipelineState: MTLRenderPipelineState?
    var depthStencilState: MTLDepthStencilState!

    var vertexFunction: MTLFunction!
    var fragmentFunction: MTLFunction!
    var argumentBuffer: MTLBuffer!

    var currentCopyCommandListId: UInt
    var copyCommandBuffers: [UInt: MTLCommandBuffer]
    var copyCommandEncoders: [UInt: MTLBlitCommandEncoder]

    var currentRenderCommandListId: UInt
    var renderCommandBuffers: [UInt: MTLCommandBuffer]
    var renderCommandEncoders: [UInt: MTLRenderCommandEncoder]

    var globalHeap: MTLHeap!
    var graphicsBuffers: [UInt: MTLBuffer]
    var cpuGraphicsBuffers: [UInt: MTLBuffer]
    var currentGraphicsBufferId: UInt

    var serialQueue: DispatchQueue

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

        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]
        self.currentGraphicsBufferId = 0

        self.copyCommandBuffers = [:]
        self.copyCommandEncoders = [:]
        self.currentCopyCommandListId = 0

        self.renderCommandBuffers = [:]
        self.renderCommandEncoders = [:]
        self.currentRenderCommandListId = 0

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

    public func createShader(_ shaderByteCodeData: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UInt {
        let dispatchData = DispatchData(bytesNoCopy: UnsafeRawBufferPointer(start: shaderByteCodeData, count: shaderByteCodeLength))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        self.vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")
        self.fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

        // Init vertex layout
        let vertexDescriptor = MTLVertexDescriptor()
        vertexDescriptor.attributes[0].format = .float3
        vertexDescriptor.attributes[0].offset = 0
        vertexDescriptor.attributes[0].bufferIndex = 0

        vertexDescriptor.attributes[1].format = .float3
        vertexDescriptor.attributes[1].offset = 12
        vertexDescriptor.attributes[1].bufferIndex = 0

        vertexDescriptor.layouts[0].stride = 24

        // Configure a pipeline descriptor that is used to create a pipeline state
        let pipelineStateDescriptor = MTLRenderPipelineDescriptor()
        pipelineStateDescriptor.label = "Simple Pipeline"
        pipelineStateDescriptor.vertexDescriptor = vertexDescriptor
        pipelineStateDescriptor.vertexFunction = vertexFunction
        pipelineStateDescriptor.fragmentFunction = fragmentFunction
        pipelineStateDescriptor.vertexBuffers[0].mutability = .immutable
        pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.metalLayer.pixelFormat
        pipelineStateDescriptor.depthAttachmentPixelFormat = .depth32Float

        do {
            self.pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
        } catch {
            print("Failed to created pipeline state, \(error)")
        }

        return 0
    }

    // TODO: Use a more precise structure to define buffer layouts
    public func createShaderParameters(_ graphicsBuffer1: UInt, _ graphicsBuffer2: UInt, _ graphicsBuffer3: UInt) -> UInt {
        let graphicsBufferIdList = [UInt(graphicsBuffer1), UInt(graphicsBuffer2), UInt(graphicsBuffer3)]
        let argumentEncoder = self.vertexFunction!.makeArgumentEncoder(bufferIndex: 1)
        self.argumentBuffer = self.device.makeBuffer(length: argumentEncoder.encodedLength)!
        self.argumentBuffer.label = "Vertex Argument Buffer"

        argumentEncoder.setArgumentBuffer(argumentBuffer, offset: 0)

        for i in 0..<graphicsBufferIdList.count {
            let graphicsBuffer = self.graphicsBuffers[graphicsBufferIdList[i]]
            argumentEncoder.setBuffer(graphicsBuffer, offset: 0, index: i)
        }

        return 0
    }

    public func createGraphicsBuffer(_ length: Int) -> UInt {
        // TODO: Page Align the length to avoid the copy of the buffer later
        serialQueue.sync {
            self.currentGraphicsBufferId += 1
        }

        // Create a the metal buffer on the CPU
        let cpuBuffer = self.device.makeBuffer(length: length, options: .cpuCacheModeWriteCombined)!
        cpuBuffer.label = "Dynamic Graphics Buffer - CPU buffer"

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: length, options: .storageModePrivate)!
        gpuBuffer.label = "Dynamic Graphics Buffer"

        self.graphicsBuffers[self.currentGraphicsBufferId] = gpuBuffer
        self.cpuGraphicsBuffers[self.currentGraphicsBufferId] = cpuBuffer

        return self.currentGraphicsBufferId
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

    public func createCopyCommandList() -> UInt {
        serialQueue.sync {
            self.currentCopyCommandListId += 1
        }

        guard let copyCommandBuffer = self.commandQueue.makeCommandBuffer() else {
            print("ERROR creating copy command buffer.")
            return 0
        }

        copyCommandBuffer.label = "Copy Command Buffer \(self.currentCopyCommandListId)"
        self.copyCommandBuffers[self.currentCopyCommandListId] = copyCommandBuffer

        guard let copyCommandEncoder = copyCommandBuffer.makeBlitCommandEncoder() else {
            print("ERROR creating copy command encoder.")
            return 0
        }

        copyCommandEncoder.label = "Copy Command Encoder \(self.currentCopyCommandListId)"
        self.copyCommandEncoders[self.currentCopyCommandListId] = copyCommandEncoder

        return self.currentCopyCommandListId
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
        serialQueue.sync {
            self.currentRenderCommandListId += 1
        }

        // Create command buffer
        let renderCommandBuffer = self.commandQueue.makeCommandBuffer()!
        renderCommandBuffer.label = "Render Command Buffer \(self.currentRenderCommandListId)"
        self.renderCommandBuffers[self.currentRenderCommandListId] = renderCommandBuffer

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

        guard let pipelineState = self.pipelineState else {
            print("pipeline state empty")
            return 0
        }
        
        let renderCommandEncoder = renderCommandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        renderCommandEncoder.label = "Render Command Encoder \(self.currentRenderCommandListId)"
        self.renderCommandEncoders[self.currentRenderCommandListId] = renderCommandEncoder

        renderCommandEncoder.setDepthStencilState(self.depthStencilState)
        renderCommandEncoder.setCullMode(.back)

        let renderSize = getRenderSize()
        renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderSize.X), height: Double(renderSize.Y), znear: -1.0, zfar: 1.0))
        renderCommandEncoder.setRenderPipelineState(pipelineState)

        renderCommandEncoder.useHeap(self.globalHeap)

        return self.currentRenderCommandListId
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

    public func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: UInt, _ indexCount: UInt, _ vertexBufferId: UInt, _ indexBufferId: UInt, _ baseInstanceId: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoders[commandListId] else {
            print("drawPrimitives: Render command encoder is nil.")
            return
        }

        let vertexGraphicsBuffer = self.graphicsBuffers[vertexBufferId]
        let indexGraphicsBuffer = self.graphicsBuffers[indexBufferId]
        
        let startIndexOffset = Int(startIndex * 4)

        renderCommandEncoder.setVertexBuffer(vertexGraphicsBuffer!, offset: 0, index: 0)
        renderCommandEncoder.setVertexBuffer(self.argumentBuffer, offset: 0, index: 1)

        var primitiveTypeMetal = MTLPrimitiveType.triangle

        if (primitiveType.rawValue == 1) {
            primitiveTypeMetal = MTLPrimitiveType.line
        }

        renderCommandEncoder.drawIndexedPrimitives(type: primitiveTypeMetal, 
                                                   indexCount: Int(indexCount), 
                                                   indexType: .uint32, 
                                                   indexBuffer: indexGraphicsBuffer!, 
                                                   indexBufferOffset: startIndexOffset, 
                                                   instanceCount: 1, 
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

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
    
    var renderCommandBuffer: MTLCommandBuffer!
    var renderCommandEncoder: MTLRenderCommandEncoder!
    var copyCommandBuffer: MTLCommandBuffer!
    var copyCommandEncoder: MTLBlitCommandEncoder!

    var vertexFunction: MTLFunction!
    var fragmentFunction: MTLFunction!

    var argumentBuffer: MTLBuffer!

    var globalHeap: MTLHeap!
    var graphicsBuffers: [UInt: MTLBuffer]
    var cpuGraphicsBuffers: [UInt: MTLBuffer]
    var graphicsBuffersToCopy: [UInt]
    var currentGraphicsBufferId: UInt

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

        self.renderCommandBuffer = nil
        self.copyCommandBuffer = nil
        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]
        self.graphicsBuffersToCopy = []
        self.currentGraphicsBufferId = 0;

        // super.init()

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

    public func createStaticGraphicsBuffer(_ data: UnsafeMutableRawPointer, _ length: Int) -> UInt {
        self.currentGraphicsBufferId += 1

        // TODO: Not thread-safe!!
        // TODO: Re-use temporary cpu buffers
        
        // Create a the metal buffer on the CPU
        let cpuBuffer = self.device.makeBuffer(bytes: data, length: length, options: .cpuCacheModeWriteCombined)!
        // TODO: Cannot avoid copy for now because the copy is deffered and the memory is only temporary pinned by dotnet
        //let cpuBuffer = self.device.makeBuffer(bytesNoCopy: data, length: Int(length), options: .cpuCacheModeWriteCombined, deallocator: nil)!
        cpuBuffer.label = "Static Graphics Buffer - CPU buffer"

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: length, options: .storageModePrivate)!
        gpuBuffer.label = "Static Graphics Buffer"

        self.graphicsBuffers[self.currentGraphicsBufferId] = gpuBuffer
        self.cpuGraphicsBuffers[self.currentGraphicsBufferId] = cpuBuffer
        self.graphicsBuffersToCopy.append(self.currentGraphicsBufferId)

        return self.currentGraphicsBufferId
    }

    public func createDynamicGraphicsBuffer(_ length: Int) -> UInt {
        self.currentGraphicsBufferId += 1

        // TODO: Not thread-safe!!
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

    public func uploadDataToGraphicsBuffer(_ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ length: Int) {
        guard let gpuBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("ERROR: GPU graphics buffer was not found")
            return
        }

        guard let cpuBuffer = self.cpuGraphicsBuffers[graphicsBufferId] else {
            print("ERROR: CPU graphics buffer was not found")
            return
        }

        cpuBuffer.contents().copyMemory(from: data.assumingMemoryBound(to: UInt8.self), byteCount: (length * MemoryLayout<UInt8>.stride))

        // TODO: Add parameters to be able to update partially the buffer
        self.copyCommandEncoder.copy(from: cpuBuffer, sourceOffset: 0, to: gpuBuffer, destinationOffset: 0, size: length)
    }

    public func beginCopyGpuData() {
        self.copyCommandBuffer = self.commandQueue.makeCommandBuffer()!
        self.copyCommandBuffer.label = "Copy Command Buffer"

        self.copyCommandEncoder = self.copyCommandBuffer.makeBlitCommandEncoder()!
        self.copyCommandEncoder.label = "Copy Command Encoder"

        // Copy pending static graphics buffers
        for i in 0..<self.graphicsBuffersToCopy.count {
            let cpuGraphicsBuffer = self.cpuGraphicsBuffers[self.graphicsBuffersToCopy[i]]!
            let gpuGraphicsBuffer = self.graphicsBuffers[self.graphicsBuffersToCopy[i]]!

            self.copyCommandEncoder.copy(from: cpuGraphicsBuffer, sourceOffset: 0, to: gpuGraphicsBuffer, destinationOffset: 0, size: gpuGraphicsBuffer.length)
            self.cpuGraphicsBuffers[self.graphicsBuffersToCopy[i]] = nil
        }

        self.graphicsBuffersToCopy = []
    }

    public func endCopyGpuData() {
        guard let copyCommandEncoder = self.copyCommandEncoder else {
            print("Error: Copy Command Encoder is null.")
            return
        }

        guard let copyCommandBuffer = self.copyCommandBuffer else {
            print("Error: Copy Command buffer is null.")
            return
        }

        copyCommandEncoder.endEncoding()
        self.copyCommandEncoder = nil

        copyCommandBuffer.commit()
        self.copyCommandBuffer = nil
    }

    public func beginRender() {
        guard let currentMetalDrawable = self.currentMetalDrawable else {
            return
        }

        let depthTexture = self.depthTextures[0]

        let renderPassDescriptor = MTLRenderPassDescriptor()
        renderPassDescriptor.colorAttachments[0].texture = currentMetalDrawable.texture
        renderPassDescriptor.colorAttachments[0].loadAction = .clear
        renderPassDescriptor.colorAttachments[0].storeAction = .dontCare
        renderPassDescriptor.colorAttachments[0].clearColor = MTLClearColor.init(red: 0.0, green: 0.215, blue: 1.0, alpha: 1.0)
        renderPassDescriptor.depthAttachment.texture = depthTexture
        renderPassDescriptor.depthAttachment.loadAction = .clear
        renderPassDescriptor.depthAttachment.storeAction = .dontCare
        renderPassDescriptor.depthAttachment.clearDepth = 1.0

        self.renderCommandBuffer = self.commandQueue.makeCommandBuffer()!
        self.renderCommandBuffer.label = "Frame Render Command Buffer"

        guard let pipelineState = self.pipelineState else {
            print("pipeline state empty")
            return
        }
        
        self.renderCommandEncoder = self.renderCommandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        self.renderCommandEncoder.label = "Frame Render Command Encoder"

        self.renderCommandEncoder.setDepthStencilState(self.depthStencilState)
        self.renderCommandEncoder.setCullMode(.back)

        let renderSize = getRenderSize()
        self.renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderSize.X), height: Double(renderSize.Y), znear: -1.0, zfar: 1.0))
        self.renderCommandEncoder.setRenderPipelineState(pipelineState)

        self.renderCommandEncoder.useHeap(self.globalHeap)
    }

    public func endRender() {
        guard let renderCommandEncoder = self.renderCommandEncoder else {
            print("Error: Render Command Encoder is null.")
            return
        }

        guard let renderCommandBuffer = self.renderCommandBuffer else {
            print("Error: Render Command buffer is null.")
            return
        }

        guard let currentMetalDrawable = self.currentMetalDrawable else {
            print("Error: Current Metal Drawable is null.")
            return
        }

        renderCommandEncoder.endEncoding()
        self.renderCommandEncoder = nil

        renderCommandBuffer.present(currentMetalDrawable)
        renderCommandBuffer.commit()
        
        self.renderCommandBuffer = nil
        self.currentMetalDrawable = nil
    }

    public func presentScreenBuffer() {
        guard let currentMetalDrawable = self.metalLayer.nextDrawable() else {
            return
        }

        self.currentMetalDrawable = currentMetalDrawable
    }

    public func drawPrimitives(_ startIndex: UInt, _ indexCount: UInt, _ vertexBufferId: UInt, _ indexBufferId: UInt, _ baseInstanceId: UInt) {
        guard let renderCommandEncoder = self.renderCommandEncoder else {
            print("Error: Render Command Encoder is null.")
            return
        }

        let vertexGraphicsBuffer = self.graphicsBuffers[vertexBufferId]
        let indexGraphicsBuffer = self.graphicsBuffers[indexBufferId]
        
        let startIndexOffset = Int(startIndex * 4)

        renderCommandEncoder.setVertexBuffer(vertexGraphicsBuffer!, offset: 0, index: 0)
        renderCommandEncoder.setVertexBuffer(self.argumentBuffer, offset: 0, index: 1)

        renderCommandEncoder.drawIndexedPrimitives(type: .triangle, 
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

import Cocoa
import Metal
import MetalKit
import simd
import CoreEngineInterop

class MacOSMetalRenderer: NSObject, MTKViewDelegate {
    let device: MTLDevice
    let mtkView: MTKView
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
    var graphicsBuffers: [UInt32: MTLBuffer]
    var cpuGraphicsBuffers: [UInt32: MTLBuffer]
    var graphicsBuffersToCopy: [UInt32]
    var currentGraphicsBufferId: UInt32

    init(view: MTKView, device: MTLDevice) {
        self.mtkView = view
        self.device = device
        self.renderCommandBuffer = nil
        self.copyCommandBuffer = nil
        self.graphicsBuffers = [:]
        self.cpuGraphicsBuffers = [:]
        self.graphicsBuffersToCopy = []
        self.currentGraphicsBufferId = 0;

        super.init()

        self.mtkView.device = device
        self.mtkView.isPaused = true
        self.mtkView.colorPixelFormat = .bgra8Unorm_srgb
        self.mtkView.depthStencilPixelFormat = .depth32Float

        let depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthStencilState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()

        let heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .`private`
        heapDescriptor.type = .automatic // TODO: Switch to placement mode for manual memory management
        heapDescriptor.size = 1024 * 1024 * 1024; // Allocate 1GB for now
        self.globalHeap = self.device.makeHeap(descriptor: heapDescriptor)!
    }

    func getRenderSize() -> Vector2 {
        return Vector2(X: Float(self.mtkView.drawableSize.width), Y: Float(self.mtkView.drawableSize.height))
    }

    func createShader(shaderByteCode: MemoryBuffer) {
        let dispatchData = DispatchData(bytesNoCopy: UnsafeRawBufferPointer(start: shaderByteCode.Pointer!, count: Int(shaderByteCode.Length)))
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
        pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.mtkView.colorPixelFormat
        pipelineStateDescriptor.depthAttachmentPixelFormat = self.mtkView.depthStencilPixelFormat

        do {
            self.pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
        } catch {
            print("Failed to created pipeline state, \(error)")
        }
    }

    // TODO: Use a more precise structure to define buffer layouts
    func createShaderParameters(_ graphicsBufferIdList: [UInt32]) -> UInt32 {
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

    func createStaticGraphicsBuffer(_ data: MemoryBuffer) -> UInt32 {
        self.currentGraphicsBufferId += 1

        // TODO: Re-use temporary cpu buffers
        
        // Create a the metal buffer on the CPU
        let cpuBuffer = self.device.makeBuffer(bytes: data.Pointer, length: Int(data.Length), options: .cpuCacheModeWriteCombined)!
        cpuBuffer.label = "Static Graphics Buffer - CPU buffer"

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: Int(data.Length), options: .storageModePrivate)!
        gpuBuffer.label = "Static Graphics Buffer"

        self.graphicsBuffers[self.currentGraphicsBufferId] = gpuBuffer
        self.cpuGraphicsBuffers[self.currentGraphicsBufferId] = cpuBuffer
        self.graphicsBuffersToCopy.append(self.currentGraphicsBufferId)

        return self.currentGraphicsBufferId
    }

    func createDynamicGraphicsBuffer(_ length: UInt32) -> MemoryBuffer {
        self.currentGraphicsBufferId += 1

        // Create a the metal buffer on the CPU
        let cpuBuffer = self.device.makeBuffer(length: Int(length), options: .cpuCacheModeWriteCombined)!
        cpuBuffer.label = "Dynamic Graphics Buffer - CPU buffer"

        // Create the metal buffer on the GPU
        let gpuBuffer = self.globalHeap.makeBuffer(length: Int(length), options: .storageModePrivate)!
        gpuBuffer.label = "Dynamic Graphics Buffer"

        self.graphicsBuffers[self.currentGraphicsBufferId] = gpuBuffer
        self.cpuGraphicsBuffers[self.currentGraphicsBufferId] = cpuBuffer

        return MemoryBuffer(Id: self.currentGraphicsBufferId, Pointer: cpuBuffer.contents().assumingMemoryBound(to: UInt8.self), Length: Int32(length))
    }

    func uploadDataToGraphicsBuffer(_ graphicsBufferId: UInt32, _ data: MemoryBuffer) {
        guard let gpuBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("ERROR: GPU graphics buffer was not found")
            return
        }

        guard let cpuBuffer = self.cpuGraphicsBuffers[graphicsBufferId] else {
            print("ERROR: CPU graphics buffer was not found")
            return
        }

        // TODO: Add parameters to be able to update partially the buffer
        self.copyCommandEncoder.copy(from: cpuBuffer, sourceOffset: 0, to: gpuBuffer, destinationOffset: 0, size: Int(data.Length))
    }

    func beginCopyGpuData() {
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

    func endCopyGpuData() {
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

    func beginRender() {
        self.mtkView.clearColor = MTLClearColor.init(red: 0.0, green: 0.215, blue: 1.0, alpha: 1.0)

        self.renderCommandBuffer = self.commandQueue.makeCommandBuffer()!
        self.renderCommandBuffer.label = "Frame Render Command Buffer"

        guard let pipelineState = self.pipelineState else {
            print("pipeline state empty")
            return
        }

        let renderPassDescriptor = self.mtkView.currentRenderPassDescriptor!
        
        self.renderCommandEncoder = self.renderCommandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        self.renderCommandEncoder.label = "Frame Render Command Encoder"

        self.renderCommandEncoder.setDepthStencilState(self.depthStencilState)
        self.renderCommandEncoder.setCullMode(.back)

        let renderSize = getRenderSize()
        self.renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderSize.X), height: Double(renderSize.Y), znear: -1.0, zfar: 1.0))
        self.renderCommandEncoder.setRenderPipelineState(pipelineState)

        self.renderCommandEncoder.useHeap(self.globalHeap)
    }

    func endRender() {
        guard let renderCommandEncoder = self.renderCommandEncoder else {
            print("Error: Render Command Encoder is null.")
            return
        }

        guard let renderCommandBuffer = self.renderCommandBuffer else {
            print("Error: Render Command buffer is null.")
            return
        }

        renderCommandEncoder.endEncoding()
        self.renderCommandEncoder = nil

        renderCommandBuffer.present(self.mtkView.currentDrawable!)
        renderCommandBuffer.commit()
        renderCommandBuffer.waitUntilScheduled()
        self.renderCommandBuffer = nil

        self.mtkView.draw()
    }

    func drawPrimitives(_ startIndex: UInt32, _ indexCount: UInt32, _ vertexBufferId: UInt32, _ indexBufferId: UInt32, _ baseInstanceId: UInt32) {
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

    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
    }

    func draw(in view: MTKView) {

    }
}

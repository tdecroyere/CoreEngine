import Cocoa
import Metal
import MetalKit
import simd
import CoreEngineInterop

struct ObjectConstantBuffer {
    var worldMatrix: float4x4

    init(_ worldMatrix: float4x4) {
        self.worldMatrix = worldMatrix
    }
}

func createShader(graphicsContext: UnsafeMutableRawPointer?, shaderByteCode: MemoryBuffer) -> UInt32 {
    //print("Swift create shader")
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.createShader(shaderByteCode: shaderByteCode)
    return 0
}

func createGraphicsBuffer(graphicsContext: UnsafeMutableRawPointer?, data: MemoryBuffer) -> UInt32 {
    //print("Swift create graphics buffer")
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createGraphicsBuffer(data: data)
}

func setRenderPassConstants(graphicsContext: UnsafeMutableRawPointer?, data: MemoryBuffer) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.setRenderPassConstants(data: data)
}

func drawPrimitives(graphicsContext: UnsafeMutableRawPointer?, primitiveCount: Int32, vertexBufferId: UInt32, indexBufferId: UInt32, worldMatrix: Matrix4x4) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()

    // TODO: Move world matrix setup to buffers
    let row1 = float4(worldMatrix.Item00, worldMatrix.Item01, worldMatrix.Item02, worldMatrix.Item03)
    let row2 = float4(worldMatrix.Item10, worldMatrix.Item11, worldMatrix.Item12, worldMatrix.Item13)
    let row3 = float4(worldMatrix.Item20, worldMatrix.Item21, worldMatrix.Item22, worldMatrix.Item23)
    let row4 = float4(worldMatrix.Item30, worldMatrix.Item31, worldMatrix.Item32, worldMatrix.Item33)
    
    let dstWorldMatrix = float4x4(rows: [row1, row2, row3, row4])

    renderer.drawPrimitives(primitiveCount, vertexBufferId, indexBufferId, dstWorldMatrix)
}


class MacOSMetalRenderer: NSObject, MTKViewDelegate {
    let device: MTLDevice
    let mtkView: MTKView
    var commandQueue: MTLCommandQueue!
    var pipelineState: MTLRenderPipelineState?
    var depthStencilState: MTLDepthStencilState!
    var currentCommandBuffer: MTLCommandBuffer!
    var currentRenderEncoder: MTLRenderCommandEncoder!

    var graphicsBuffers: [UInt32: MTLBuffer]
    var currentGraphicsBufferId: UInt32

    var viewportSize: float2
    
    init(view: MTKView, device: MTLDevice) {
        self.mtkView = view
        self.device = device
        self.viewportSize = float2(Float(view.drawableSize.width), Float(view.drawableSize.height))
        self.currentCommandBuffer = nil
        self.graphicsBuffers = [:]
        self.currentGraphicsBufferId = 0;

        super.init()

        self.mtkView.colorPixelFormat = .bgra8Unorm_srgb
        self.mtkView.depthStencilPixelFormat = .depth32Float

        let depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthStencilState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()
    }

    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
        self.viewportSize = simd_float2(Float(view.drawableSize.width), Float(view.drawableSize.height))
    }

    func createShader(shaderByteCode: MemoryBuffer) {
        let dispatchData = DispatchData(bytesNoCopy: UnsafeRawBufferPointer(start: shaderByteCode.Pointer!, count: Int(shaderByteCode.Length)))
        let defaultLibrary = try! self.device.makeLibrary(data: dispatchData as __DispatchData)

        let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")
        let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

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
        pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.mtkView.colorPixelFormat
        pipelineStateDescriptor.depthAttachmentPixelFormat = self.mtkView.depthStencilPixelFormat

        do {
            self.pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
        } catch {
            print("Failed to created pipeline state, \(error)")
        }
    }

    func createGraphicsBuffer(data: MemoryBuffer) -> UInt32 {
        self.currentGraphicsBufferId += 1
        self.graphicsBuffers[self.currentGraphicsBufferId] = self.device.makeBuffer(bytes: data.Pointer, length: Int(data.Length), options: .storageModeManaged)
        return self.currentGraphicsBufferId
    }

    func setRenderPassConstants(data: MemoryBuffer) {
        if (self.currentRenderEncoder != nil) {
            self.currentRenderEncoder.setVertexBytes(data.Pointer, length: Int(data.Length), index: 1)
        }
    }

    func drawPrimitives(_ primitiveCount: Int32, _ vertexBufferId: UInt32, _ indexBufferId: UInt32, _ worldMatrix: float4x4) {
        if (self.currentRenderEncoder != nil) {
            // TODO: Change the fact that we have only one command buffer stored in a private field

            // TODO: Switch to metal buffers
            var objectBuffer = ObjectConstantBuffer(worldMatrix)
            self.currentRenderEncoder.setVertexBytes(&objectBuffer, length: MemoryLayout<ObjectConstantBuffer>.size, index: 2)

            // Draw the 3 vertices of our triangle
            let vertexGraphicsBuffer = self.graphicsBuffers[vertexBufferId]
            let indexGraphicsBuffer = self.graphicsBuffers[indexBufferId]

            // TODO: Check for nullable buffers

            let indexComputedCount = Int(primitiveCount) * 3

            self.currentRenderEncoder!.setVertexBuffer(vertexGraphicsBuffer!, offset: 0, index: 0)
            self.currentRenderEncoder!.drawIndexedPrimitives(type: .triangle, indexCount: indexComputedCount, indexType: .uint32, indexBuffer: indexGraphicsBuffer!, indexBufferOffset: 0, instanceCount: 1, baseVertex: 0, baseInstance: 0)
        }
    }

    func beginRender() {
        self.mtkView.clearColor = MTLClearColor.init(red: 0.0, green: 0.215, blue: 1.0, alpha: 1.0)

        // Create a new command buffer for each render pass to the current drawable
        self.currentCommandBuffer = self.commandQueue.makeCommandBuffer()!
        self.currentCommandBuffer.label = "MyCommand"

        guard let pipelineState = self.pipelineState else {
            print("pipeline state empty")
            return
        }

        // Obtain a render pass descriptor, generated from the view's drawable
        let renderPassDescriptor = self.mtkView.currentRenderPassDescriptor!
        
        self.currentRenderEncoder = self.currentCommandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        self.currentRenderEncoder.label = "BeginRenderEncoder"

        self.currentRenderEncoder.setDepthStencilState(self.depthStencilState)
        self.currentRenderEncoder.setCullMode(.back)

        // Set the region of the drawable to which we'll draw.
        self.currentRenderEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(self.viewportSize.x), height: Double(self.viewportSize.y), znear: -1.0, zfar: 1.0))
        self.currentRenderEncoder.setRenderPipelineState(pipelineState)
    }

    func endRender() {
        if (self.currentRenderEncoder != nil) {
            self.currentRenderEncoder.endEncoding()
            self.currentRenderEncoder = nil
        }
        
        self.currentCommandBuffer.present(self.mtkView.currentDrawable!)
        self.currentCommandBuffer.commit()
        self.currentCommandBuffer = nil
    }

    func draw(in view: MTKView) {

    }
}

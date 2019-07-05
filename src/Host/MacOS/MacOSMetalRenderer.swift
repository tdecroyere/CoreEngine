import Cocoa
import Metal
import MetalKit
import simd
import CoreEngineInterop

func getRenderSizeHandle(graphicsContext: UnsafeMutableRawPointer?) -> Vector2 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.getRenderSize()
}

func createShaderHandle(graphicsContext: UnsafeMutableRawPointer?, shaderByteCode: MemoryBuffer) -> UInt32 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.createShader(shaderByteCode: shaderByteCode)
    return 0
}

func createShaderParametersHandle(graphicsContext: UnsafeMutableRawPointer?, graphicsBuffer1: UInt32, graphicsBuffer2: UInt32, graphicsBuffer3: UInt32) -> UInt32 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createShaderParameters([graphicsBuffer1, graphicsBuffer2, graphicsBuffer3])
}

func createGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, data: MemoryBuffer) -> UInt32 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createGraphicsBuffer(data: data)
}

func uploadDataToGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, graphicsBufferId: UInt32, data: MemoryBuffer) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.uploadDataToGraphicsBuffer(graphicsBufferId, data)
}

func drawPrimitivesHandle(graphicsContext: UnsafeMutableRawPointer?, startIndex: UInt32, indexCount: UInt32, vertexBufferId: UInt32, indexBufferId: UInt32, objectPropertyIndex: Int32) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.drawPrimitives(startIndex, indexCount, vertexBufferId, indexBufferId, objectPropertyIndex)
}


class MacOSMetalRenderer: NSObject, MTKViewDelegate {
    let device: MTLDevice
    let mtkView: MTKView
    var commandQueue: MTLCommandQueue!
    var pipelineState: MTLRenderPipelineState?
    var depthStencilState: MTLDepthStencilState!
    var currentCommandBuffer: MTLCommandBuffer!
    var currentRenderEncoder: MTLRenderCommandEncoder!

    var vertexFunction: MTLFunction!
    var fragmentFunction: MTLFunction!

    var argumentBuffer: MTLBuffer!
    var renderPassParametersBuffer: MTLBuffer!
    var vertexShaderParametersBuffer: MTLBuffer!
    var objectPropertiesBuffer: MTLBuffer!

    var graphicsBuffers: [UInt32: MTLBuffer]
    var currentGraphicsBufferId: UInt32

    init(view: MTKView, device: MTLDevice) {
        self.mtkView = view
        self.device = device
        self.currentCommandBuffer = nil
        self.graphicsBuffers = [:]
        self.currentGraphicsBufferId = 0;

        super.init()

        self.mtkView.isPaused = true
        self.mtkView.colorPixelFormat = .bgra8Unorm_srgb
        self.mtkView.depthStencilPixelFormat = .depth32Float

        let depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .less
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthStencilState = self.device.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        self.commandQueue = self.device.makeCommandQueue()
    }

    func initGraphicsService(_ graphicsService: inout GraphicsService) {
        graphicsService.GraphicsContext = Unmanaged.passUnretained(self).toOpaque()
        graphicsService.GetRenderSize = getRenderSizeHandle
        graphicsService.CreateShader = createShaderHandle
        graphicsService.CreateShaderParameters = createShaderParametersHandle
        graphicsService.CreateGraphicsBuffer = createGraphicsBufferHandle
        graphicsService.UploadDataToGraphicsBuffer = uploadDataToGraphicsBufferHandle
        graphicsService.DrawPrimitives = drawPrimitivesHandle
    }

    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
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
        self.argumentBuffer.label = "argument buffer"

        argumentEncoder.setArgumentBuffer(argumentBuffer, offset: 0)

        for i in 0...graphicsBufferIdList.count - 1 {
            let graphicsBuffer = self.graphicsBuffers[graphicsBufferIdList[i]]
            argumentEncoder.setBuffer(graphicsBuffer, offset: 0, index: i)
        }

        self.renderPassParametersBuffer = self.graphicsBuffers[graphicsBufferIdList[0]]
        self.renderPassParametersBuffer.label = "RenderPassBuffer"
        
        self.objectPropertiesBuffer = self.graphicsBuffers[graphicsBufferIdList[1]]
        self.objectPropertiesBuffer.label = "ObjectPropertiesBuffer"

        self.vertexShaderParametersBuffer = self.graphicsBuffers[graphicsBufferIdList[2]]
        self.vertexShaderParametersBuffer.label = "VertexShaderParametersBuffer"

        return 0
    }

    func createGraphicsBuffer(data: MemoryBuffer) -> UInt32 {
        self.currentGraphicsBufferId += 1
        self.graphicsBuffers[self.currentGraphicsBufferId] = self.device.makeBuffer(bytes: data.Pointer, length: Int(data.Length), options: .storageModeShared)
        return self.currentGraphicsBufferId
    }

    func uploadDataToGraphicsBuffer(_ graphicsBufferId: UInt32, _ data: MemoryBuffer) {
        guard let graphicsBuffer = self.graphicsBuffers[graphicsBufferId] else {
            print("ERROR: Graphics buffer was not found")
            return
        }

        let bufferContents = graphicsBuffer.contents()
        bufferContents.copyMemory(from: data.Pointer, byteCount: Int(data.Length))
    }

    func drawPrimitives(_ startIndex: UInt32, _ indexCount: UInt32, _ vertexBufferId: UInt32, _ indexBufferId: UInt32, _ objectPropertyIndex: Int32) {
        if (self.currentRenderEncoder != nil) {
            // TODO: Change the fact that we have only one command buffer stored in a private field
            
            if (self.renderPassParametersBuffer == nil)
            {
                return
            }

            let vertexGraphicsBuffer = self.graphicsBuffers[vertexBufferId]
            let indexGraphicsBuffer = self.graphicsBuffers[indexBufferId]

            self.currentRenderEncoder!.useResource(self.renderPassParametersBuffer, usage: .read)
            self.currentRenderEncoder!.useResource(self.vertexShaderParametersBuffer, usage: .read)
            self.currentRenderEncoder!.useResource(self.objectPropertiesBuffer, usage: .read)

            let startIndexOffset = Int(startIndex * 4)

            self.currentRenderEncoder!.setVertexBuffer(vertexGraphicsBuffer!, offset: 0, index: 0)
            self.currentRenderEncoder!.setVertexBuffer(self.argumentBuffer, offset: 0, index: 1)

            self.currentRenderEncoder!.drawIndexedPrimitives(type: .triangle, 
                                                             indexCount: Int(indexCount), 
                                                             indexType: .uint32, 
                                                             indexBuffer: indexGraphicsBuffer!, 
                                                             indexBufferOffset: startIndexOffset, 
                                                             instanceCount: 1, 
                                                             baseVertex: 0, 
                                                             baseInstance: Int(objectPropertyIndex))
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
        let renderSize = getRenderSize()
        self.currentRenderEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(renderSize.X), height: Double(renderSize.Y), znear: -1.0, zfar: 1.0))
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

        self.mtkView.draw()
    }

    func draw(in view: MTKView) {

    }
}

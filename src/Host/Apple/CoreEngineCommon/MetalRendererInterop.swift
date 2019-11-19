import CoreEngineCommonInterop

func getRenderSizeHandle(graphicsContext: UnsafeMutableRawPointer?) -> Vector2 {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.getRenderSize()
}

func createShaderHandle(graphicsContext: UnsafeMutableRawPointer?, shaderByteCodeData: UnsafeMutableRawPointer?, shaderByteCodeLength: Int32) -> UInt32 {
    guard let dataBuffer = shaderByteCodeData else {
        print("ERROR: Static data buffer data is null")
        return 0
    }

    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.createShader(shaderByteCodeData: dataBuffer, shaderByteCodeLength: shaderByteCodeLength)
    return 0
}

func createShaderParametersHandle(graphicsContext: UnsafeMutableRawPointer?, graphicsBuffer1: UInt32, graphicsBuffer2: UInt32, graphicsBuffer3: UInt32) -> UInt32 {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createShaderParameters([graphicsBuffer1, graphicsBuffer2, graphicsBuffer3])
}

func createStaticGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, data: UnsafeMutableRawPointer?, length: Int32) -> UInt32 {
    guard let dataBuffer = data else {
        print("ERROR: Static data buffer data is null")
        return 0
    }

    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createStaticGraphicsBuffer(dataBuffer, length)
}

func createDynamicGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, length: UInt32) -> UInt32 {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createDynamicGraphicsBuffer(length)
}

func uploadDataToGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, graphicsBufferId: UInt32, data: UnsafeMutableRawPointer?, length: Int32) {
    guard let dataBuffer = data else {
        print("ERROR: Dynamic data buffer data is null")
        return
    }

    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.uploadDataToGraphicsBuffer(graphicsBufferId, dataBuffer, length)
}

// TODO: ToRemove
func beginCopyGpuDataHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.beginCopyGpuData()
}

// TODO: ToRemove
func endCopyGpuDataHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.endCopyGpuData()
}

// TODO: ToRemove
func beginRenderHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.beginRender()
}

// TODO: ToRemove
func endRenderHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.endRender()
}

func drawPrimitivesHandle(graphicsContext: UnsafeMutableRawPointer?, startIndex: UInt32, indexCount: UInt32, vertexBufferId: UInt32, indexBufferId: UInt32, baseInstanceId: UInt32) {
    let renderer = Unmanaged<MetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.drawPrimitives(startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId)
}

func initGraphicsService(_ renderer: MetalRenderer, _ graphicsService: inout GraphicsService) {
    graphicsService.GraphicsContext = Unmanaged.passUnretained(renderer).toOpaque()
    graphicsService.GetRenderSize = getRenderSizeHandle
    graphicsService.CreateShader = createShaderHandle
    graphicsService.CreateShaderParameters = createShaderParametersHandle
    graphicsService.CreateStaticGraphicsBuffer = createStaticGraphicsBufferHandle
    graphicsService.CreateDynamicGraphicsBuffer = createDynamicGraphicsBufferHandle
    graphicsService.UploadDataToGraphicsBuffer = uploadDataToGraphicsBufferHandle
    graphicsService.BeginCopyGpuData = beginCopyGpuDataHandle
    graphicsService.EndCopyGpuData = endCopyGpuDataHandle
    graphicsService.BeginRender = beginRenderHandle
    graphicsService.EndRender = endRenderHandle
    graphicsService.DrawPrimitives = drawPrimitivesHandle
}
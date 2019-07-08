import CoreEngineInterop

func getRenderSizeHandle(graphicsContext: UnsafeMutableRawPointer?) -> Vector2 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.getRenderSize()
}

func createShaderHandle(graphicsContext: UnsafeMutableRawPointer?, shaderByteCode: HostMemoryBuffer) -> UInt32 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.createShader(shaderByteCode: shaderByteCode)
    return 0
}

func createShaderParametersHandle(graphicsContext: UnsafeMutableRawPointer?, graphicsBuffer1: UInt32, graphicsBuffer2: UInt32, graphicsBuffer3: UInt32) -> UInt32 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createShaderParameters([graphicsBuffer1, graphicsBuffer2, graphicsBuffer3])
}

func createStaticGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, data: HostMemoryBuffer) -> UInt32 {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createStaticGraphicsBuffer(data)
}

func createDynamicGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, length: UInt32) -> HostMemoryBuffer {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    return renderer.createDynamicGraphicsBuffer(length)
}

func uploadDataToGraphicsBufferHandle(graphicsContext: UnsafeMutableRawPointer?, graphicsBufferId: UInt32, data: HostMemoryBuffer) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.uploadDataToGraphicsBuffer(graphicsBufferId, data)
}

func beginCopyGpuDataHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.beginCopyGpuData()
}

func endCopyGpuDataHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.endCopyGpuData()
}

func beginRenderHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.beginRender()
}

func endRenderHandle(graphicsContext: UnsafeMutableRawPointer?) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.endRender()
}

func drawPrimitivesHandle(graphicsContext: UnsafeMutableRawPointer?, startIndex: UInt32, indexCount: UInt32, vertexBufferId: UInt32, indexBufferId: UInt32, baseInstanceId: UInt32) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()
    renderer.drawPrimitives(startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId)
}

func initGraphicsService(_ renderer: MacOSMetalRenderer, _ graphicsService: inout GraphicsService) {
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
import CoreEngineCommonInterop

func getRenderSizeInterop(context: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getRenderSize()
}

func createShaderInterop(context: UnsafeMutableRawPointer?, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createShader(shaderByteCode!, Int(shaderByteCodeLength)))
}

func createShaderParametersInterop(context: UnsafeMutableRawPointer?, _ graphicsBuffer1: UInt32, _ graphicsBuffer2: UInt32, _ graphicsBuffer3: UInt32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createShaderParameters(UInt(graphicsBuffer1), UInt(graphicsBuffer2), UInt(graphicsBuffer3)))
}

func createStaticGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createStaticGraphicsBuffer(data!, Int(dataLength)))
}

func createDynamicGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ length: Int32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createDynamicGraphicsBuffer(Int(length)))
}

func uploadDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToGraphicsBuffer(UInt(graphicsBufferId), data!, Int(dataLength))
}

func beginCopyGpuDataInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.beginCopyGpuData()
}

func endCopyGpuDataInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.endCopyGpuData()
}

func beginRenderInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.beginRender()
}

func endRenderInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.endRender()
}

func drawPrimitivesInterop(context: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startIndex: UInt32, _ indexCount: UInt32, _ vertexBufferId: UInt32, _ indexBufferId: UInt32, _ baseInstanceId: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawPrimitives(primitiveType, UInt(startIndex), UInt(indexCount), UInt(vertexBufferId), UInt(indexBufferId), UInt(baseInstanceId))
}

func initGraphicsService(_ context: MetalRenderer, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GetRenderSize = getRenderSizeInterop
    service.CreateShader = createShaderInterop
    service.CreateShaderParameters = createShaderParametersInterop
    service.CreateStaticGraphicsBuffer = createStaticGraphicsBufferInterop
    service.CreateDynamicGraphicsBuffer = createDynamicGraphicsBufferInterop
    service.UploadDataToGraphicsBuffer = uploadDataToGraphicsBufferInterop
    service.BeginCopyGpuData = beginCopyGpuDataInterop
    service.EndCopyGpuData = endCopyGpuDataInterop
    service.BeginRender = beginRenderInterop
    service.EndRender = endRenderInterop
    service.DrawPrimitives = drawPrimitivesInterop
}

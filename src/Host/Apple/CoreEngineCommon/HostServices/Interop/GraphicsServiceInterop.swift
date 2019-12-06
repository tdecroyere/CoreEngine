import CoreEngineCommonInterop

func getRenderSizeInterop(context: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getRenderSize()
}

func createPipelineStateInterop(context: UnsafeMutableRawPointer?, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createPipelineState(shaderByteCode!, Int(shaderByteCodeLength)))
}

func removePipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.removePipelineState(UInt(pipelineStateId))
}

func createShaderParametersInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32, _ graphicsBuffer1: UInt32, _ graphicsBuffer2: UInt32, _ graphicsBuffer3: UInt32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createShaderParameters(UInt(pipelineStateId), UInt(graphicsBuffer1), UInt(graphicsBuffer2), UInt(graphicsBuffer3)))
}

func createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ length: Int32) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createGraphicsBuffer(Int(length)))
}

func createCopyCommandListInterop(context: UnsafeMutableRawPointer?) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createCopyCommandList())
}

func executeCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeCopyCommandList(UInt(commandListId))
}

func uploadDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToGraphicsBuffer(UInt(commandListId), UInt(graphicsBufferId), data!, Int(dataLength))
}

func createRenderCommandListInterop(context: UnsafeMutableRawPointer?) -> UInt32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return UInt32(contextObject.createRenderCommandList())
}

func executeRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeRenderCommandList(UInt(commandListId))
}

func setPipelineStateInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineState(UInt(commandListId), UInt(pipelineStateId))
}

func setGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setGraphicsBuffer(UInt(commandListId), UInt(graphicsBufferId), graphicsBindStage, UInt(slot))
}

func drawPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ primitiveType: GraphicsPrimitiveType, _ startIndex: UInt32, _ indexCount: UInt32, _ vertexBufferId: UInt32, _ indexBufferId: UInt32, _ baseInstanceId: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawPrimitives(UInt(commandListId), primitiveType, UInt(startIndex), UInt(indexCount), UInt(vertexBufferId), UInt(indexBufferId), UInt(baseInstanceId))
}

func presentScreenBufferInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.presentScreenBuffer()
}

func initGraphicsService(_ context: MetalRenderer, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GetRenderSize = getRenderSizeInterop
    service.CreatePipelineState = createPipelineStateInterop
    service.RemovePipelineState = removePipelineStateInterop
    service.CreateShaderParameters = createShaderParametersInterop
    service.CreateGraphicsBuffer = createGraphicsBufferInterop
    service.CreateCopyCommandList = createCopyCommandListInterop
    service.ExecuteCopyCommandList = executeCopyCommandListInterop
    service.UploadDataToGraphicsBuffer = uploadDataToGraphicsBufferInterop
    service.CreateRenderCommandList = createRenderCommandListInterop
    service.ExecuteRenderCommandList = executeRenderCommandListInterop
    service.SetPipelineState = setPipelineStateInterop
    service.SetGraphicsBuffer = setGraphicsBufferInterop
    service.DrawPrimitives = drawPrimitivesInterop
    service.PresentScreenBuffer = presentScreenBufferInterop
}

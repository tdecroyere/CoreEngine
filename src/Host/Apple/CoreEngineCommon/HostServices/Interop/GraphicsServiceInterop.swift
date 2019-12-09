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

func createShaderParametersInterop(context: UnsafeMutableRawPointer?, _ graphicsResourceId: UInt32, _ pipelineStateId: UInt32, _ graphicsBuffer1: UInt32, _ graphicsBuffer2: UInt32, _ graphicsBuffer3: UInt32) -> Int32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createShaderParameters(UInt(graphicsResourceId), UInt(pipelineStateId), UInt(graphicsBuffer1), UInt(graphicsBuffer2), UInt(graphicsBuffer3)) ? 1 : 0)
}

func createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsResourceId: UInt32, _ length: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsBuffer(UInt(graphicsResourceId), Int(length)) ? 1 : 0)
}

func createTextureInterop(context: UnsafeMutableRawPointer?, _ graphicsResourceId: UInt32, _ width: Int32, _ height: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createTexture(UInt(graphicsResourceId), Int(width), Int(height)) ? 1 : 0)
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

func uploadDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ width: Int32, _ height: Int32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToTexture(UInt(commandListId), UInt(textureId), Int(width), Int(height), data!, Int(dataLength))
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

func setTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setTexture(UInt(commandListId), UInt(textureId), graphicsBindStage, UInt(slot))
}

func drawPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int32, _ indexCount: Int32, _ vertexBufferId: UInt32, _ indexBufferId: UInt32, _ instanceCount: Int32, _ baseInstanceId: Int32) {
    let contextObject = Unmanaged<MetalRenderer>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawPrimitives(UInt(commandListId), primitiveType, Int(startIndex), Int(indexCount), UInt(vertexBufferId), UInt(indexBufferId), Int(instanceCount), Int(baseInstanceId))
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
    service.CreateTexture = createTextureInterop
    service.CreateCopyCommandList = createCopyCommandListInterop
    service.ExecuteCopyCommandList = executeCopyCommandListInterop
    service.UploadDataToGraphicsBuffer = uploadDataToGraphicsBufferInterop
    service.UploadDataToTexture = uploadDataToTextureInterop
    service.CreateRenderCommandList = createRenderCommandListInterop
    service.ExecuteRenderCommandList = executeRenderCommandListInterop
    service.SetPipelineState = setPipelineStateInterop
    service.SetGraphicsBuffer = setGraphicsBufferInterop
    service.SetTexture = setTextureInterop
    service.DrawPrimitives = drawPrimitivesInterop
    service.PresentScreenBuffer = presentScreenBufferInterop
}

import CoreEngineCommonInterop

func GraphicsService_getRenderSizeInterop(context: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getRenderSize()
}

func GraphicsService_createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ length: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsBuffer(UInt(graphicsBufferId), Int(length)) ? 1 : 0)
}

func GraphicsService_createTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32, _ width: Int32, _ height: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createTexture(UInt(textureId), Int(width), Int(height)) ? 1 : 0)
}

func GraphicsService_createShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createShader(UInt(shaderId), shaderByteCode!, Int(shaderByteCodeLength)) ? 1 : 0)
}

func GraphicsService_removeShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.removeShader(UInt(shaderId))
}

func GraphicsService_createCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createCopyCommandList(UInt(commandListId)) ? 1 : 0)
}

func GraphicsService_executeCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeCopyCommandList(UInt(commandListId))
}

func GraphicsService_uploadDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToGraphicsBuffer(UInt(commandListId), UInt(graphicsBufferId), data!, Int(dataLength))
}

func GraphicsService_uploadDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ width: Int32, _ height: Int32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToTexture(UInt(commandListId), UInt(textureId), Int(width), Int(height), data!, Int(dataLength))
}

func GraphicsService_createRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createRenderCommandList(UInt(commandListId)) ? 1 : 0)
}

func GraphicsService_executeRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeRenderCommandList(UInt(commandListId))
}

func GraphicsService_setShaderInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShader(UInt(commandListId), UInt(shaderId))
}

func GraphicsService_setShaderBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderBuffer(UInt(commandListId), UInt(graphicsBufferId), Int(slot), Int(index))
}

func GraphicsService_setShaderBuffersInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferIdList: UnsafeMutablePointer<UInt32>?, _ graphicsBufferIdListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderBuffers(UInt(commandListId), Array(UnsafeBufferPointer(start: graphicsBufferIdList, count: Int(graphicsBufferIdListLength))), Int(slot), Int(index))
}

func GraphicsService_setShaderTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderTexture(UInt(commandListId), UInt(textureId), Int(slot), Int(index))
}

func GraphicsService_setShaderTexturesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureIdList: UnsafeMutablePointer<UInt32>?, _ textureIdListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderTextures(UInt(commandListId), Array(UnsafeBufferPointer(start: textureIdList, count: Int(textureIdListLength))), Int(slot), Int(index))
}

func GraphicsService_setIndexBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setIndexBuffer(UInt(commandListId), UInt(graphicsBufferId))
}

func GraphicsService_drawIndexedPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int32, _ indexCount: Int32, _ instanceCount: Int32, _ baseInstanceId: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawIndexedPrimitives(UInt(commandListId), primitiveType, Int(startIndex), Int(indexCount), Int(instanceCount), Int(baseInstanceId))
}

func GraphicsService_presentScreenBufferInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.presentScreenBuffer()
}

func initGraphicsService(_ context: MetalGraphicsService, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GraphicsService_GetRenderSize = GraphicsService_getRenderSizeInterop
    service.GraphicsService_CreateGraphicsBuffer = GraphicsService_createGraphicsBufferInterop
    service.GraphicsService_CreateTexture = GraphicsService_createTextureInterop
    service.GraphicsService_CreateShader = GraphicsService_createShaderInterop
    service.GraphicsService_RemoveShader = GraphicsService_removeShaderInterop
    service.GraphicsService_CreateCopyCommandList = GraphicsService_createCopyCommandListInterop
    service.GraphicsService_ExecuteCopyCommandList = GraphicsService_executeCopyCommandListInterop
    service.GraphicsService_UploadDataToGraphicsBuffer = GraphicsService_uploadDataToGraphicsBufferInterop
    service.GraphicsService_UploadDataToTexture = GraphicsService_uploadDataToTextureInterop
    service.GraphicsService_CreateRenderCommandList = GraphicsService_createRenderCommandListInterop
    service.GraphicsService_ExecuteRenderCommandList = GraphicsService_executeRenderCommandListInterop
    service.GraphicsService_SetShader = GraphicsService_setShaderInterop
    service.GraphicsService_SetShaderBuffer = GraphicsService_setShaderBufferInterop
    service.GraphicsService_SetShaderBuffers = GraphicsService_setShaderBuffersInterop
    service.GraphicsService_SetShaderTexture = GraphicsService_setShaderTextureInterop
    service.GraphicsService_SetShaderTextures = GraphicsService_setShaderTexturesInterop
    service.GraphicsService_SetIndexBuffer = GraphicsService_setIndexBufferInterop
    service.GraphicsService_DrawIndexedPrimitives = GraphicsService_drawIndexedPrimitivesInterop
    service.GraphicsService_PresentScreenBuffer = GraphicsService_presentScreenBufferInterop
}

import Foundation
import CoreEngineCommonInterop

func GraphicsService_getRenderSizeInterop(context: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getRenderSize()
}

func GraphicsService_getGraphicsAdapterNameInterop(context: UnsafeMutableRawPointer?) -> UnsafeMutablePointer<Int8>? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return UnsafeMutablePointer<Int8>?(strdup(contextObject.getGraphicsAdapterName()!))
}

func GraphicsService_getGpuExecutionTimeInterop(context: UnsafeMutableRawPointer?, _ frameNumber: UInt32) -> Float {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getGpuExecutionTime(UInt(frameNumber))
}

func GraphicsService_createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ length: Int32, _ debugName: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsBuffer(UInt(graphicsBufferId), Int(length), (debugName != nil) ? String(cString: debugName!) : nil) ? 1 : 0)
}

func GraphicsService_createTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ mipLevels: Int32, _ multisampleCount: Int32, _ isRenderTarget: Int32, _ debugName: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createTexture(UInt(textureId), textureFormat, Int(width), Int(height), Int(mipLevels), Int(multisampleCount), Bool(isRenderTarget == 1), (debugName != nil) ? String(cString: debugName!) : nil) ? 1 : 0)
}

func GraphicsService_removeTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.removeTexture(UInt(textureId))
}

func GraphicsService_createShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32, _ computeShaderFunction: UnsafeMutablePointer<Int8>?, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32, _ debugName: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createShader(UInt(shaderId), (computeShaderFunction != nil) ? String(cString: computeShaderFunction!) : nil, shaderByteCode!, Int(shaderByteCodeLength), (debugName != nil) ? String(cString: debugName!) : nil) ? 1 : 0)
}

func GraphicsService_removeShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.removeShader(UInt(shaderId))
}

func GraphicsService_createPipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32, _ shaderId: UInt32, _ renderPassDescriptor: GraphicsRenderPassDescriptor, _ debugName: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createPipelineState(UInt(pipelineStateId), UInt(shaderId), renderPassDescriptor, (debugName != nil) ? String(cString: debugName!) : nil) ? 1 : 0)
}

func GraphicsService_removePipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.removePipelineState(UInt(pipelineStateId))
}

func GraphicsService_createCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ debugName: UnsafeMutablePointer<Int8>?, _ createNewCommandBuffer: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createCopyCommandList(UInt(commandListId), (debugName != nil) ? String(cString: debugName!) : nil, Bool(createNewCommandBuffer == 1)) ? 1 : 0)
}

func GraphicsService_executeCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeCopyCommandList(UInt(commandListId))
}

func GraphicsService_uploadDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToGraphicsBuffer(UInt(commandListId), UInt(graphicsBufferId), data!, Int(dataLength))
}

func GraphicsService_uploadDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ mipLevel: Int32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToTexture(UInt(commandListId), UInt(textureId), textureFormat, Int(width), Int(height), Int(mipLevel), data!, Int(dataLength))
}

func GraphicsService_resetIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_optimizeIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.optimizeIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_createComputeCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ debugName: UnsafeMutablePointer<Int8>?, _ createNewCommandBuffer: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createComputeCommandList(UInt(commandListId), (debugName != nil) ? String(cString: debugName!) : nil, Bool(createNewCommandBuffer == 1)) ? 1 : 0)
}

func GraphicsService_executeComputeCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeComputeCommandList(UInt(commandListId))
}

func GraphicsService_dispatchThreadsInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ threadGroupCountX: UInt32, _ threadGroupCountY: UInt32, _ threadGroupCountZ: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.dispatchThreads(UInt(commandListId), UInt(threadGroupCountX), UInt(threadGroupCountY), UInt(threadGroupCountZ))
}

func GraphicsService_createRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ renderDescriptor: GraphicsRenderPassDescriptor, _ debugName: UnsafeMutablePointer<Int8>?, _ createNewCommandBuffer: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createRenderCommandList(UInt(commandListId), renderDescriptor, (debugName != nil) ? String(cString: debugName!) : nil, Bool(createNewCommandBuffer == 1)) ? 1 : 0)
}

func GraphicsService_executeRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeRenderCommandList(UInt(commandListId))
}

func GraphicsService_createIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ maxCommandCount: Int32, _ debugName: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createIndirectCommandList(UInt(commandListId), Int(maxCommandCount), (debugName != nil) ? String(cString: debugName!) : nil) ? 1 : 0)
}

func GraphicsService_setPipelineStateInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineState(UInt(commandListId), UInt(pipelineStateId))
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

func GraphicsService_setShaderTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ slot: Int32, _ isReadOnly: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderTexture(UInt(commandListId), UInt(textureId), Int(slot), Bool(isReadOnly == 1), Int(index))
}

func GraphicsService_setShaderTexturesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureIdList: UnsafeMutablePointer<UInt32>?, _ textureIdListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderTextures(UInt(commandListId), Array(UnsafeBufferPointer(start: textureIdList, count: Int(textureIdListLength))), Int(slot), Int(index))
}

func GraphicsService_setShaderIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(slot), Int(index))
}

func GraphicsService_executeIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_setIndexBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setIndexBuffer(UInt(commandListId), UInt(graphicsBufferId))
}

func GraphicsService_drawIndexedPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int32, _ indexCount: Int32, _ instanceCount: Int32, _ baseInstanceId: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawIndexedPrimitives(UInt(commandListId), primitiveType, Int(startIndex), Int(indexCount), Int(instanceCount), Int(baseInstanceId))
}

func GraphicsService_drawPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int32, _ vertexCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawPrimitives(UInt(commandListId), primitiveType, Int(startVertex), Int(vertexCount))
}

func GraphicsService_presentScreenBufferInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.presentScreenBuffer()
}

func initGraphicsService(_ context: MetalGraphicsService, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GraphicsService_GetRenderSize = GraphicsService_getRenderSizeInterop
    service.GraphicsService_GetGraphicsAdapterName = GraphicsService_getGraphicsAdapterNameInterop
    service.GraphicsService_GetGpuExecutionTime = GraphicsService_getGpuExecutionTimeInterop
    service.GraphicsService_CreateGraphicsBuffer = GraphicsService_createGraphicsBufferInterop
    service.GraphicsService_CreateTexture = GraphicsService_createTextureInterop
    service.GraphicsService_RemoveTexture = GraphicsService_removeTextureInterop
    service.GraphicsService_CreateShader = GraphicsService_createShaderInterop
    service.GraphicsService_RemoveShader = GraphicsService_removeShaderInterop
    service.GraphicsService_CreatePipelineState = GraphicsService_createPipelineStateInterop
    service.GraphicsService_RemovePipelineState = GraphicsService_removePipelineStateInterop
    service.GraphicsService_CreateCopyCommandList = GraphicsService_createCopyCommandListInterop
    service.GraphicsService_ExecuteCopyCommandList = GraphicsService_executeCopyCommandListInterop
    service.GraphicsService_UploadDataToGraphicsBuffer = GraphicsService_uploadDataToGraphicsBufferInterop
    service.GraphicsService_UploadDataToTexture = GraphicsService_uploadDataToTextureInterop
    service.GraphicsService_ResetIndirectCommandList = GraphicsService_resetIndirectCommandListInterop
    service.GraphicsService_OptimizeIndirectCommandList = GraphicsService_optimizeIndirectCommandListInterop
    service.GraphicsService_CreateComputeCommandList = GraphicsService_createComputeCommandListInterop
    service.GraphicsService_ExecuteComputeCommandList = GraphicsService_executeComputeCommandListInterop
    service.GraphicsService_DispatchThreads = GraphicsService_dispatchThreadsInterop
    service.GraphicsService_CreateRenderCommandList = GraphicsService_createRenderCommandListInterop
    service.GraphicsService_ExecuteRenderCommandList = GraphicsService_executeRenderCommandListInterop
    service.GraphicsService_CreateIndirectCommandList = GraphicsService_createIndirectCommandListInterop
    service.GraphicsService_SetPipelineState = GraphicsService_setPipelineStateInterop
    service.GraphicsService_SetShader = GraphicsService_setShaderInterop
    service.GraphicsService_SetShaderBuffer = GraphicsService_setShaderBufferInterop
    service.GraphicsService_SetShaderBuffers = GraphicsService_setShaderBuffersInterop
    service.GraphicsService_SetShaderTexture = GraphicsService_setShaderTextureInterop
    service.GraphicsService_SetShaderTextures = GraphicsService_setShaderTexturesInterop
    service.GraphicsService_SetShaderIndirectCommandList = GraphicsService_setShaderIndirectCommandListInterop
    service.GraphicsService_ExecuteIndirectCommandList = GraphicsService_executeIndirectCommandListInterop
    service.GraphicsService_SetIndexBuffer = GraphicsService_setIndexBufferInterop
    service.GraphicsService_DrawIndexedPrimitives = GraphicsService_drawIndexedPrimitivesInterop
    service.GraphicsService_DrawPrimitives = GraphicsService_drawPrimitivesInterop
    service.GraphicsService_PresentScreenBuffer = GraphicsService_presentScreenBufferInterop
}

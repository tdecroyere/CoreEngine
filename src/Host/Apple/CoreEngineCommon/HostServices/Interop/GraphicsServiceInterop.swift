import Foundation
import CoreEngineCommonInterop

func GraphicsService_getGraphicsAdapterNameInterop(context: UnsafeMutableRawPointer?, _ output: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.getGraphicsAdapterName(output)
}

func GraphicsService_getRenderSizeInterop(context: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getRenderSize()
}

func GraphicsService_getTextureAllocationInfosInterop(context: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32) -> GraphicsAllocationInfos {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getTextureAllocationInfos(textureFormat, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount))
}

func GraphicsService_createGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapId: UInt32, _ type: GraphicsServiceHeapType, _ length: UInt, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsHeap(UInt(graphicsHeapId), type, length, String(cString: label!)) ? 1 : 0)
}

func GraphicsService_deleteGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsHeap(UInt(graphicsHeapId))
}

func GraphicsService_createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ graphicsHeapId: UInt32, _ heapOffset: UInt, _ length: Int32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsBuffer(UInt(graphicsBufferId), UInt(graphicsHeapId), heapOffset, Int(length), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_getGraphicsBufferCpuPointerInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getGraphicsBufferCpuPointer(UInt(graphicsBufferId))
}

func GraphicsService_deleteGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsBuffer(UInt(graphicsBufferId))
}

func GraphicsService_createTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32, _ graphicsHeapId: UInt32, _ heapOffset: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32, _ isRenderTarget: Int32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createTexture(UInt(textureId), UInt(graphicsHeapId), heapOffset, textureFormat, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount), Bool(isRenderTarget == 1), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_createTextureOldInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32, _ isRenderTarget: Int32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createTextureOld(UInt(textureId), textureFormat, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount), Bool(isRenderTarget == 1), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_deleteTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteTexture(UInt(textureId))
}

func GraphicsService_createIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ indirectCommandBufferId: UInt32, _ maxCommandCount: Int32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createIndirectCommandBuffer(UInt(indirectCommandBufferId), Int(maxCommandCount), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_createShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32, _ computeShaderFunction: UnsafeMutablePointer<Int8>?, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createShader(UInt(shaderId), (computeShaderFunction != nil) ? String(cString: computeShaderFunction!) : nil, shaderByteCode!, Int(shaderByteCodeLength), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_deleteShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteShader(UInt(shaderId))
}

func GraphicsService_createPipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32, _ shaderId: UInt32, _ renderPassDescriptor: GraphicsRenderPassDescriptor, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createPipelineState(UInt(pipelineStateId), UInt(shaderId), renderPassDescriptor, String(cString: label!)) ? 1 : 0)
}

func GraphicsService_deletePipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deletePipelineState(UInt(pipelineStateId))
}

func GraphicsService_createCommandBufferInterop(context: UnsafeMutableRawPointer?, _ commandBufferId: UInt32, _ commandBufferType: GraphicsCommandBufferType, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createCommandBuffer(UInt(commandBufferId), commandBufferType, String(cString: label!)) ? 1 : 0)
}

func GraphicsService_deleteCommandBufferInterop(context: UnsafeMutableRawPointer?, _ commandBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteCommandBuffer(UInt(commandBufferId))
}

func GraphicsService_resetCommandBufferInterop(context: UnsafeMutableRawPointer?, _ commandBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetCommandBuffer(UInt(commandBufferId))
}

func GraphicsService_executeCommandBufferInterop(context: UnsafeMutableRawPointer?, _ commandBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeCommandBuffer(UInt(commandBufferId))
}

func GraphicsService_getCommandBufferStatusInterop(context: UnsafeMutableRawPointer?, _ commandBufferId: UInt32) -> NullableGraphicsCommandBufferStatus {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getCommandBufferStatus(UInt(commandBufferId))
}

func GraphicsService_setShaderBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ slot: Int32, _ isReadOnly: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderBuffer(UInt(commandListId), UInt(graphicsBufferId), Int(slot), Bool(isReadOnly == 1), Int(index))
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

func GraphicsService_setShaderIndirectCommandListsInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListIdList: UnsafeMutablePointer<UInt32>?, _ indirectCommandListIdListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderIndirectCommandLists(UInt(commandListId), Array(UnsafeBufferPointer(start: indirectCommandListIdList, count: Int(indirectCommandListIdListLength))), Int(slot), Int(index))
}

func GraphicsService_createCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ commandBufferId: UInt32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createCopyCommandList(UInt(commandListId), UInt(commandBufferId), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_commitCopyCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.commitCopyCommandList(UInt(commandListId))
}

func GraphicsService_uploadDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ destinationGraphicsBufferId: UInt32, _ sourceGraphicsBufferId: UInt32, _ length: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToGraphicsBuffer(UInt(commandListId), UInt(destinationGraphicsBufferId), UInt(sourceGraphicsBufferId), Int(length))
}

func GraphicsService_copyGraphicsBufferDataToCpuOldInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsBufferId: UInt32, _ length: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyGraphicsBufferDataToCpuOld(UInt(commandListId), UInt(graphicsBufferId), Int(length))
}

func GraphicsService_readGraphicsBufferDataOldInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.readGraphicsBufferDataOld(UInt(graphicsBufferId), data!, Int(dataLength))
}

func GraphicsService_uploadDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ destinationTextureId: UInt32, _ sourceGraphicsBufferId: UInt32, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ slice: Int32, _ mipLevel: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToTexture(UInt(commandListId), UInt(destinationTextureId), UInt(sourceGraphicsBufferId), textureFormat, Int(width), Int(height), Int(slice), Int(mipLevel))
}

func GraphicsService_uploadDataToTextureOldInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ textureId: UInt32, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ slice: Int32, _ mipLevel: Int32, _ data: UnsafeMutableRawPointer?, _ dataLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.uploadDataToTextureOld(UInt(commandListId), UInt(textureId), textureFormat, Int(width), Int(height), Int(slice), Int(mipLevel), data!, Int(dataLength))
}

func GraphicsService_resetIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_optimizeIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.optimizeIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_createComputeCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ commandBufferId: UInt32, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createComputeCommandList(UInt(commandListId), UInt(commandBufferId), String(cString: label!)) ? 1 : 0)
}

func GraphicsService_commitComputeCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.commitComputeCommandList(UInt(commandListId))
}

func GraphicsService_dispatchThreadsInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ threadCountX: UInt32, _ threadCountY: UInt32, _ threadCountZ: UInt32) -> Vector3 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.dispatchThreads(UInt(commandListId), UInt(threadCountX), UInt(threadCountY), UInt(threadCountZ))
}

func GraphicsService_createRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ commandBufferId: UInt32, _ renderDescriptor: GraphicsRenderPassDescriptor, _ label: UnsafeMutablePointer<Int8>?) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createRenderCommandList(UInt(commandListId), UInt(commandBufferId), renderDescriptor, String(cString: label!)) ? 1 : 0)
}

func GraphicsService_commitRenderCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.commitRenderCommandList(UInt(commandListId))
}

func GraphicsService_setPipelineStateInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineState(UInt(commandListId), UInt(pipelineStateId))
}

func GraphicsService_setShaderInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShader(UInt(commandListId), UInt(shaderId))
}

func GraphicsService_bindGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ graphicsHeapId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.bindGraphicsHeap(UInt(commandListId), UInt(graphicsHeapId))
}

func GraphicsService_executeIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandBufferId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeIndirectCommandBuffer(UInt(commandListId), UInt(indirectCommandBufferId), Int(maxCommandCount))
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

func GraphicsService_waitForCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ commandListToWaitId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForCommandList(UInt(commandListId), UInt(commandListToWaitId))
}

func GraphicsService_presentScreenBufferInterop(context: UnsafeMutableRawPointer?, _ commandBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.presentScreenBuffer(UInt(commandBufferId))
}

func GraphicsService_waitForAvailableScreenBufferInterop(context: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForAvailableScreenBuffer()
}

func initGraphicsService(_ context: MetalGraphicsService, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GraphicsService_GetGraphicsAdapterName = GraphicsService_getGraphicsAdapterNameInterop
    service.GraphicsService_GetRenderSize = GraphicsService_getRenderSizeInterop
    service.GraphicsService_GetTextureAllocationInfos = GraphicsService_getTextureAllocationInfosInterop
    service.GraphicsService_CreateGraphicsHeap = GraphicsService_createGraphicsHeapInterop
    service.GraphicsService_DeleteGraphicsHeap = GraphicsService_deleteGraphicsHeapInterop
    service.GraphicsService_CreateGraphicsBuffer = GraphicsService_createGraphicsBufferInterop
    service.GraphicsService_GetGraphicsBufferCpuPointer = GraphicsService_getGraphicsBufferCpuPointerInterop
    service.GraphicsService_DeleteGraphicsBuffer = GraphicsService_deleteGraphicsBufferInterop
    service.GraphicsService_CreateTexture = GraphicsService_createTextureInterop
    service.GraphicsService_CreateTextureOld = GraphicsService_createTextureOldInterop
    service.GraphicsService_DeleteTexture = GraphicsService_deleteTextureInterop
    service.GraphicsService_CreateIndirectCommandBuffer = GraphicsService_createIndirectCommandBufferInterop
    service.GraphicsService_CreateShader = GraphicsService_createShaderInterop
    service.GraphicsService_DeleteShader = GraphicsService_deleteShaderInterop
    service.GraphicsService_CreatePipelineState = GraphicsService_createPipelineStateInterop
    service.GraphicsService_DeletePipelineState = GraphicsService_deletePipelineStateInterop
    service.GraphicsService_CreateCommandBuffer = GraphicsService_createCommandBufferInterop
    service.GraphicsService_DeleteCommandBuffer = GraphicsService_deleteCommandBufferInterop
    service.GraphicsService_ResetCommandBuffer = GraphicsService_resetCommandBufferInterop
    service.GraphicsService_ExecuteCommandBuffer = GraphicsService_executeCommandBufferInterop
    service.GraphicsService_GetCommandBufferStatus = GraphicsService_getCommandBufferStatusInterop
    service.GraphicsService_SetShaderBuffer = GraphicsService_setShaderBufferInterop
    service.GraphicsService_SetShaderBuffers = GraphicsService_setShaderBuffersInterop
    service.GraphicsService_SetShaderTexture = GraphicsService_setShaderTextureInterop
    service.GraphicsService_SetShaderTextures = GraphicsService_setShaderTexturesInterop
    service.GraphicsService_SetShaderIndirectCommandList = GraphicsService_setShaderIndirectCommandListInterop
    service.GraphicsService_SetShaderIndirectCommandLists = GraphicsService_setShaderIndirectCommandListsInterop
    service.GraphicsService_CreateCopyCommandList = GraphicsService_createCopyCommandListInterop
    service.GraphicsService_CommitCopyCommandList = GraphicsService_commitCopyCommandListInterop
    service.GraphicsService_UploadDataToGraphicsBuffer = GraphicsService_uploadDataToGraphicsBufferInterop
    service.GraphicsService_CopyGraphicsBufferDataToCpuOld = GraphicsService_copyGraphicsBufferDataToCpuOldInterop
    service.GraphicsService_ReadGraphicsBufferDataOld = GraphicsService_readGraphicsBufferDataOldInterop
    service.GraphicsService_UploadDataToTexture = GraphicsService_uploadDataToTextureInterop
    service.GraphicsService_UploadDataToTextureOld = GraphicsService_uploadDataToTextureOldInterop
    service.GraphicsService_ResetIndirectCommandList = GraphicsService_resetIndirectCommandListInterop
    service.GraphicsService_OptimizeIndirectCommandList = GraphicsService_optimizeIndirectCommandListInterop
    service.GraphicsService_CreateComputeCommandList = GraphicsService_createComputeCommandListInterop
    service.GraphicsService_CommitComputeCommandList = GraphicsService_commitComputeCommandListInterop
    service.GraphicsService_DispatchThreads = GraphicsService_dispatchThreadsInterop
    service.GraphicsService_CreateRenderCommandList = GraphicsService_createRenderCommandListInterop
    service.GraphicsService_CommitRenderCommandList = GraphicsService_commitRenderCommandListInterop
    service.GraphicsService_SetPipelineState = GraphicsService_setPipelineStateInterop
    service.GraphicsService_SetShader = GraphicsService_setShaderInterop
    service.GraphicsService_BindGraphicsHeap = GraphicsService_bindGraphicsHeapInterop
    service.GraphicsService_ExecuteIndirectCommandBuffer = GraphicsService_executeIndirectCommandBufferInterop
    service.GraphicsService_SetIndexBuffer = GraphicsService_setIndexBufferInterop
    service.GraphicsService_DrawIndexedPrimitives = GraphicsService_drawIndexedPrimitivesInterop
    service.GraphicsService_DrawPrimitives = GraphicsService_drawPrimitivesInterop
    service.GraphicsService_WaitForCommandList = GraphicsService_waitForCommandListInterop
    service.GraphicsService_PresentScreenBuffer = GraphicsService_presentScreenBufferInterop
    service.GraphicsService_WaitForAvailableScreenBuffer = GraphicsService_waitForAvailableScreenBufferInterop
}

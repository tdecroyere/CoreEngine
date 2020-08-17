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

func GraphicsService_getTextureAllocationInfosInterop(context: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32) -> GraphicsAllocationInfos {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getTextureAllocationInfos(textureFormat, usage, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount))
}

func GraphicsService_createGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapId: UInt32, _ type: GraphicsServiceHeapType, _ length: UInt) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsHeap(UInt(graphicsHeapId), type, length) ? 1 : 0)
}

func GraphicsService_setGraphicsHeapLabelInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setGraphicsHeapLabel(UInt(graphicsHeapId), String(cString: label!))
}

func GraphicsService_deleteGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsHeap(UInt(graphicsHeapId))
}

func GraphicsService_createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ graphicsHeapId: UInt32, _ heapOffset: UInt, _ isAliasable: Int32, _ sizeInBytes: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createGraphicsBuffer(UInt(graphicsBufferId), UInt(graphicsHeapId), heapOffset, Bool(isAliasable == 1), Int(sizeInBytes)) ? 1 : 0)
}

func GraphicsService_setGraphicsBufferLabelInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setGraphicsBufferLabel(UInt(graphicsBufferId), String(cString: label!))
}

func GraphicsService_deleteGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsBuffer(UInt(graphicsBufferId))
}

func GraphicsService_getGraphicsBufferCpuPointerInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferId: UInt32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getGraphicsBufferCpuPointer(UInt(graphicsBufferId))
}

func GraphicsService_createTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32, _ graphicsHeapId: UInt32, _ heapOffset: UInt, _ isAliasable: Int32, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createTexture(UInt(textureId), UInt(graphicsHeapId), heapOffset, Bool(isAliasable == 1), textureFormat, usage, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount)) ? 1 : 0)
}

func GraphicsService_setTextureLabelInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setTextureLabel(UInt(textureId), String(cString: label!))
}

func GraphicsService_deleteTextureInterop(context: UnsafeMutableRawPointer?, _ textureId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteTexture(UInt(textureId))
}

func GraphicsService_createIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ indirectCommandBufferId: UInt32, _ maxCommandCount: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createIndirectCommandBuffer(UInt(indirectCommandBufferId), Int(maxCommandCount)) ? 1 : 0)
}

func GraphicsService_setIndirectCommandBufferLabelInterop(context: UnsafeMutableRawPointer?, _ indirectCommandBufferId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setIndirectCommandBufferLabel(UInt(indirectCommandBufferId), String(cString: label!))
}

func GraphicsService_deleteIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ indirectCommandBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteIndirectCommandBuffer(UInt(indirectCommandBufferId))
}

func GraphicsService_createShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32, _ computeShaderFunction: UnsafeMutablePointer<Int8>?, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createShader(UInt(shaderId), (computeShaderFunction != nil) ? String(cString: computeShaderFunction!) : nil, shaderByteCode!, Int(shaderByteCodeLength)) ? 1 : 0)
}

func GraphicsService_setShaderLabelInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderLabel(UInt(shaderId), String(cString: label!))
}

func GraphicsService_deleteShaderInterop(context: UnsafeMutableRawPointer?, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteShader(UInt(shaderId))
}

func GraphicsService_createPipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32, _ shaderId: UInt32, _ renderPassDescriptor: GraphicsRenderPassDescriptor) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createPipelineState(UInt(pipelineStateId), UInt(shaderId), renderPassDescriptor) ? 1 : 0)
}

func GraphicsService_setPipelineStateLabelInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineStateLabel(UInt(pipelineStateId), String(cString: label!))
}

func GraphicsService_deletePipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deletePipelineState(UInt(pipelineStateId))
}

func GraphicsService_createCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueueId: UInt32, _ commandQueueType: GraphicsServiceCommandType) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createCommandQueue(UInt(commandQueueId), commandQueueType) ? 1 : 0)
}

func GraphicsService_setCommandQueueLabelInterop(context: UnsafeMutableRawPointer?, _ commandQueueId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setCommandQueueLabel(UInt(commandQueueId), String(cString: label!))
}

func GraphicsService_deleteCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueueId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteCommandQueue(UInt(commandQueueId))
}

func GraphicsService_getCommandQueueTimestampFrequencyInterop(context: UnsafeMutableRawPointer?, _ commandQueueId: UInt32) -> UInt {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getCommandQueueTimestampFrequency(UInt(commandQueueId))
}

func GraphicsService_executeCommandListsInterop(context: UnsafeMutableRawPointer?, _ commandQueueId: UInt32, _ commandLists: UnsafeMutablePointer<UInt32>?, _ commandListsLength: Int32, _ isAwaitable: Int32) -> UInt {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.executeCommandLists(UInt(commandQueueId), Array(UnsafeBufferPointer(start: commandLists, count: Int(commandListsLength))), Bool(isAwaitable == 1))
}

func GraphicsService_waitForCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueueId: UInt32, _ commandQueueToWaitId: UInt32, _ fenceValue: UInt) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForCommandQueue(UInt(commandQueueId), UInt(commandQueueToWaitId), fenceValue)
}

func GraphicsService_waitForCommandQueueOnCpuInterop(context: UnsafeMutableRawPointer?, _ commandQueueToWaitId: UInt32, _ fenceValue: UInt) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForCommandQueueOnCpu(UInt(commandQueueToWaitId), fenceValue)
}

func GraphicsService_createCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ commandQueueId: UInt32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createCommandList(UInt(commandListId), UInt(commandQueueId)) ? 1 : 0)
}

func GraphicsService_setCommandListLabelInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setCommandListLabel(UInt(commandListId), String(cString: label!))
}

func GraphicsService_deleteCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteCommandList(UInt(commandListId))
}

func GraphicsService_resetCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetCommandList(UInt(commandListId))
}

func GraphicsService_commitCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.commitCommandList(UInt(commandListId))
}

func GraphicsService_createQueryBufferInterop(context: UnsafeMutableRawPointer?, _ queryBufferId: UInt32, _ queryBufferType: GraphicsQueryBufferType, _ length: Int32) -> Int32 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return Int32(contextObject.createQueryBuffer(UInt(queryBufferId), queryBufferType, Int(length)) ? 1 : 0)
}

func GraphicsService_setQueryBufferLabelInterop(context: UnsafeMutableRawPointer?, _ queryBufferId: UInt32, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setQueryBufferLabel(UInt(queryBufferId), String(cString: label!))
}

func GraphicsService_deleteQueryBufferInterop(context: UnsafeMutableRawPointer?, _ queryBufferId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteQueryBuffer(UInt(queryBufferId))
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

func GraphicsService_copyDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ destinationGraphicsBufferId: UInt32, _ sourceGraphicsBufferId: UInt32, _ length: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyDataToGraphicsBuffer(UInt(commandListId), UInt(destinationGraphicsBufferId), UInt(sourceGraphicsBufferId), Int(length))
}

func GraphicsService_copyDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ destinationTextureId: UInt32, _ sourceGraphicsBufferId: UInt32, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ slice: Int32, _ mipLevel: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyDataToTexture(UInt(commandListId), UInt(destinationTextureId), UInt(sourceGraphicsBufferId), textureFormat, Int(width), Int(height), Int(slice), Int(mipLevel))
}

func GraphicsService_copyTextureInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ destinationTextureId: UInt32, _ sourceTextureId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyTexture(UInt(commandListId), UInt(destinationTextureId), UInt(sourceTextureId))
}

func GraphicsService_resetIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_optimizeIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ indirectCommandListId: UInt32, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.optimizeIndirectCommandList(UInt(commandListId), UInt(indirectCommandListId), Int(maxCommandCount))
}

func GraphicsService_dispatchThreadsInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ threadCountX: UInt32, _ threadCountY: UInt32, _ threadCountZ: UInt32) -> Vector3 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.dispatchThreads(UInt(commandListId), UInt(threadCountX), UInt(threadCountY), UInt(threadCountZ))
}

func GraphicsService_beginRenderPassInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ renderPassDescriptor: GraphicsRenderPassDescriptor) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.beginRenderPass(UInt(commandListId), renderPassDescriptor)
}

func GraphicsService_endRenderPassInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.endRenderPass(UInt(commandListId))
}

func GraphicsService_setPipelineStateInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ pipelineStateId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineState(UInt(commandListId), UInt(pipelineStateId))
}

func GraphicsService_setShaderInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ shaderId: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShader(UInt(commandListId), UInt(shaderId))
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

func GraphicsService_queryTimestampInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ queryBufferId: UInt32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.queryTimestamp(UInt(commandListId), UInt(queryBufferId), Int(index))
}

func GraphicsService_resolveQueryDataInterop(context: UnsafeMutableRawPointer?, _ commandListId: UInt32, _ queryBufferId: UInt32, _ destinationBufferId: UInt32, _ startIndex: Int32, _ endIndex: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resolveQueryData(UInt(commandListId), UInt(queryBufferId), UInt(destinationBufferId), Int(startIndex), Int(endIndex))
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
    service.GraphicsService_SetGraphicsHeapLabel = GraphicsService_setGraphicsHeapLabelInterop
    service.GraphicsService_DeleteGraphicsHeap = GraphicsService_deleteGraphicsHeapInterop
    service.GraphicsService_CreateGraphicsBuffer = GraphicsService_createGraphicsBufferInterop
    service.GraphicsService_SetGraphicsBufferLabel = GraphicsService_setGraphicsBufferLabelInterop
    service.GraphicsService_DeleteGraphicsBuffer = GraphicsService_deleteGraphicsBufferInterop
    service.GraphicsService_GetGraphicsBufferCpuPointer = GraphicsService_getGraphicsBufferCpuPointerInterop
    service.GraphicsService_CreateTexture = GraphicsService_createTextureInterop
    service.GraphicsService_SetTextureLabel = GraphicsService_setTextureLabelInterop
    service.GraphicsService_DeleteTexture = GraphicsService_deleteTextureInterop
    service.GraphicsService_CreateIndirectCommandBuffer = GraphicsService_createIndirectCommandBufferInterop
    service.GraphicsService_SetIndirectCommandBufferLabel = GraphicsService_setIndirectCommandBufferLabelInterop
    service.GraphicsService_DeleteIndirectCommandBuffer = GraphicsService_deleteIndirectCommandBufferInterop
    service.GraphicsService_CreateShader = GraphicsService_createShaderInterop
    service.GraphicsService_SetShaderLabel = GraphicsService_setShaderLabelInterop
    service.GraphicsService_DeleteShader = GraphicsService_deleteShaderInterop
    service.GraphicsService_CreatePipelineState = GraphicsService_createPipelineStateInterop
    service.GraphicsService_SetPipelineStateLabel = GraphicsService_setPipelineStateLabelInterop
    service.GraphicsService_DeletePipelineState = GraphicsService_deletePipelineStateInterop
    service.GraphicsService_CreateCommandQueue = GraphicsService_createCommandQueueInterop
    service.GraphicsService_SetCommandQueueLabel = GraphicsService_setCommandQueueLabelInterop
    service.GraphicsService_DeleteCommandQueue = GraphicsService_deleteCommandQueueInterop
    service.GraphicsService_GetCommandQueueTimestampFrequency = GraphicsService_getCommandQueueTimestampFrequencyInterop
    service.GraphicsService_ExecuteCommandLists = GraphicsService_executeCommandListsInterop
    service.GraphicsService_WaitForCommandQueue = GraphicsService_waitForCommandQueueInterop
    service.GraphicsService_WaitForCommandQueueOnCpu = GraphicsService_waitForCommandQueueOnCpuInterop
    service.GraphicsService_CreateCommandList = GraphicsService_createCommandListInterop
    service.GraphicsService_SetCommandListLabel = GraphicsService_setCommandListLabelInterop
    service.GraphicsService_DeleteCommandList = GraphicsService_deleteCommandListInterop
    service.GraphicsService_ResetCommandList = GraphicsService_resetCommandListInterop
    service.GraphicsService_CommitCommandList = GraphicsService_commitCommandListInterop
    service.GraphicsService_CreateQueryBuffer = GraphicsService_createQueryBufferInterop
    service.GraphicsService_SetQueryBufferLabel = GraphicsService_setQueryBufferLabelInterop
    service.GraphicsService_DeleteQueryBuffer = GraphicsService_deleteQueryBufferInterop
    service.GraphicsService_SetShaderBuffer = GraphicsService_setShaderBufferInterop
    service.GraphicsService_SetShaderBuffers = GraphicsService_setShaderBuffersInterop
    service.GraphicsService_SetShaderTexture = GraphicsService_setShaderTextureInterop
    service.GraphicsService_SetShaderTextures = GraphicsService_setShaderTexturesInterop
    service.GraphicsService_SetShaderIndirectCommandList = GraphicsService_setShaderIndirectCommandListInterop
    service.GraphicsService_SetShaderIndirectCommandLists = GraphicsService_setShaderIndirectCommandListsInterop
    service.GraphicsService_CopyDataToGraphicsBuffer = GraphicsService_copyDataToGraphicsBufferInterop
    service.GraphicsService_CopyDataToTexture = GraphicsService_copyDataToTextureInterop
    service.GraphicsService_CopyTexture = GraphicsService_copyTextureInterop
    service.GraphicsService_ResetIndirectCommandList = GraphicsService_resetIndirectCommandListInterop
    service.GraphicsService_OptimizeIndirectCommandList = GraphicsService_optimizeIndirectCommandListInterop
    service.GraphicsService_DispatchThreads = GraphicsService_dispatchThreadsInterop
    service.GraphicsService_BeginRenderPass = GraphicsService_beginRenderPassInterop
    service.GraphicsService_EndRenderPass = GraphicsService_endRenderPassInterop
    service.GraphicsService_SetPipelineState = GraphicsService_setPipelineStateInterop
    service.GraphicsService_SetShader = GraphicsService_setShaderInterop
    service.GraphicsService_ExecuteIndirectCommandBuffer = GraphicsService_executeIndirectCommandBufferInterop
    service.GraphicsService_SetIndexBuffer = GraphicsService_setIndexBufferInterop
    service.GraphicsService_DrawIndexedPrimitives = GraphicsService_drawIndexedPrimitivesInterop
    service.GraphicsService_DrawPrimitives = GraphicsService_drawPrimitivesInterop
    service.GraphicsService_QueryTimestamp = GraphicsService_queryTimestampInterop
    service.GraphicsService_ResolveQueryData = GraphicsService_resolveQueryDataInterop
    service.GraphicsService_PresentScreenBuffer = GraphicsService_presentScreenBufferInterop
    service.GraphicsService_WaitForAvailableScreenBuffer = GraphicsService_waitForAvailableScreenBufferInterop
}

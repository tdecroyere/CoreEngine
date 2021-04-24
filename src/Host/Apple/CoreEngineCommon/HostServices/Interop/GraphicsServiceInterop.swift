import Foundation
import CoreEngineCommonInterop

func GraphicsService_getGraphicsAdapterNameInterop(context: UnsafeMutableRawPointer?, _ output: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.getGraphicsAdapterName(output)
}

func GraphicsService_getTextureAllocationInfosInterop(context: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32) -> GraphicsAllocationInfos {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getTextureAllocationInfos(textureFormat, usage, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount))
}

func GraphicsService_createCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueueType: GraphicsServiceCommandType) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createCommandQueue(commandQueueType)
}

func GraphicsService_setCommandQueueLabelInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setCommandQueueLabel(commandQueuePointer, String(cString: label!))
}

func GraphicsService_deleteCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteCommandQueue(commandQueuePointer)
}

func GraphicsService_resetCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetCommandQueue(commandQueuePointer)
}

func GraphicsService_getCommandQueueTimestampFrequencyInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?) -> UInt {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getCommandQueueTimestampFrequency(commandQueuePointer)
}

func GraphicsService_executeCommandListsInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ commandLists: UnsafeMutablePointer<UnsafeMutableRawPointer?>?, _ commandListsLength: Int32, _ isAwaitable: Int32) -> UInt {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.executeCommandLists(commandQueuePointer, Array(UnsafeBufferPointer(start: commandLists, count: Int(commandListsLength))), Bool(isAwaitable == 1))
}

func GraphicsService_waitForCommandQueueInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ commandQueueToWaitPointer: UnsafeMutableRawPointer?, _ fenceValue: UInt) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForCommandQueue(commandQueuePointer, commandQueueToWaitPointer, fenceValue)
}

func GraphicsService_waitForCommandQueueOnCpuInterop(context: UnsafeMutableRawPointer?, _ commandQueueToWaitPointer: UnsafeMutableRawPointer?, _ fenceValue: UInt) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForCommandQueueOnCpu(commandQueueToWaitPointer, fenceValue)
}

func GraphicsService_createCommandListInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createCommandList(commandQueuePointer)
}

func GraphicsService_setCommandListLabelInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setCommandListLabel(commandListPointer, String(cString: label!))
}

func GraphicsService_deleteCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteCommandList(commandListPointer)
}

func GraphicsService_resetCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetCommandList(commandListPointer)
}

func GraphicsService_commitCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.commitCommandList(commandListPointer)
}

func GraphicsService_createGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ type: GraphicsServiceHeapType, _ length: UInt) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createGraphicsHeap(type, length)
}

func GraphicsService_setGraphicsHeapLabelInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setGraphicsHeapLabel(graphicsHeapPointer, String(cString: label!))
}

func GraphicsService_deleteGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsHeap(graphicsHeapPointer)
}

func GraphicsService_createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Int32, _ sizeInBytes: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createGraphicsBuffer(graphicsHeapPointer, heapOffset, Bool(isAliasable == 1), Int(sizeInBytes))
}

func GraphicsService_setGraphicsBufferLabelInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setGraphicsBufferLabel(graphicsBufferPointer, String(cString: label!))
}

func GraphicsService_deleteGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsBuffer(graphicsBufferPointer)
}

func GraphicsService_getGraphicsBufferCpuPointerInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getGraphicsBufferCpuPointer(graphicsBufferPointer)
}

func GraphicsService_createTextureInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Int32, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int32, _ height: Int32, _ faceCount: Int32, _ mipLevels: Int32, _ multisampleCount: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createTexture(graphicsHeapPointer, heapOffset, Bool(isAliasable == 1), textureFormat, usage, Int(width), Int(height), Int(faceCount), Int(mipLevels), Int(multisampleCount))
}

func GraphicsService_setTextureLabelInterop(context: UnsafeMutableRawPointer?, _ texturePointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setTextureLabel(texturePointer, String(cString: label!))
}

func GraphicsService_deleteTextureInterop(context: UnsafeMutableRawPointer?, _ texturePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteTexture(texturePointer)
}

func GraphicsService_createSwapChainInterop(context: UnsafeMutableRawPointer?, _ windowPointer: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ width: Int32, _ height: Int32, _ textureFormat: GraphicsTextureFormat) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createSwapChain(windowPointer, commandQueuePointer, Int(width), Int(height), textureFormat)
}

func GraphicsService_resizeSwapChainInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?, _ width: Int32, _ height: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resizeSwapChain(swapChainPointer, Int(width), Int(height))
}

func GraphicsService_getSwapChainBackBufferTextureInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getSwapChainBackBufferTexture(swapChainPointer)
}

func GraphicsService_presentSwapChainInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.presentSwapChain(swapChainPointer)
}

func GraphicsService_waitForSwapChainOnCpuInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForSwapChainOnCpu(swapChainPointer)
}

func GraphicsService_createIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ maxCommandCount: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createIndirectCommandBuffer(Int(maxCommandCount))
}

func GraphicsService_setIndirectCommandBufferLabelInterop(context: UnsafeMutableRawPointer?, _ indirectCommandBufferPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setIndirectCommandBufferLabel(indirectCommandBufferPointer, String(cString: label!))
}

func GraphicsService_deleteIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ indirectCommandBufferPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteIndirectCommandBuffer(indirectCommandBufferPointer)
}

func GraphicsService_createQueryBufferInterop(context: UnsafeMutableRawPointer?, _ queryBufferType: GraphicsQueryBufferType, _ length: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createQueryBuffer(queryBufferType, Int(length))
}

func GraphicsService_setQueryBufferLabelInterop(context: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setQueryBufferLabel(queryBufferPointer, String(cString: label!))
}

func GraphicsService_deleteQueryBufferInterop(context: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteQueryBuffer(queryBufferPointer)
}

func GraphicsService_createShaderInterop(context: UnsafeMutableRawPointer?, _ computeShaderFunction: UnsafeMutablePointer<Int8>?, _ shaderByteCode: UnsafeMutableRawPointer?, _ shaderByteCodeLength: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createShader((computeShaderFunction != nil) ? String(cString: computeShaderFunction!) : nil, shaderByteCode!, Int(shaderByteCodeLength))
}

func GraphicsService_setShaderLabelInterop(context: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderLabel(shaderPointer, String(cString: label!))
}

func GraphicsService_deleteShaderInterop(context: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteShader(shaderPointer)
}

func GraphicsService_createPipelineStateInterop(context: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createPipelineState(shaderPointer, renderPassDescriptor)
}

func GraphicsService_setPipelineStateLabelInterop(context: UnsafeMutableRawPointer?, _ pipelineStatePointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineStateLabel(pipelineStatePointer, String(cString: label!))
}

func GraphicsService_deletePipelineStateInterop(context: UnsafeMutableRawPointer?, _ pipelineStatePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deletePipelineState(pipelineStatePointer)
}

func GraphicsService_setShaderBufferInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?, _ slot: Int32, _ isReadOnly: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderBuffer(commandListPointer, graphicsBufferPointer, Int(slot), Bool(isReadOnly == 1), Int(index))
}

func GraphicsService_setShaderBuffersInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointerList: UnsafeMutablePointer<UnsafeMutableRawPointer?>?, _ graphicsBufferPointerListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderBuffers(commandListPointer, Array(UnsafeBufferPointer(start: graphicsBufferPointerList, count: Int(graphicsBufferPointerListLength))), Int(slot), Int(index))
}

func GraphicsService_setShaderTextureInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ texturePointer: UnsafeMutableRawPointer?, _ slot: Int32, _ isReadOnly: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderTexture(commandListPointer, texturePointer, Int(slot), Bool(isReadOnly == 1), Int(index))
}

func GraphicsService_setShaderTexturesInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ texturePointerList: UnsafeMutablePointer<UnsafeMutableRawPointer?>?, _ texturePointerListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderTextures(commandListPointer, Array(UnsafeBufferPointer(start: texturePointerList, count: Int(texturePointerListLength))), Int(slot), Int(index))
}

func GraphicsService_setShaderIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderIndirectCommandList(commandListPointer, indirectCommandListPointer, Int(slot), Int(index))
}

func GraphicsService_setShaderIndirectCommandListsInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointerList: UnsafeMutablePointer<UnsafeMutableRawPointer?>?, _ indirectCommandListPointerListLength: Int32, _ slot: Int32, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderIndirectCommandLists(commandListPointer, Array(UnsafeBufferPointer(start: indirectCommandListPointerList, count: Int(indirectCommandListPointerListLength))), Int(slot), Int(index))
}

func GraphicsService_copyDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ destinationGraphicsBufferPointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ length: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyDataToGraphicsBuffer(commandListPointer, destinationGraphicsBufferPointer, sourceGraphicsBufferPointer, Int(length))
}

func GraphicsService_copyDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ slice: Int32, _ mipLevel: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyDataToTexture(commandListPointer, destinationTexturePointer, sourceGraphicsBufferPointer, textureFormat, Int(width), Int(height), Int(slice), Int(mipLevel))
}

func GraphicsService_copyTextureInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceTexturePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyTexture(commandListPointer, destinationTexturePointer, sourceTexturePointer)
}

func GraphicsService_resetIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetIndirectCommandList(commandListPointer, indirectCommandListPointer, Int(maxCommandCount))
}

func GraphicsService_optimizeIndirectCommandListInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.optimizeIndirectCommandList(commandListPointer, indirectCommandListPointer, Int(maxCommandCount))
}

func GraphicsService_dispatchThreadsInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ threadCountX: UInt32, _ threadCountY: UInt32, _ threadCountZ: UInt32) -> Vector3 {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.dispatchThreads(commandListPointer, UInt(threadCountX), UInt(threadCountY), UInt(threadCountZ))
}

func GraphicsService_beginRenderPassInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.beginRenderPass(commandListPointer, renderPassDescriptor)
}

func GraphicsService_endRenderPassInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.endRenderPass(commandListPointer)
}

func GraphicsService_setPipelineStateInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ pipelineStatePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setPipelineState(commandListPointer, pipelineStatePointer)
}

func GraphicsService_setShaderInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShader(commandListPointer, shaderPointer)
}

func GraphicsService_executeIndirectCommandBufferInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandBufferPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeIndirectCommandBuffer(commandListPointer, indirectCommandBufferPointer, Int(maxCommandCount))
}

func GraphicsService_setIndexBufferInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setIndexBuffer(commandListPointer, graphicsBufferPointer)
}

func GraphicsService_drawIndexedPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int32, _ indexCount: Int32, _ instanceCount: Int32, _ baseInstanceId: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawIndexedPrimitives(commandListPointer, primitiveType, Int(startIndex), Int(indexCount), Int(instanceCount), Int(baseInstanceId))
}

func GraphicsService_drawPrimitivesInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int32, _ vertexCount: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.drawPrimitives(commandListPointer, primitiveType, Int(startVertex), Int(vertexCount))
}

func GraphicsService_queryTimestampInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.queryTimestamp(commandListPointer, queryBufferPointer, Int(index))
}

func GraphicsService_resolveQueryDataInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ destinationBufferPointer: UnsafeMutableRawPointer?, _ startIndex: Int32, _ endIndex: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resolveQueryData(commandListPointer, queryBufferPointer, destinationBufferPointer, Int(startIndex), Int(endIndex))
}

func initGraphicsService(_ context: MetalGraphicsService, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GraphicsService_GetGraphicsAdapterName = GraphicsService_getGraphicsAdapterNameInterop
    service.GraphicsService_GetTextureAllocationInfos = GraphicsService_getTextureAllocationInfosInterop
    service.GraphicsService_CreateCommandQueue = GraphicsService_createCommandQueueInterop
    service.GraphicsService_SetCommandQueueLabel = GraphicsService_setCommandQueueLabelInterop
    service.GraphicsService_DeleteCommandQueue = GraphicsService_deleteCommandQueueInterop
    service.GraphicsService_ResetCommandQueue = GraphicsService_resetCommandQueueInterop
    service.GraphicsService_GetCommandQueueTimestampFrequency = GraphicsService_getCommandQueueTimestampFrequencyInterop
    service.GraphicsService_ExecuteCommandLists = GraphicsService_executeCommandListsInterop
    service.GraphicsService_WaitForCommandQueue = GraphicsService_waitForCommandQueueInterop
    service.GraphicsService_WaitForCommandQueueOnCpu = GraphicsService_waitForCommandQueueOnCpuInterop
    service.GraphicsService_CreateCommandList = GraphicsService_createCommandListInterop
    service.GraphicsService_SetCommandListLabel = GraphicsService_setCommandListLabelInterop
    service.GraphicsService_DeleteCommandList = GraphicsService_deleteCommandListInterop
    service.GraphicsService_ResetCommandList = GraphicsService_resetCommandListInterop
    service.GraphicsService_CommitCommandList = GraphicsService_commitCommandListInterop
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
    service.GraphicsService_CreateSwapChain = GraphicsService_createSwapChainInterop
    service.GraphicsService_ResizeSwapChain = GraphicsService_resizeSwapChainInterop
    service.GraphicsService_GetSwapChainBackBufferTexture = GraphicsService_getSwapChainBackBufferTextureInterop
    service.GraphicsService_PresentSwapChain = GraphicsService_presentSwapChainInterop
    service.GraphicsService_WaitForSwapChainOnCpu = GraphicsService_waitForSwapChainOnCpuInterop
    service.GraphicsService_CreateIndirectCommandBuffer = GraphicsService_createIndirectCommandBufferInterop
    service.GraphicsService_SetIndirectCommandBufferLabel = GraphicsService_setIndirectCommandBufferLabelInterop
    service.GraphicsService_DeleteIndirectCommandBuffer = GraphicsService_deleteIndirectCommandBufferInterop
    service.GraphicsService_CreateQueryBuffer = GraphicsService_createQueryBufferInterop
    service.GraphicsService_SetQueryBufferLabel = GraphicsService_setQueryBufferLabelInterop
    service.GraphicsService_DeleteQueryBuffer = GraphicsService_deleteQueryBufferInterop
    service.GraphicsService_CreateShader = GraphicsService_createShaderInterop
    service.GraphicsService_SetShaderLabel = GraphicsService_setShaderLabelInterop
    service.GraphicsService_DeleteShader = GraphicsService_deleteShaderInterop
    service.GraphicsService_CreatePipelineState = GraphicsService_createPipelineStateInterop
    service.GraphicsService_SetPipelineStateLabel = GraphicsService_setPipelineStateLabelInterop
    service.GraphicsService_DeletePipelineState = GraphicsService_deletePipelineStateInterop
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
}

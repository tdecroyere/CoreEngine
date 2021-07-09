import Foundation
import CoreEngineCommonInterop

func GraphicsService_getGraphicsAdapterNameInterop(context: UnsafeMutableRawPointer?, _ output: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.getGraphicsAdapterName(output)
}

func GraphicsService_getBufferAllocationInfosInterop(context: UnsafeMutableRawPointer?, _ sizeInBytes: Int32) -> GraphicsAllocationInfos {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getBufferAllocationInfos(Int(sizeInBytes))
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

func GraphicsService_executeCommandListsInterop(context: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ commandLists: UnsafeMutablePointer<UnsafeMutableRawPointer?>?, _ commandListsLength: Int32, _ fencesToWait: UnsafeMutablePointer<GraphicsFence>?, _ fencesToWaitLength: Int32) -> UInt {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.executeCommandLists(commandQueuePointer, Array(UnsafeBufferPointer(start: commandLists, count: Int(commandListsLength))), Array(UnsafeBufferPointer(start: fencesToWait, count: Int(fencesToWaitLength))))
}

func GraphicsService_waitForCommandQueueOnCpuInterop(context: UnsafeMutableRawPointer?, _ fenceToWait: GraphicsFence) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForCommandQueueOnCpu(fenceToWait)
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

func GraphicsService_createGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ type: GraphicsServiceHeapType, _ sizeInBytes: UInt) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createGraphicsHeap(type, sizeInBytes)
}

func GraphicsService_setGraphicsHeapLabelInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setGraphicsHeapLabel(graphicsHeapPointer, String(cString: label!))
}

func GraphicsService_deleteGraphicsHeapInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteGraphicsHeap(graphicsHeapPointer)
}

func GraphicsService_createShaderResourceHeapInterop(context: UnsafeMutableRawPointer?, _ length: UInt) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createShaderResourceHeap(length)
}

func GraphicsService_setShaderResourceHeapLabelInterop(context: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ label: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderResourceHeapLabel(shaderResourceHeapPointer, String(cString: label!))
}

func GraphicsService_deleteShaderResourceHeapInterop(context: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteShaderResourceHeap(shaderResourceHeapPointer)
}

func GraphicsService_createShaderResourceTextureInterop(context: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt32, _ texturePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.createShaderResourceTexture(shaderResourceHeapPointer, UInt(index), texturePointer)
}

func GraphicsService_deleteShaderResourceTextureInterop(context: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteShaderResourceTexture(shaderResourceHeapPointer, UInt(index))
}

func GraphicsService_createShaderResourceBufferInterop(context: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt32, _ bufferPointer: UnsafeMutableRawPointer?, _ isWriteable: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.createShaderResourceBuffer(shaderResourceHeapPointer, UInt(index), bufferPointer, Bool(isWriteable == 1))
}

func GraphicsService_deleteShaderResourceBufferInterop(context: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteShaderResourceBuffer(shaderResourceHeapPointer, UInt(index))
}

func GraphicsService_createGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ graphicsBufferUsage: GraphicsBufferUsage, _ sizeInBytes: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createGraphicsBuffer(graphicsHeapPointer, heapOffset, graphicsBufferUsage, Int(sizeInBytes))
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

func GraphicsService_releaseGraphicsBufferCpuPointerInterop(context: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.releaseGraphicsBufferCpuPointer(graphicsBufferPointer)
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

func GraphicsService_deleteSwapChainInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.deleteSwapChain(swapChainPointer)
}

func GraphicsService_resizeSwapChainInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?, _ width: Int32, _ height: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resizeSwapChain(swapChainPointer, Int(width), Int(height))
}

func GraphicsService_getSwapChainBackBufferTextureInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getSwapChainBackBufferTexture(swapChainPointer)
}

func GraphicsService_presentSwapChainInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) -> UInt {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.presentSwapChain(swapChainPointer)
}

func GraphicsService_waitForSwapChainOnCpuInterop(context: UnsafeMutableRawPointer?, _ swapChainPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.waitForSwapChainOnCpu(swapChainPointer)
}

func GraphicsService_createQueryBufferInterop(context: UnsafeMutableRawPointer?, _ queryBufferType: GraphicsQueryBufferType, _ length: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createQueryBuffer(queryBufferType, Int(length))
}

func GraphicsService_resetQueryBufferInterop(context: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resetQueryBuffer(queryBufferPointer)
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

func GraphicsService_createComputePipelineStateInterop(context: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createComputePipelineState(shaderPointer)
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

func GraphicsService_copyDataToGraphicsBufferInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ destinationGraphicsBufferPointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ sizeInBytes: UInt32, _ destinationOffsetInBytes: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyDataToGraphicsBuffer(commandListPointer, destinationGraphicsBufferPointer, sourceGraphicsBufferPointer, UInt(sizeInBytes), UInt(destinationOffsetInBytes))
}

func GraphicsService_copyDataToTextureInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ width: Int32, _ height: Int32, _ slice: Int32, _ mipLevel: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyDataToTexture(commandListPointer, destinationTexturePointer, sourceGraphicsBufferPointer, textureFormat, Int(width), Int(height), Int(slice), Int(mipLevel))
}

func GraphicsService_copyTextureInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceTexturePointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.copyTexture(commandListPointer, destinationTexturePointer, sourceTexturePointer)
}

func GraphicsService_transitionGraphicsBufferToStateInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?, _ resourceState: GraphicsResourceState) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.transitionGraphicsBufferToState(commandListPointer, graphicsBufferPointer, resourceState)
}

func GraphicsService_dispatchThreadsInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ threadGroupCountX: UInt32, _ threadGroupCountY: UInt32, _ threadGroupCountZ: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.dispatchThreads(commandListPointer, UInt(threadGroupCountX), UInt(threadGroupCountY), UInt(threadGroupCountZ))
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

func GraphicsService_setShaderResourceHeapInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderResourceHeap(commandListPointer, shaderResourceHeapPointer)
}

func GraphicsService_setShaderInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShader(commandListPointer, shaderPointer)
}

func GraphicsService_setShaderParameterValuesInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ slot: UInt32, _ values: UnsafeMutablePointer<UInt32>?, _ valuesLength: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setShaderParameterValues(commandListPointer, UInt(slot), Array(UnsafeBufferPointer(start: values, count: Int(valuesLength))))
}

func GraphicsService_dispatchMeshInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ threadGroupCountX: UInt32, _ threadGroupCountY: UInt32, _ threadGroupCountZ: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.dispatchMesh(commandListPointer, UInt(threadGroupCountX), UInt(threadGroupCountY), UInt(threadGroupCountZ))
}

func GraphicsService_executeIndirectInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: UInt32, _ commandGraphicsBufferPointer: UnsafeMutableRawPointer?, _ commandBufferOffset: UInt32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.executeIndirect(commandListPointer, UInt(maxCommandCount), commandGraphicsBufferPointer, UInt(commandBufferOffset))
}

func GraphicsService_beginQueryInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.beginQuery(commandListPointer, queryBufferPointer, Int(index))
}

func GraphicsService_endQueryInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.endQuery(commandListPointer, queryBufferPointer, Int(index))
}

func GraphicsService_resolveQueryDataInterop(context: UnsafeMutableRawPointer?, _ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ destinationBufferPointer: UnsafeMutableRawPointer?, _ startIndex: Int32, _ endIndex: Int32) {
    let contextObject = Unmanaged<MetalGraphicsService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.resolveQueryData(commandListPointer, queryBufferPointer, destinationBufferPointer, Int(startIndex), Int(endIndex))
}

func initGraphicsService(_ context: MetalGraphicsService, _ service: inout GraphicsService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.GraphicsService_GetGraphicsAdapterName = GraphicsService_getGraphicsAdapterNameInterop
    service.GraphicsService_GetBufferAllocationInfos = GraphicsService_getBufferAllocationInfosInterop
    service.GraphicsService_GetTextureAllocationInfos = GraphicsService_getTextureAllocationInfosInterop
    service.GraphicsService_CreateCommandQueue = GraphicsService_createCommandQueueInterop
    service.GraphicsService_SetCommandQueueLabel = GraphicsService_setCommandQueueLabelInterop
    service.GraphicsService_DeleteCommandQueue = GraphicsService_deleteCommandQueueInterop
    service.GraphicsService_ResetCommandQueue = GraphicsService_resetCommandQueueInterop
    service.GraphicsService_GetCommandQueueTimestampFrequency = GraphicsService_getCommandQueueTimestampFrequencyInterop
    service.GraphicsService_ExecuteCommandLists = GraphicsService_executeCommandListsInterop
    service.GraphicsService_WaitForCommandQueueOnCpu = GraphicsService_waitForCommandQueueOnCpuInterop
    service.GraphicsService_CreateCommandList = GraphicsService_createCommandListInterop
    service.GraphicsService_SetCommandListLabel = GraphicsService_setCommandListLabelInterop
    service.GraphicsService_DeleteCommandList = GraphicsService_deleteCommandListInterop
    service.GraphicsService_ResetCommandList = GraphicsService_resetCommandListInterop
    service.GraphicsService_CommitCommandList = GraphicsService_commitCommandListInterop
    service.GraphicsService_CreateGraphicsHeap = GraphicsService_createGraphicsHeapInterop
    service.GraphicsService_SetGraphicsHeapLabel = GraphicsService_setGraphicsHeapLabelInterop
    service.GraphicsService_DeleteGraphicsHeap = GraphicsService_deleteGraphicsHeapInterop
    service.GraphicsService_CreateShaderResourceHeap = GraphicsService_createShaderResourceHeapInterop
    service.GraphicsService_SetShaderResourceHeapLabel = GraphicsService_setShaderResourceHeapLabelInterop
    service.GraphicsService_DeleteShaderResourceHeap = GraphicsService_deleteShaderResourceHeapInterop
    service.GraphicsService_CreateShaderResourceTexture = GraphicsService_createShaderResourceTextureInterop
    service.GraphicsService_DeleteShaderResourceTexture = GraphicsService_deleteShaderResourceTextureInterop
    service.GraphicsService_CreateShaderResourceBuffer = GraphicsService_createShaderResourceBufferInterop
    service.GraphicsService_DeleteShaderResourceBuffer = GraphicsService_deleteShaderResourceBufferInterop
    service.GraphicsService_CreateGraphicsBuffer = GraphicsService_createGraphicsBufferInterop
    service.GraphicsService_SetGraphicsBufferLabel = GraphicsService_setGraphicsBufferLabelInterop
    service.GraphicsService_DeleteGraphicsBuffer = GraphicsService_deleteGraphicsBufferInterop
    service.GraphicsService_GetGraphicsBufferCpuPointer = GraphicsService_getGraphicsBufferCpuPointerInterop
    service.GraphicsService_ReleaseGraphicsBufferCpuPointer = GraphicsService_releaseGraphicsBufferCpuPointerInterop
    service.GraphicsService_CreateTexture = GraphicsService_createTextureInterop
    service.GraphicsService_SetTextureLabel = GraphicsService_setTextureLabelInterop
    service.GraphicsService_DeleteTexture = GraphicsService_deleteTextureInterop
    service.GraphicsService_CreateSwapChain = GraphicsService_createSwapChainInterop
    service.GraphicsService_DeleteSwapChain = GraphicsService_deleteSwapChainInterop
    service.GraphicsService_ResizeSwapChain = GraphicsService_resizeSwapChainInterop
    service.GraphicsService_GetSwapChainBackBufferTexture = GraphicsService_getSwapChainBackBufferTextureInterop
    service.GraphicsService_PresentSwapChain = GraphicsService_presentSwapChainInterop
    service.GraphicsService_WaitForSwapChainOnCpu = GraphicsService_waitForSwapChainOnCpuInterop
    service.GraphicsService_CreateQueryBuffer = GraphicsService_createQueryBufferInterop
    service.GraphicsService_ResetQueryBuffer = GraphicsService_resetQueryBufferInterop
    service.GraphicsService_SetQueryBufferLabel = GraphicsService_setQueryBufferLabelInterop
    service.GraphicsService_DeleteQueryBuffer = GraphicsService_deleteQueryBufferInterop
    service.GraphicsService_CreateShader = GraphicsService_createShaderInterop
    service.GraphicsService_SetShaderLabel = GraphicsService_setShaderLabelInterop
    service.GraphicsService_DeleteShader = GraphicsService_deleteShaderInterop
    service.GraphicsService_CreateComputePipelineState = GraphicsService_createComputePipelineStateInterop
    service.GraphicsService_CreatePipelineState = GraphicsService_createPipelineStateInterop
    service.GraphicsService_SetPipelineStateLabel = GraphicsService_setPipelineStateLabelInterop
    service.GraphicsService_DeletePipelineState = GraphicsService_deletePipelineStateInterop
    service.GraphicsService_CopyDataToGraphicsBuffer = GraphicsService_copyDataToGraphicsBufferInterop
    service.GraphicsService_CopyDataToTexture = GraphicsService_copyDataToTextureInterop
    service.GraphicsService_CopyTexture = GraphicsService_copyTextureInterop
    service.GraphicsService_TransitionGraphicsBufferToState = GraphicsService_transitionGraphicsBufferToStateInterop
    service.GraphicsService_DispatchThreads = GraphicsService_dispatchThreadsInterop
    service.GraphicsService_BeginRenderPass = GraphicsService_beginRenderPassInterop
    service.GraphicsService_EndRenderPass = GraphicsService_endRenderPassInterop
    service.GraphicsService_SetPipelineState = GraphicsService_setPipelineStateInterop
    service.GraphicsService_SetShaderResourceHeap = GraphicsService_setShaderResourceHeapInterop
    service.GraphicsService_SetShader = GraphicsService_setShaderInterop
    service.GraphicsService_SetShaderParameterValues = GraphicsService_setShaderParameterValuesInterop
    service.GraphicsService_DispatchMesh = GraphicsService_dispatchMeshInterop
    service.GraphicsService_ExecuteIndirect = GraphicsService_executeIndirectInterop
    service.GraphicsService_BeginQuery = GraphicsService_beginQueryInterop
    service.GraphicsService_EndQuery = GraphicsService_endQueryInterop
    service.GraphicsService_ResolveQueryData = GraphicsService_resolveQueryDataInterop
}

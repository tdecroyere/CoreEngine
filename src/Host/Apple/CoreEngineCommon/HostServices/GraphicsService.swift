import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getGraphicsAdapterName(_ output: UnsafeMutablePointer<Int8>?)
    func getRenderSize() -> Vector2
    func getTextureAllocationInfos(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> GraphicsAllocationInfos
    func createGraphicsHeap(_ graphicsHeapId: UInt, _ type: GraphicsServiceHeapType, _ length: UInt) -> Bool
    func setGraphicsHeapLabel(_ graphicsHeapId: UInt, _ label: String)
    func deleteGraphicsHeap(_ graphicsHeapId: UInt)
    func createGraphicsBuffer(_ graphicsBufferId: UInt, _ graphicsHeapId: UInt, _ heapOffset: UInt, _ isAliasable: Bool, _ sizeInBytes: Int) -> Bool
    func setGraphicsBufferLabel(_ graphicsBufferId: UInt, _ label: String)
    func deleteGraphicsBuffer(_ graphicsBufferId: UInt)
    func getGraphicsBufferCpuPointer(_ graphicsBufferId: UInt) -> UnsafeMutableRawPointer?
    func createTexture(_ textureId: UInt, _ graphicsHeapId: UInt, _ heapOffset: UInt, _ isAliasable: Bool, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> Bool
    func setTextureLabel(_ textureId: UInt, _ label: String)
    func deleteTexture(_ textureId: UInt)
    func createIndirectCommandBuffer(_ indirectCommandBufferId: UInt, _ maxCommandCount: Int) -> Bool
    func setIndirectCommandBufferLabel(_ indirectCommandBufferId: UInt, _ label: String)
    func deleteIndirectCommandBuffer(_ indirectCommandBufferId: UInt)
    func createShader(_ shaderId: UInt, _ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> Bool
    func setShaderLabel(_ shaderId: UInt, _ label: String)
    func deleteShader(_ shaderId: UInt)
    func createPipelineState(_ pipelineStateId: UInt, _ shaderId: UInt, _ renderPassDescriptor: GraphicsRenderPassDescriptor) -> Bool
    func setPipelineStateLabel(_ pipelineStateId: UInt, _ label: String)
    func deletePipelineState(_ pipelineStateId: UInt)
    func createCommandQueue(_ commandQueueId: UInt, _ commandQueueType: GraphicsServiceCommandType) -> Bool
    func setCommandQueueLabel(_ commandQueueId: UInt, _ label: String)
    func deleteCommandQueue(_ commandQueueId: UInt)
    func getCommandQueueTimestampFrequency(_ commandQueueId: UInt) -> UInt
    func executeCommandLists(_ commandQueueId: UInt, _ commandLists: [UInt32], _ signalFence: Bool) -> UInt
    func waitForCommandQueue(_ commandQueueId: UInt, _ commandQueueToWaitId: UInt, _ fenceValue: UInt)
    func waitForCommandQueueOnCpu(_ commandQueueToWaitId: UInt, _ fenceValue: UInt)
    func createCommandList(_ commandListId: UInt, _ commandQueueId: UInt) -> Bool
    func setCommandListLabel(_ commandListId: UInt, _ label: String)
    func deleteCommandList(_ commandListId: UInt)
    func resetCommandList(_ commandListId: UInt)
    func commitCommandList(_ commandListId: UInt)
    func createQueryBuffer(_ queryBufferId: UInt, _ queryBufferType: GraphicsQueryBufferType, _ length: Int) -> Bool
    func setQueryBufferLabel(_ queryBufferId: UInt, _ label: String)
    func deleteQueryBuffer(_ queryBufferId: UInt)
    func setShaderBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ slot: Int, _ isReadOnly: Bool, _ index: Int)
    func setShaderBuffers(_ commandListId: UInt, _ graphicsBufferIdList: [UInt32], _ slot: Int, _ index: Int)
    func setShaderTexture(_ commandListId: UInt, _ textureId: UInt, _ slot: Int, _ isReadOnly: Bool, _ index: Int)
    func setShaderTextures(_ commandListId: UInt, _ textureIdList: [UInt32], _ slot: Int, _ index: Int)
    func setShaderIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ slot: Int, _ index: Int)
    func setShaderIndirectCommandLists(_ commandListId: UInt, _ indirectCommandListIdList: [UInt32], _ slot: Int, _ index: Int)
    func copyDataToGraphicsBuffer(_ commandListId: UInt, _ destinationGraphicsBufferId: UInt, _ sourceGraphicsBufferId: UInt, _ length: Int)
    func copyDataToTexture(_ commandListId: UInt, _ destinationTextureId: UInt, _ sourceGraphicsBufferId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int)
    func copyTexture(_ commandListId: UInt, _ destinationTextureId: UInt, _ sourceTextureId: UInt)
    func resetIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int)
    func optimizeIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int)
    func dispatchThreads(_ commandListId: UInt, _ threadCountX: UInt, _ threadCountY: UInt, _ threadCountZ: UInt) -> Vector3
    func beginRenderPass(_ commandListId: UInt, _ renderPassDescriptor: GraphicsRenderPassDescriptor)
    func endRenderPass(_ commandListId: UInt)
    func setPipelineState(_ commandListId: UInt, _ pipelineStateId: UInt)
    func setShader(_ commandListId: UInt, _ shaderId: UInt)
    func executeIndirectCommandBuffer(_ commandListId: UInt, _ indirectCommandBufferId: UInt, _ maxCommandCount: Int)
    func setIndexBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt)
    func drawIndexedPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ instanceCount: Int, _ baseInstanceId: Int)
    func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int, _ vertexCount: Int)
    func queryTimestamp(_ commandListId: UInt, _ queryBufferId: UInt, _ index: Int)
    func resolveQueryData(_ commandListId: UInt, _ queryBufferId: UInt, _ destinationBufferId: UInt, _ startIndex: Int, _ endIndex: Int)
    func presentScreenBuffer(_ commandBufferId: UInt)
    func waitForAvailableScreenBuffer()
}

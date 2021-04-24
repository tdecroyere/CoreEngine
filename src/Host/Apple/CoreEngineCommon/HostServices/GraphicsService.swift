import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getGraphicsAdapterName(_ output: UnsafeMutablePointer<Int8>?)
    func getTextureAllocationInfos(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> GraphicsAllocationInfos
    func createCommandQueue(_ commandQueueType: GraphicsServiceCommandType) -> UnsafeMutableRawPointer?
    func setCommandQueueLabel(_ commandQueuePointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?)
    func resetCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?)
    func getCommandQueueTimestampFrequency(_ commandQueuePointer: UnsafeMutableRawPointer?) -> UInt
    func executeCommandLists(_ commandQueuePointer: UnsafeMutableRawPointer?, _ commandLists: [UnsafeMutableRawPointer?], _ isAwaitable: Bool) -> UInt
    func waitForCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?, _ commandQueueToWaitPointer: UnsafeMutableRawPointer?, _ fenceValue: UInt)
    func waitForCommandQueueOnCpu(_ commandQueueToWaitPointer: UnsafeMutableRawPointer?, _ fenceValue: UInt)
    func createCommandList(_ commandQueuePointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func setCommandListLabel(_ commandListPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteCommandList(_ commandListPointer: UnsafeMutableRawPointer?)
    func resetCommandList(_ commandListPointer: UnsafeMutableRawPointer?)
    func commitCommandList(_ commandListPointer: UnsafeMutableRawPointer?)
    func createGraphicsHeap(_ type: GraphicsServiceHeapType, _ length: UInt) -> UnsafeMutableRawPointer?
    func setGraphicsHeapLabel(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteGraphicsHeap(_ graphicsHeapPointer: UnsafeMutableRawPointer?)
    func createGraphicsBuffer(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Bool, _ sizeInBytes: Int) -> UnsafeMutableRawPointer?
    func setGraphicsBufferLabel(_ graphicsBufferPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteGraphicsBuffer(_ graphicsBufferPointer: UnsafeMutableRawPointer?)
    func getGraphicsBufferCpuPointer(_ graphicsBufferPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func createTexture(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Bool, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> UnsafeMutableRawPointer?
    func setTextureLabel(_ texturePointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteTexture(_ texturePointer: UnsafeMutableRawPointer?)
    func createSwapChain(_ windowPointer: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ width: Int, _ height: Int, _ textureFormat: GraphicsTextureFormat) -> UnsafeMutableRawPointer?
    func resizeSwapChain(_ swapChainPointer: UnsafeMutableRawPointer?, _ width: Int, _ height: Int)
    func getSwapChainBackBufferTexture(_ swapChainPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func presentSwapChain(_ swapChainPointer: UnsafeMutableRawPointer?)
    func waitForSwapChainOnCpu(_ swapChainPointer: UnsafeMutableRawPointer?)
    func createIndirectCommandBuffer(_ maxCommandCount: Int) -> UnsafeMutableRawPointer?
    func setIndirectCommandBufferLabel(_ indirectCommandBufferPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteIndirectCommandBuffer(_ indirectCommandBufferPointer: UnsafeMutableRawPointer?)
    func createQueryBuffer(_ queryBufferType: GraphicsQueryBufferType, _ length: Int) -> UnsafeMutableRawPointer?
    func setQueryBufferLabel(_ queryBufferPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteQueryBuffer(_ queryBufferPointer: UnsafeMutableRawPointer?)
    func createShader(_ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UnsafeMutableRawPointer?
    func setShaderLabel(_ shaderPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteShader(_ shaderPointer: UnsafeMutableRawPointer?)
    func createPipelineState(_ shaderPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor) -> UnsafeMutableRawPointer?
    func setPipelineStateLabel(_ pipelineStatePointer: UnsafeMutableRawPointer?, _ label: String)
    func deletePipelineState(_ pipelineStatePointer: UnsafeMutableRawPointer?)
    func setShaderBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?, _ slot: Int, _ isReadOnly: Bool, _ index: Int)
    func setShaderBuffers(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointerList: [UnsafeMutableRawPointer?], _ slot: Int, _ index: Int)
    func setShaderTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ texturePointer: UnsafeMutableRawPointer?, _ slot: Int, _ isReadOnly: Bool, _ index: Int)
    func setShaderTextures(_ commandListPointer: UnsafeMutableRawPointer?, _ texturePointerList: [UnsafeMutableRawPointer?], _ slot: Int, _ index: Int)
    func setShaderIndirectCommandList(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ slot: Int, _ index: Int)
    func setShaderIndirectCommandLists(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointerList: [UnsafeMutableRawPointer?], _ slot: Int, _ index: Int)
    func copyDataToGraphicsBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationGraphicsBufferPointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ length: Int)
    func copyDataToTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int)
    func copyTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceTexturePointer: UnsafeMutableRawPointer?)
    func resetIndirectCommandList(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int)
    func optimizeIndirectCommandList(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int)
    func dispatchThreads(_ commandListPointer: UnsafeMutableRawPointer?, _ threadCountX: UInt, _ threadCountY: UInt, _ threadCountZ: UInt) -> Vector3
    func beginRenderPass(_ commandListPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor)
    func endRenderPass(_ commandListPointer: UnsafeMutableRawPointer?)
    func setPipelineState(_ commandListPointer: UnsafeMutableRawPointer?, _ pipelineStatePointer: UnsafeMutableRawPointer?)
    func setShader(_ commandListPointer: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?)
    func executeIndirectCommandBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandBufferPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int)
    func setIndexBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?)
    func drawIndexedPrimitives(_ commandListPointer: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ instanceCount: Int, _ baseInstanceId: Int)
    func drawPrimitives(_ commandListPointer: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int, _ vertexCount: Int)
    func queryTimestamp(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int)
    func resolveQueryData(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ destinationBufferPointer: UnsafeMutableRawPointer?, _ startIndex: Int, _ endIndex: Int)
}

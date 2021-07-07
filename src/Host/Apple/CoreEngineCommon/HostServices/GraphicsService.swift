import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getGraphicsAdapterName(_ output: UnsafeMutablePointer<Int8>?)
    func getBufferAllocationInfos(_ sizeInBytes: Int) -> GraphicsAllocationInfos
    func getTextureAllocationInfos(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> GraphicsAllocationInfos
    func createCommandQueue(_ commandQueueType: GraphicsServiceCommandType) -> UnsafeMutableRawPointer?
    func setCommandQueueLabel(_ commandQueuePointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?)
    func resetCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?)
    func getCommandQueueTimestampFrequency(_ commandQueuePointer: UnsafeMutableRawPointer?) -> UInt
    func executeCommandLists(_ commandQueuePointer: UnsafeMutableRawPointer?, _ commandLists: [UnsafeMutableRawPointer?], _ fencesToWait: [GraphicsFence]) -> UInt
    func waitForCommandQueueOnCpu(_ fenceToWait: GraphicsFence)
    func createCommandList(_ commandQueuePointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func setCommandListLabel(_ commandListPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteCommandList(_ commandListPointer: UnsafeMutableRawPointer?)
    func resetCommandList(_ commandListPointer: UnsafeMutableRawPointer?)
    func commitCommandList(_ commandListPointer: UnsafeMutableRawPointer?)
    func createGraphicsHeap(_ type: GraphicsServiceHeapType, _ sizeInBytes: UInt) -> UnsafeMutableRawPointer?
    func setGraphicsHeapLabel(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteGraphicsHeap(_ graphicsHeapPointer: UnsafeMutableRawPointer?)
    func createShaderResourceHeap(_ length: UInt) -> UnsafeMutableRawPointer?
    func setShaderResourceHeapLabel(_ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteShaderResourceHeap(_ shaderResourceHeapPointer: UnsafeMutableRawPointer?)
    func createShaderResourceTexture(_ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt, _ texturePointer: UnsafeMutableRawPointer?)
    func deleteShaderResourceTexture(_ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt)
    func createShaderResourceBuffer(_ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt, _ bufferPointer: UnsafeMutableRawPointer?)
    func deleteShaderResourceBuffer(_ shaderResourceHeapPointer: UnsafeMutableRawPointer?, _ index: UInt)
    func createGraphicsBuffer(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ graphicsBufferUsage: GraphicsBufferUsage, _ sizeInBytes: Int) -> UnsafeMutableRawPointer?
    func setGraphicsBufferLabel(_ graphicsBufferPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteGraphicsBuffer(_ graphicsBufferPointer: UnsafeMutableRawPointer?)
    func getGraphicsBufferCpuPointer(_ graphicsBufferPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func releaseGraphicsBufferCpuPointer(_ graphicsBufferPointer: UnsafeMutableRawPointer?)
    func createTexture(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Bool, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> UnsafeMutableRawPointer?
    func setTextureLabel(_ texturePointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteTexture(_ texturePointer: UnsafeMutableRawPointer?)
    func createSwapChain(_ windowPointer: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ width: Int, _ height: Int, _ textureFormat: GraphicsTextureFormat) -> UnsafeMutableRawPointer?
    func deleteSwapChain(_ swapChainPointer: UnsafeMutableRawPointer?)
    func resizeSwapChain(_ swapChainPointer: UnsafeMutableRawPointer?, _ width: Int, _ height: Int)
    func getSwapChainBackBufferTexture(_ swapChainPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func presentSwapChain(_ swapChainPointer: UnsafeMutableRawPointer?) -> UInt
    func waitForSwapChainOnCpu(_ swapChainPointer: UnsafeMutableRawPointer?)
    func createQueryBuffer(_ queryBufferType: GraphicsQueryBufferType, _ length: Int) -> UnsafeMutableRawPointer?
    func resetQueryBuffer(_ queryBufferPointer: UnsafeMutableRawPointer?)
    func setQueryBufferLabel(_ queryBufferPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteQueryBuffer(_ queryBufferPointer: UnsafeMutableRawPointer?)
    func createShader(_ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UnsafeMutableRawPointer?
    func setShaderLabel(_ shaderPointer: UnsafeMutableRawPointer?, _ label: String)
    func deleteShader(_ shaderPointer: UnsafeMutableRawPointer?)
    func createComputePipelineState(_ shaderPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer?
    func createPipelineState(_ shaderPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor) -> UnsafeMutableRawPointer?
    func setPipelineStateLabel(_ pipelineStatePointer: UnsafeMutableRawPointer?, _ label: String)
    func deletePipelineState(_ pipelineStatePointer: UnsafeMutableRawPointer?)
    func copyDataToGraphicsBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationGraphicsBufferPointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ length: Int)
    func copyDataToTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int)
    func copyTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceTexturePointer: UnsafeMutableRawPointer?)
    func transitionGraphicsBufferToState(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?, _ resourceState: GraphicsResourceState)
    func dispatchThreads(_ commandListPointer: UnsafeMutableRawPointer?, _ threadGroupCountX: UInt, _ threadGroupCountY: UInt, _ threadGroupCountZ: UInt)
    func beginRenderPass(_ commandListPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor)
    func endRenderPass(_ commandListPointer: UnsafeMutableRawPointer?)
    func setPipelineState(_ commandListPointer: UnsafeMutableRawPointer?, _ pipelineStatePointer: UnsafeMutableRawPointer?)
    func setShaderResourceHeap(_ commandListPointer: UnsafeMutableRawPointer?, _ shaderResourceHeapPointer: UnsafeMutableRawPointer?)
    func setShader(_ commandListPointer: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?)
    func setShaderParameterValues(_ commandListPointer: UnsafeMutableRawPointer?, _ slot: UInt, _ values: [UInt32])
    func dispatchMesh(_ commandListPointer: UnsafeMutableRawPointer?, _ threadGroupCountX: UInt, _ threadGroupCountY: UInt, _ threadGroupCountZ: UInt)
    func dispatchMeshIndirect(_ commandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: UInt, _ commandGraphicsBufferPointer: UnsafeMutableRawPointer?, _ commandBufferOffset: UInt, _ commandSizeInBytes: UInt)
    func beginQuery(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int)
    func endQuery(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int)
    func resolveQueryData(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ destinationBufferPointer: UnsafeMutableRawPointer?, _ startIndex: Int, _ endIndex: Int)
}

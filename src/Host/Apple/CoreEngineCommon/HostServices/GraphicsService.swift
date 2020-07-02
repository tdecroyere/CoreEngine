import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getRenderSize() -> Vector2
    func getGraphicsAdapterName() -> String
    func createGraphicsBuffer(_ graphicsBufferId: UInt, _ length: Int, _ isWriteOnly: Bool, _ label: String) -> Bool
    func createTexture(_ textureId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int, _ isRenderTarget: Bool, _ label: String) -> Bool
    func deleteTexture(_ textureId: UInt)
    func createIndirectCommandBuffer(_ indirectCommandBufferId: UInt, _ maxCommandCount: Int, _ label: String) -> Bool
    func createShader(_ shaderId: UInt, _ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int, _ label: String) -> Bool
    func deleteShader(_ shaderId: UInt)
    func createPipelineState(_ pipelineStateId: UInt, _ shaderId: UInt, _ renderPassDescriptor: GraphicsRenderPassDescriptor, _ label: String) -> Bool
    func deletePipelineState(_ pipelineStateId: UInt)
    func createCommandBuffer(_ commandBufferId: UInt, _ commandBufferType: GraphicsCommandBufferType, _ label: String) -> Bool
    func deleteCommandBuffer(_ commandBufferId: UInt)
    func resetCommandBuffer(_ commandBufferId: UInt)
    func executeCommandBuffer(_ commandBufferId: UInt)
    func getCommandBufferStatus(_ commandBufferId: UInt) -> NullableGraphicsCommandBufferStatus
    func setShaderBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ slot: Int, _ isReadOnly: Bool, _ index: Int)
    func setShaderBuffers(_ commandListId: UInt, _ graphicsBufferIdList: [UInt32], _ slot: Int, _ index: Int)
    func setShaderTexture(_ commandListId: UInt, _ textureId: UInt, _ slot: Int, _ isReadOnly: Bool, _ index: Int)
    func setShaderTextures(_ commandListId: UInt, _ textureIdList: [UInt32], _ slot: Int, _ index: Int)
    func setShaderIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ slot: Int, _ index: Int)
    func setShaderIndirectCommandLists(_ commandListId: UInt, _ indirectCommandListIdList: [UInt32], _ slot: Int, _ index: Int)
    func createCopyCommandList(_ commandListId: UInt, _ commandBufferId: UInt, _ label: String) -> Bool
    func commitCopyCommandList(_ commandListId: UInt)
    func uploadDataToGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func copyGraphicsBufferDataToCpu(_ commandListId: UInt, _ graphicsBufferId: UInt, _ length: Int)
    func readGraphicsBufferData(_ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func uploadDataToTexture(_ commandListId: UInt, _ textureId: UInt, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func resetIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int)
    func optimizeIndirectCommandList(_ commandListId: UInt, _ indirectCommandListId: UInt, _ maxCommandCount: Int)
    func createComputeCommandList(_ commandListId: UInt, _ commandBufferId: UInt, _ label: String) -> Bool
    func commitComputeCommandList(_ commandListId: UInt)
    func dispatchThreads(_ commandListId: UInt, _ threadCountX: UInt, _ threadCountY: UInt, _ threadCountZ: UInt) -> Vector3
    func createRenderCommandList(_ commandListId: UInt, _ commandBufferId: UInt, _ renderDescriptor: GraphicsRenderPassDescriptor, _ label: String) -> Bool
    func commitRenderCommandList(_ commandListId: UInt)
    func setPipelineState(_ commandListId: UInt, _ pipelineStateId: UInt)
    func setShader(_ commandListId: UInt, _ shaderId: UInt)
    func executeIndirectCommandBuffer(_ commandListId: UInt, _ indirectCommandBufferId: UInt, _ maxCommandCount: Int)
    func setIndexBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt)
    func drawIndexedPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ instanceCount: Int, _ baseInstanceId: Int)
    func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int, _ vertexCount: Int)
    func waitForCommandList(_ commandListId: UInt, _ commandListToWaitId: UInt)
    func presentScreenBuffer(_ commandBufferId: UInt)
    func waitForAvailableScreenBuffer()
}

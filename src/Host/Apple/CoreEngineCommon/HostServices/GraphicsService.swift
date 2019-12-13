import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getRenderSize() -> Vector2
    func createGraphicsBuffer(_ graphicsBufferId: UInt, _ length: Int, _ debugName: String?) -> Bool
    func createTexture(_ textureId: UInt, _ width: Int, _ height: Int, _ isRenderTarget: Bool, _ debugName: String?) -> Bool
    func removeTexture(_ textureId: UInt)
    func createShader(_ shaderId: UInt, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int, _ useDepthBuffer: Bool, _ debugName: String?) -> Bool
    func removeShader(_ shaderId: UInt)
    func createCopyCommandList(_ commandListId: UInt, _ debugName: String?, _ createNewCommandBuffer: Bool) -> Bool
    func executeCopyCommandList(_ commandListId: UInt)
    func uploadDataToGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func uploadDataToTexture(_ commandListId: UInt, _ textureId: UInt, _ width: Int, _ height: Int, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func createRenderCommandList(_ commandListId: UInt, _ renderDescriptor: GraphicsRenderPassDescriptor, _ debugName: String?, _ createNewCommandBuffer: Bool) -> Bool
    func executeRenderCommandList(_ commandListId: UInt)
    func setShader(_ commandListId: UInt, _ shaderId: UInt)
    func setShaderBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ slot: Int, _ index: Int)
    func setShaderBuffers(_ commandListId: UInt, _ graphicsBufferIdList: [UInt32], _ slot: Int, _ index: Int)
    func setShaderTexture(_ commandListId: UInt, _ textureId: UInt, _ slot: Int, _ index: Int)
    func setShaderTextures(_ commandListId: UInt, _ textureIdList: [UInt32], _ slot: Int, _ index: Int)
    func setIndexBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt)
    func drawIndexedPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ instanceCount: Int, _ baseInstanceId: Int)
    func presentScreenBuffer()
}

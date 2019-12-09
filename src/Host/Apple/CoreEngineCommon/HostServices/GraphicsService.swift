import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getRenderSize() -> Vector2
    func createPipelineState(_ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UInt
    func removePipelineState(_ pipelineStateId: UInt)
    func createShaderParameters(_ graphicsResourceId: UInt, _ pipelineStateId: UInt, _ graphicsBuffer1: UInt, _ graphicsBuffer2: UInt, _ graphicsBuffer3: UInt) -> Bool
    func createGraphicsBuffer(_ graphicsResourceId: UInt, _ length: Int) -> Bool
    func createTexture(_ graphicsResourceId: UInt, _ width: Int, _ height: Int) -> Bool
    func createCopyCommandList() -> UInt
    func executeCopyCommandList(_ commandListId: UInt)
    func uploadDataToGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func uploadDataToTexture(_ commandListId: UInt, _ textureId: UInt, _ width: Int, _ height: Int, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func createRenderCommandList() -> UInt
    func executeRenderCommandList(_ commandListId: UInt)
    func setPipelineState(_ commandListId: UInt, _ pipelineStateId: UInt)
    func setGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt)
    func setTexture(_ commandListId: UInt, _ textureId: UInt, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt)
    func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ vertexBufferId: UInt, _ indexBufferId: UInt, _ instanceCount: Int, _ baseInstanceId: Int)
    func presentScreenBuffer()
}

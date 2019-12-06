import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getRenderSize() -> Vector2
    func createPipelineState(_ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UInt
    func removePipelineState(_ pipelineStateId: UInt)
    func createShaderParameters(_ pipelineStateId: UInt, _ graphicsBuffer1: UInt, _ graphicsBuffer2: UInt, _ graphicsBuffer3: UInt) -> UInt
    func createGraphicsBuffer(_ length: Int) -> UInt
    func createCopyCommandList() -> UInt
    func executeCopyCommandList(_ commandListId: UInt)
    func uploadDataToGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func createRenderCommandList() -> UInt
    func executeRenderCommandList(_ commandListId: UInt)
    func setPipelineState(_ commandListId: UInt, _ pipelineStateId: UInt)
    func setGraphicsBuffer(_ commandListId: UInt, _ graphicsBufferId: UInt, _ graphicsBindStage: GraphicsBindStage, _ slot: UInt)
    func drawPrimitives(_ commandListId: UInt, _ primitiveType: GraphicsPrimitiveType, _ startIndex: UInt, _ indexCount: UInt, _ vertexBufferId: UInt, _ indexBufferId: UInt, _ baseInstanceId: UInt)
    func presentScreenBuffer()
}

import CoreEngineCommonInterop

public protocol GraphicsServiceProtocol {
    func getRenderSize() -> Vector2
    func createShader(_ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UInt
    func createShaderParameters(_ graphicsBuffer1: UInt, _ graphicsBuffer2: UInt, _ graphicsBuffer3: UInt) -> UInt
    func createStaticGraphicsBuffer(_ data: UnsafeMutableRawPointer, _ dataLength: Int) -> UInt
    func createDynamicGraphicsBuffer(_ length: Int) -> UInt
    func uploadDataToGraphicsBuffer(_ graphicsBufferId: UInt, _ data: UnsafeMutableRawPointer, _ dataLength: Int)
    func beginCopyGpuData()
    func endCopyGpuData()
    func beginRender()
    func endRender()
    func drawPrimitives(_ primitiveType: GraphicsPrimitiveType, _ startIndex: UInt, _ indexCount: UInt, _ vertexBufferId: UInt, _ indexBufferId: UInt, _ baseInstanceId: UInt)
}

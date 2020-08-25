import CoreEngineCommonInterop

public protocol NativeUIServiceProtocol {
    func createWindow(_ title: String, _ width: Int, _ height: Int) -> UnsafeMutableRawPointer?
    func getWindowRenderSize(_ windowPointer: UnsafeMutableRawPointer?) -> Vector2
}

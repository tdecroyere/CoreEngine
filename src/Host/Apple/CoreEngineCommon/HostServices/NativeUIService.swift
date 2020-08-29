import CoreEngineCommonInterop

public protocol NativeUIServiceProtocol {
    func createWindow(_ title: String, _ width: Int, _ height: Int, _ windowState: NativeWindowState) -> UnsafeMutableRawPointer?
    func getWindowRenderSize(_ windowPointer: UnsafeMutableRawPointer?) -> Vector2
    func processSystemMessages() -> NativeAppStatus
}

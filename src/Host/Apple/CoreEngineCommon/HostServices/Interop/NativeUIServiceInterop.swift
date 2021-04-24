import Foundation
import CoreEngineCommonInterop

func NativeUIService_createWindowInterop(context: UnsafeMutableRawPointer?, _ title: UnsafeMutablePointer<Int8>?, _ width: Int32, _ height: Int32, _ windowState: NativeWindowState) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MacOSNativeUIService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createWindow(String(cString: title!), Int(width), Int(height), windowState)
}

func NativeUIService_setWindowTitleInterop(context: UnsafeMutableRawPointer?, _ windowPointer: UnsafeMutableRawPointer?, _ title: UnsafeMutablePointer<Int8>?) {
    let contextObject = Unmanaged<MacOSNativeUIService>.fromOpaque(context!).takeUnretainedValue()
    contextObject.setWindowTitle(windowPointer, String(cString: title!))
}

func NativeUIService_getWindowRenderSizeInterop(context: UnsafeMutableRawPointer?, _ windowPointer: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MacOSNativeUIService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getWindowRenderSize(windowPointer)
}

func NativeUIService_processSystemMessagesInterop(context: UnsafeMutableRawPointer?) -> NativeAppStatus {
    let contextObject = Unmanaged<MacOSNativeUIService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.processSystemMessages()
}

func initNativeUIService(_ context: MacOSNativeUIService, _ service: inout NativeUIService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.NativeUIService_CreateWindow = NativeUIService_createWindowInterop
    service.NativeUIService_SetWindowTitle = NativeUIService_setWindowTitleInterop
    service.NativeUIService_GetWindowRenderSize = NativeUIService_getWindowRenderSizeInterop
    service.NativeUIService_ProcessSystemMessages = NativeUIService_processSystemMessagesInterop
}

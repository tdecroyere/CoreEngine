import Foundation
import CoreEngineCommonInterop

func NativeUIService_createWindowInterop(context: UnsafeMutableRawPointer?, _ title: UnsafeMutablePointer<Int8>?, _ width: Int32, _ height: Int32) -> UnsafeMutableRawPointer? {
    let contextObject = Unmanaged<MacOSNativeUIService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.createWindow(String(cString: title!), Int(width), Int(height))
}

func NativeUIService_getWindowRenderSizeInterop(context: UnsafeMutableRawPointer?, _ windowPointer: UnsafeMutableRawPointer?) -> Vector2 {
    let contextObject = Unmanaged<MacOSNativeUIService>.fromOpaque(context!).takeUnretainedValue()
    return contextObject.getWindowRenderSize(windowPointer)
}

func initNativeUIService(_ context: MacOSNativeUIService, _ service: inout NativeUIService) {
    service.Context = Unmanaged.passUnretained(context).toOpaque()
    service.NativeUIService_CreateWindow = NativeUIService_createWindowInterop
    service.NativeUIService_GetWindowRenderSize = NativeUIService_getWindowRenderSizeInterop
}

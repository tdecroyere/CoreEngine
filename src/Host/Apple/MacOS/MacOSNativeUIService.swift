import Cocoa
import CoreEngineCommonInterop
import GameController

class MacOSWindow {
    let windowObject: NSWindow
    let metalView: MacOSMetalView
    let mainScreenScaling: CGFloat

    init (_ windowObject: NSWindow, _ metalView: MacOSMetalView, _ mainScreenScaling: CGFloat) {
        self.windowObject = windowObject
        self.metalView = metalView
        self.mainScreenScaling = mainScreenScaling
    }
}

public class MacOSNativeUIService: NativeUIServiceProtocol {
    public init() {
     
    }

    public func createWindow(_ title: String, _ width: Int, _ height: Int, _ windowState: NativeWindowState) -> UnsafeMutableRawPointer? {
        let window = NSWindow(contentRect: NSMakeRect(0, 0, CGFloat(width), CGFloat(height)), 
                                styleMask: [.resizable, .titled, .miniaturizable, .closable], 
                                backing: .buffered, 
                                defer: false)

        window.title = title

        let contentView = window.contentView! as NSView
        let metalView = MacOSMetalView()
        metalView.frame = contentView.frame
        contentView.addSubview(metalView)

        window.center()
        window.makeKeyAndOrderFront(nil)

        if (windowState == Maximized) {
            window.setFrame(window.screen!.visibleFrame, display: true, animate: false)
        }

        let mainScreenScaling = window.screen!.backingScaleFactor

        let nativeWindow = MacOSWindow(window, metalView, mainScreenScaling)
        return Unmanaged.passRetained(nativeWindow).toOpaque()
    }

    public func getWindowRenderSize(_ windowPointer: UnsafeMutableRawPointer?) -> Vector2 {
        let window = Unmanaged<MacOSWindow>.fromOpaque(windowPointer!).takeUnretainedValue()

        let contentView = window.windowObject.contentView! as NSView
        
        var size = contentView.frame.size
        size.width *= window.mainScreenScaling;
        size.height *= window.mainScreenScaling;

        return Vector2(X: Float(size.width), Y: Float(size.height))
    }

    public func processSystemMessages() -> NativeAppStatus {
        // TODO: Do we need an auto release pool if we carrefully delete objc objects manually?

        var appStatus = NativeAppStatus()
        //appStatus.IsActive = (delegate.mainWindow.occlusionState.rawValue != 8194) ? 1 : 0

        var rawEvent: NSEvent? = nil

        repeat {
            if (appStatus.IsActive == 1) {
                rawEvent = NSApplication.shared.nextEvent(matching: .any, until: NSDate.distantFuture, inMode: .default, dequeue: true)
            } else {
                rawEvent = NSApplication.shared.nextEvent(matching: .any, until: nil, inMode: .default, dequeue: true)
            }

            guard let event = rawEvent else {
                var appStatus = NativeAppStatus()
                appStatus.IsRunning = 1
                appStatus.IsActive = 1
                return appStatus
            }
            
            switch event.type {
            // case .keyUp, .keyDown:
            //     if (!event.modifierFlags.contains(.command)) {
            //         inputsManager.processKeyboardEvent(event)
            //     } else {
            //         NSApplication.shared.sendEvent(event)
            //     }
            // case .mouseMoved, .leftMouseDragged:
            //     inputsManager.processMouseMovedEvent(event)
            //     NSApplication.shared.sendEvent(event)
            // case .leftMouseUp, .leftMouseDown:
            //     // TODO: Prevent the event to be catched when dragging the window title
            //     inputsManager.processMouseLeftButtonEvent(event)
            //     NSApplication.shared.sendEvent(event)
            default:
                NSApplication.shared.sendEvent(event)
            }
        } while (rawEvent != nil && !isGamePaused)

        appStatus.IsRunning = 1
        appStatus.IsActive = 1//(delegate.mainWindow.occlusionState.rawValue != 8194) ? 1 : 0

        return appStatus
    }
}

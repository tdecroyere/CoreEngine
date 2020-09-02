import Cocoa

var isGameRunning = true
var isGamePaused = false

func processPendingMessages(inputsManager: InputsManager) {
    var rawEvent: NSEvent? = nil

    repeat {
        if (isGamePaused) {
            rawEvent = NSApplication.shared.nextEvent(matching: .any, until: NSDate.distantFuture, inMode: .default, dequeue: true)
        } else {
            rawEvent = NSApplication.shared.nextEvent(matching: .any, until: nil, inMode: .default, dequeue: true)
        }

        guard let event = rawEvent else {
            return
        }
        
        switch event.type {
        case .keyUp, .keyDown:
            if (!event.modifierFlags.contains(.command)) {
                inputsManager.processKeyboardEvent(event)
            } else {
                NSApplication.shared.sendEvent(event)
            }
        case .mouseMoved, .leftMouseDragged:
            inputsManager.processMouseMovedEvent(event)
            NSApplication.shared.sendEvent(event)
        case .leftMouseUp, .leftMouseDown:
            // TODO: Prevent the event to be catched when dragging the window title
            inputsManager.processMouseLeftButtonEvent(event)
            NSApplication.shared.sendEvent(event)
        default:
            NSApplication.shared.sendEvent(event)
        }
    } while (rawEvent != nil && !isGamePaused)
}

autoreleasepool {
    print("CoreEngine MacOS Host")

    let delegate = MacOSAppDelegate()
    NSApplication.shared.delegate = delegate
    NSApplication.shared.activate(ignoringOtherApps: true)
    NSApplication.shared.finishLaunching()

    let nativeUIService = MacOSNativeUIService()
    let graphicsService = MetalGraphicsService()
    let inputsManager = InputsManager()

    // while (delegate.renderer == nil) {
    //     processPendingMessages(inputsManager: inputsManager)
    // }

    var assemblyName = "CoreEngine"

    if (CommandLine.arguments.count > 1 && CommandLine.arguments[1] != "-NSDocumentRevisionsDebugMode") {
        assemblyName = "CoreEngine-" + CommandLine.arguments[1]
    }

    let coreEngineHost = CoreEngineHost(nativeUIService: nativeUIService, graphicsService: graphicsService, inputsManager: inputsManager)

    autoreleasepool {
        coreEngineHost.startEngine(assemblyName)
    }

    // while (isGameRunning) {
    //     autoreleasepool {
    //         processPendingMessages(inputsManager: inputsManager)

    //         isGamePaused = (delegate.mainWindow.occlusionState.rawValue != 8194)

    //         if (!isGamePaused) {
    //             inputsManager.processGamepadControllers()
    //         }
    //     }
    // }
}

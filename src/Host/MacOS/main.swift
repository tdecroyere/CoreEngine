import Cocoa
import CoreEngineInterop

var keyLeftPressed = false
var keyRightPressed = false
var keyUpPressed = false
var keyDownPressed = false

var isGameRunning = true
var isGamePaused = false

func processPendingMessages(inputsManager: MacOSInputsManager) {
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

    let memoryManager = MacOSMemoryManager()
    let inputsManager = MacOSInputsManager()

    while (delegate.renderer == nil) {
        processPendingMessages(inputsManager: inputsManager)
    }

    // TODO: Sometimes it seems there is a malloc error but not all the time (See MacOSCrash_20190324.txt)
    let renderer = delegate.renderer!

    var appName: String? = nil

    if (CommandLine.arguments.count > 1) {
        appName = CommandLine.arguments[1]
    }

    let coreEngineHost = MacOSCoreEngineHost(memoryManager: memoryManager, renderer: renderer, inputsManager: inputsManager)
    coreEngineHost.startEngine(appName)

    // Update is called currently at 60 fps because metal rendering is syncing the draw at 60Hz
    let stepTimeInSeconds = Float(1.0 / 60.0)

    while (isGameRunning) {
        autoreleasepool {
            processPendingMessages(inputsManager: inputsManager)

            isGamePaused = (delegate.mainWindow.occlusionState.rawValue != 8194)

            if (!isGamePaused) {
                inputsManager.processGamepadControllers()
                coreEngineHost.updateEngine(stepTimeInSeconds)
                renderer.presentScreenBuffer()
            }
        }
    }
}

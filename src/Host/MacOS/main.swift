import Cocoa
import CoreEngineInterop

var keyLeftPressed = false
var keyRightPressed = false
var keyUpPressed = false
var keyDownPressed = false

var gameRunning = true

func processPendingMessages(inputsManager: MacOSInputsManager) {
    var rawEvent: NSEvent? = nil

    repeat {
        rawEvent = NSApplication.shared.nextEvent(matching: .any, until: nil, inMode: .default, dequeue: true)

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
        default:
            NSApplication.shared.sendEvent(event)
        }
    } while (rawEvent != nil)
}

autoreleasepool {
    print("CoreEngine MacOS Host")

    let delegate = MacOSAppDelegate()
    NSApplication.shared.delegate = delegate
    NSApplication.shared.activate(ignoringOtherApps: true)
    NSApplication.shared.finishLaunching()

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

    let coreEngineHost = MacOSCoreEngineHost(renderer: renderer, inputsManager: inputsManager)
    coreEngineHost.startEngine(appName)

    // var machTimebaseInfo = mach_timebase_info(numer: 0, denom: 0)
    // mach_timebase_info(&machTimebaseInfo)

    // var lastCounter = mach_absolute_time()
    let stepTimeInSeconds = Float(1.0 / 60.0)

    while (gameRunning) {
        autoreleasepool {
            // Update is called currently at 60 fps because metal rendering is syncing the draw at 60Hz
            processPendingMessages(inputsManager: inputsManager)

            // let currentCounter = mach_absolute_time()

            // // TODO: Precise frame time calculation is not used for the moment
            // let elapsed = currentCounter - lastCounter
            // let nanoSeconds = elapsed * UInt64(machTimebaseInfo.numer) / UInt64(machTimebaseInfo.denom)
            // let milliSeconds = Double(nanoSeconds) / 1_000_000
            // lastCounter = currentCounter

            // TODO: Implement Draw triangle debug function
            renderer.beginRender()
            
            // TODO: Update at 60Hz for now
            coreEngineHost.updateEngine(stepTimeInSeconds)

            renderer.endRender()
            renderer.mtkView.draw()
        }
    }
}

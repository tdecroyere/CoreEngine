import Cocoa

var keyLeftPressed = false
var keyRightPressed = false
var keyUpPressed = false
var keyDownPressed = false

var gameRunning = true

func processPendingMessages() {
    var rawEvent: NSEvent? = nil

    repeat {
        rawEvent = NSApplication.shared.nextEvent(matching: .any, until: nil, inMode: .default, dequeue: true)

        guard let event = rawEvent else {
            return
        }
        
        switch event.type {
        case .keyUp, .keyDown:
            let keyCode = event.keyCode
            
            if (keyCode == 123) { // Left Arrow
                keyLeftPressed = (event.type == .keyDown)
            } else if (keyCode == 124) { // Right Arrow
                keyRightPressed = (event.type == .keyDown)
            } else if (keyCode == 126) { // Up Arrow
                keyUpPressed = (event.type == .keyDown)
            } else if (keyCode == 125) { // Down Arrow
                keyDownPressed = (event.type == .keyDown)
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

    while (delegate.renderer == nil) {
        processPendingMessages()
    }

    // TODO: Sometimes it seems there is a malloc error but not all the time (See MacOSCrash_20190324.txt)
    let renderer = delegate.renderer!

    var appName: String? = nil

    if (CommandLine.arguments.count > 1) {
        appName = CommandLine.arguments[1]
    }

    let coreEngineHost = MacOSCoreEngineHost(renderer: renderer)
    coreEngineHost.startEngine(appName)

    // var machTimebaseInfo = mach_timebase_info(numer: 0, denom: 0)
    // mach_timebase_info(&machTimebaseInfo)

    // var lastCounter = mach_absolute_time()
    let stepTimeInSeconds = Float(1.0 / 60.0)

    while (gameRunning) {
        autoreleasepool {
            // Update is called currently at 60 fps because metal rendering is syncing the draw at 60Hz
            processPendingMessages()

            // let currentCounter = mach_absolute_time()

            // // TODO: Precise frame time calculation is not used for the moment
            // let elapsed = currentCounter - lastCounter
            // let nanoSeconds = elapsed * UInt64(machTimebaseInfo.numer) / UInt64(machTimebaseInfo.denom)
            // let milliSeconds = Double(nanoSeconds) / 1_000_000
            // lastCounter = currentCounter

            // TODO: Build input system
            if (keyLeftPressed) {
                print(coreEngineHost.hostPlatform.InputsService.Keyboard.KeyQ.Value)
                coreEngineHost.hostPlatform.InputsService.Keyboard.KeyQ.Value = 1.0
                print(coreEngineHost.hostPlatform.InputsService.Keyboard.KeyQ.Value)
            }
            
            // if (keyRightPressed) {
            //     delegate.renderer.currentRotationY -= 50.0 * stepTimeInSeconds
            // }

            // if (keyUpPressed) {
            //     delegate.renderer.currentRotationX += 50.0 * stepTimeInSeconds
            // }
            
            // if (keyDownPressed) {
            //     delegate.renderer.currentRotationX -= 50.0 * stepTimeInSeconds
            // }

            // TODO: Implement Draw triangle debug function
            renderer.beginRender()
            
            // TODO: Update at 60Hz for now
            coreEngineHost.updateEngine(stepTimeInSeconds)

            renderer.endRender()
            renderer.mtkView.draw()
        }
    }
}

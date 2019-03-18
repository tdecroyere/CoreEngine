import Cocoa
import CoreEngineInterop

var startEnginePointer: StartEnginePtr?
var updateEnginePointer: UpdateEnginePtr?

var keyLeftPressed = false
var keyRightPressed = false
var keyUpPressed = false
var keyDownPressed = false

func getTestBuffer() -> Span
{
    let bufferPtr = UnsafeMutablePointer<UInt8>.allocate(capacity: 5)
    let buffer = UnsafeMutableBufferPointer(start: bufferPtr, count: 5)
    buffer[0] = 1
    buffer[1] = 2
    buffer[2] = 3
    buffer[3] = 45
    buffer[4] = 5

	return Span(Buffer: bufferPtr, Length: 5)
}

func addTestHostMethod(_ a: Int32, _ b: Int32) -> Int32
{
	return a + b + 40
}

func initCoreClrSwift() {
    let appPath = Bundle.main.bundleURL.appendingPathComponent("Contents/CoreClr")
    let coreLibPath = appPath.appendingPathComponent("libcoreclr.dylib").path
    
    let handle = dlopen(coreLibPath, RTLD_NOW | RTLD_LOCAL)
    
    let coreClrInitializeHandle = dlsym(handle, "coreclr_initialize")
    let coreclr_initialize = unsafeBitCast(coreClrInitializeHandle, to: coreclr_initialize_ptr.self)
    
    let coreClrCreateDelegateHandle = dlsym(handle, "coreclr_create_delegate")
    let coreclr_create_delegate = unsafeBitCast(coreClrCreateDelegateHandle, to: coreclr_create_delegate_ptr.self)

    // TPA List
    let fileManager = FileManager.default
    var tpaList: [String] = []
    
    do {
        var assembliesList = try fileManager.contentsOfDirectory(at: appPath, includingPropertiesForKeys: nil, options: [])
        assembliesList = assembliesList.filter{ $0.pathExtension == "dll" }
        
        for assembly in assembliesList {
            tpaList.append(assembly.path)
            //print(assembly.path)
        }
        //print("TPA list:", tpaList.joined(separator: ":") )

    } catch {
        print("ERROR: Could not find CoreCLR path")
    }
    
    let propertyKeys = [ "TRUSTED_PLATFORM_ASSEMBLIES" ]
    let propertyValues = [ tpaList.joined(separator: ":") ]
    var hostHandle: UnsafeMutableRawPointer? = nil
    var domainId: UInt32 = 0
    var managedDelegate: UnsafeMutableRawPointer? = nil
    
    var cargs = propertyKeys.map { UnsafePointer(strdup($0)) }
    var cargs2 = propertyValues.map { UnsafePointer(strdup($0)) }
    
    defer {
        //cargs.forEach { free($0) }
    }
    var result = coreclr_initialize(appPath.path,
                                    "CoreEngineAppDomain",
                                    1,
                                    &cargs,
                                    &cargs2,
                                    &hostHandle,
                                    &domainId)

    if (result == 0) {
        result = coreclr_create_delegate(hostHandle!, 
                                        domainId,
                                        "CoreEngine",
                                        "CoreEngine.Bootloader",
                                        "StartEngine",
                                        &managedDelegate)

        if (result == 0) {
            startEnginePointer = unsafeBitCast(managedDelegate!, to: StartEnginePtr.self)
        }

        result = coreclr_create_delegate(hostHandle!, 
                                        domainId,
                                        "CoreEngine",
                                        "CoreEngine.Bootloader",
                                        "UpdateEngine",
                                        &managedDelegate)

        if (result == 0) {
            updateEnginePointer = unsafeBitCast(managedDelegate!, to: UpdateEnginePtr.self)
        }
    }
    
    // TODO: Do not forget to call the shutdownCoreClr method
    
    //dlclose(handle)
}

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

    initCoreClrSwift()

    var appName: String = nil

    var hostPlatform = HostPlatform()
    hostPlatform.TestParameter = 5

    if (CommandLine.arguments.count > 1) {
        appName = CommandLine.arguments[1]
    }

    let addTestMethod: AddTestHostMethodPtr = addTestHostMethod
    hostPlatform.AddTestHostMethod = unsafeBitCast(addTestMethod, to: UnsafeMutableRawPointer.self)

    let getTestBufferMethod: GetTestBufferPtr = getTestBuffer
    hostPlatform.GetTestBuffer = unsafeBitCast(getTestBufferMethod, to: UnsafeMutableRawPointer.self)

    let delegate = MacOSAppDelegate()
    NSApplication.shared.delegate = delegate
    NSApplication.shared.activate(ignoringOtherApps: true)
    NSApplication.shared.finishLaunching()

    guard let startEngine = startEnginePointer else {
        print("CoreEngine StartEngine method is not initialized")
        return
    }

    guard let updateEngine = updateEnginePointer else {
        print("CoreEngine UpdatEngine method is not initialized")
        return
    }

    startEngine(appName, &hostPlatform)

    while (gameRunning) {
        processPendingMessages()

        if (keyLeftPressed) {
            delegate.renderer.currentRotationY += 0.005
        }
        
        if (keyRightPressed) {
            delegate.renderer.currentRotationY -= 0.005
        }

        if (keyUpPressed) {
            delegate.renderer.currentRotationX += 0.005
        }
        
        if (keyDownPressed) {
            delegate.renderer.currentRotationX -= 0.005
        }

        updateEngine(0)
    }
}

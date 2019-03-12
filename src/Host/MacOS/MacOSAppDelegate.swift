import Cocoa
import Metal
import MetalKit
import CoreEngineInterop

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

class AppDelegate: NSObject, NSApplicationDelegate {
    var mainWindow: NSWindow!
    var mainController: NSViewController!
    var metalDevice: MTLDevice!
    var mtkView: MTKView!
    var renderer: Renderer!

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        print("CoreEngine MacOS Host")
        self.buildMainMenu()

        self.mainWindow = NSWindow(contentRect: NSMakeRect(0, 0, 1280, 720), 
                                   styleMask: [.resizable, .titled, .miniaturizable, .closable], 
                                   backing: .buffered, 
                                   defer: false)

        self.mainWindow.title = "Core Engine"

        self.mainController = NSViewController()
        self.mtkView = MTKView()
        self.mtkView.translatesAutoresizingMaskIntoConstraints = false

        self.mainController.view = self.mtkView
        
        let content = self.mainWindow.contentView! as NSView
        let view = self.mainController.view
        content.addSubview(view)
        content.addConstraints(NSLayoutConstraint.constraints(withVisualFormat: "|[mtkView]|", options: [], metrics: nil, views: ["mtkView" : mtkView]))
        content.addConstraints(NSLayoutConstraint.constraints(withVisualFormat: "V:|[mtkView]|", options: [], metrics: nil, views: ["mtkView" : mtkView]))

        self.mainWindow.center()
        self.mainWindow.makeKeyAndOrderFront(nil)

        // Select the device to render with.  We choose the default device
        guard let defaultDevice = MTLCreateSystemDefaultDevice() else {
            print("Metal is not supported on this device")
            return
        }

        print(defaultDevice.name)
        self.mtkView.device = defaultDevice
        self.mtkView.colorPixelFormat = .bgra8Unorm

        self.renderer = Renderer(view: self.mtkView, device: defaultDevice)
        self.mtkView.delegate = renderer
        
        self.initCoreClrSwift()
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
                let startEngine = unsafeBitCast(managedDelegate!, to: StartEnginePtr.self)

                var hostPlatform = HostPlatform()
                hostPlatform.TestParameter = 5

                if (CommandLine.arguments.count > 1) {
                    hostPlatform.AppName = strdup(CommandLine.arguments[1])
                }

                let addTestMethod: AddTestHostMethodPtr = addTestHostMethod
                hostPlatform.AddTestHostMethod = unsafeBitCast(addTestMethod, to: UnsafeMutableRawPointer.self)

                let getTestBufferMethod: GetTestBufferPtr = getTestBuffer
                hostPlatform.GetTestBuffer = unsafeBitCast(getTestBufferMethod, to: UnsafeMutableRawPointer.self)

                startEngine(&hostPlatform)
            }
        }
        
        // TODO: Do not forget to call the shutdownCoreClr method
        
        //dlclose(handle)
    }
    
    func buildMainMenu() {
        let mainMenu = NSMenu(title: "MainMenu")
        
        let menuItem = mainMenu.addItem(withTitle: "ApplicationMenu", action: nil, keyEquivalent: "")
        let subMenu = NSMenu(title: "Application")
        mainMenu.setSubmenu(subMenu, for: menuItem)
        
        subMenu.addItem(withTitle: "About CoreEngine", action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)), keyEquivalent: "")
        subMenu.addItem(NSMenuItem.separator())
        subMenu.addItem(withTitle: "Quit", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        
        NSApp.mainMenu = mainMenu
    }
}

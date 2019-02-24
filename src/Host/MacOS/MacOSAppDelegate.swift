import Cocoa
import Metal
import MetalKit

public struct Span {
    public var Buffer: UnsafeMutablePointer<UInt8>!
    public var Length: Int32

    public init() {
        self.Length = 0
    }
}

public struct HostPlatform {
    public var testParameter: Int32
    public var appName: UnsafePointer<Int8>?
    //public var addTestHostMethod: UnsafeMutableRawPointer!
    //public var GetTestBuffer: UnsafeMutableRawPointer!

    public init() {
        self.testParameter = -1
        self.appName = nil
        //self.addTestHostMethod = nil
    }
    
    // public mutating func assignAddTestHostMethod(_ method: UnsafeMutableRawPointer!) {
    //     self.addTestHostMethod = method
    // }
}

func addTestHostMethod(_ a: Int32, _ b: Int32) -> Int32
{
	return a + b;
}

public typealias coreclr_initialize_ptr = @convention(c) (UnsafePointer<Int8>?, UnsafePointer<Int8>?, Int32, UnsafeMutablePointer<UnsafePointer<Int8>?>?, UnsafeMutablePointer<UnsafePointer<Int8>?>?, UnsafeMutablePointer<UnsafeMutableRawPointer?>?, UnsafeMutablePointer<UInt32>?) -> Int32
public typealias coreclr_create_delegate_ptr = @convention(c) (UnsafeMutableRawPointer?, UInt32, UnsafePointer<Int8>?, UnsafePointer<Int8>?, UnsafePointer<Int8>?, UnsafeMutablePointer<UnsafeMutableRawPointer?>?) -> Int32

public typealias AddTestHostMethodType = @convention(c) (Int32, Int32) -> Int32;
public typealias StartEngine_ptr = @convention(c) (UnsafeRawPointer?, UnsafeMutableRawPointer!) -> Void

class AppDelegate: NSObject, NSApplicationDelegate {
    var mainWindow: NSWindow!
    // var controller: ViewController?

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        print("CoreEngine MacOS Host")
        self.buildMainMenu()

        self.mainWindow = NSWindow(contentRect: NSMakeRect(0, 0, 1280, 720), 
                                   styleMask: [.resizable, .titled, .miniaturizable, .closable], 
                                   backing: .buffered, 
                                   defer: false)

        self.mainWindow.title = "Core Engine"

        // controller = ViewController()
        // let content = newWindow!.contentView! as NSView
        // let view = controller!.view
        // content.addSubview(view)


        self.mainWindow.center()
        self.mainWindow.makeKeyAndOrderFront(nil)
        
        let defaultDevice = MTLCreateSystemDefaultDevice()
        
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
                let startEngine = unsafeBitCast(managedDelegate!, to: StartEngine_ptr.self)

                // let s = "Test Parameter"
                var hostPlatform = HostPlatform()
                hostPlatform.testParameter = 5

                if (CommandLine.arguments.count > 1) {
                    hostPlatform.appName = UnsafePointer<Int8>?((CommandLine.arguments[1] as NSString).utf8String!)
                }

                //var addTestMethodPointer = addTestHostMethod
                //let addTestMethod = unsafeBitCast(&addTestHostMethod, to: AddTestHostMethodType.self)
                //hostPlatform.assignAddTestHostMethod(addTestHostMethod)
                //hostPlatform.GetTestBuffer = (void*)getTestBufferMethod;
                startEngine(&hostPlatform, &addTestHostMethod)
            }
        }
        
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

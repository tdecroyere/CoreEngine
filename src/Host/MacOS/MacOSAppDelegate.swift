import Cocoa
import Metal
import MetalKit

class MacOSAppDelegate: NSObject, NSApplicationDelegate {
    public var mainWindow: NSWindow!
    var mainWindowDelegate: MacOSWindowDelegate!
    var mainController: NSViewController!
    var metalDevice: MTLDevice!
    public var mtkView: MTKView!
    public var renderer: MacOSMetalRenderer!

    // TODO: Catch activate/inactivate events to interrupt game update

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        self.buildMainMenu()

        self.mainWindow = NSWindow(contentRect: NSMakeRect(0, 0, 1280, 720), 
                                   styleMask: [.resizable, .titled, .miniaturizable, .closable], 
                                   backing: .buffered, 
                                   defer: false)

        self.mainWindow.title = "Core Engine"
    
        self.mainWindowDelegate = MacOSWindowDelegate()
        self.mainWindow.delegate = self.mainWindowDelegate

        self.mainController = NSViewController()
        self.mtkView = MTKView()
        self.mtkView.translatesAutoresizingMaskIntoConstraints = false

        self.mainController.view = self.mtkView
        
        let content = self.mainWindow.contentView! as NSView
        let view = self.mainController.view
        content.addSubview(view)
        content.addConstraints(NSLayoutConstraint.constraints(withVisualFormat: "|[mtkView]|", options: [], metrics: nil, views: ["mtkView" : mtkView!]))
        content.addConstraints(NSLayoutConstraint.constraints(withVisualFormat: "V:|[mtkView]|", options: [], metrics: nil, views: ["mtkView" : mtkView!]))

        self.mainWindow.center()
        self.mainWindow.makeKeyAndOrderFront(nil)

        // Select the device to render with.  We choose the default device
        guard let defaultDevice = MTLCreateSystemDefaultDevice() else {
            print("Metal is not supported on this device")
            return
        }

        print(defaultDevice.name)

        self.renderer = MacOSMetalRenderer(view: self.mtkView, device: defaultDevice)
        self.mtkView.delegate = renderer
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

class MacOSWindowDelegate: NSObject, NSWindowDelegate {
    func windowDidEnterFullScreen(_ notification: Notification) {
        // TODO: Re-hide cursor when it is moved after 2 seconds
        print("Swift fullscreen")
        NSCursor.setHiddenUntilMouseMoves(true)
    }
}
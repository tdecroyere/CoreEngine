import Cocoa
import Metal
import MetalKit

class MacOSAppDelegate: NSObject, NSApplicationDelegate {
    public var mainWindow: NSWindow!
    var mainWindowDelegate: MacOSWindowDelegate!
    var mainController: NSViewController!
    var metalDevice: MTLDevice!
    public var renderer: MacOSMetalRenderer!

    // TODO: Catch activate/inactivate events to interrupt game update

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        self.buildMainMenu()

        self.mainWindow = NSWindow(contentRect: NSMakeRect(0, 0, 1280, 720), 
                                   styleMask: [.resizable, .titled, .miniaturizable, .closable], 
                                   backing: .buffered, 
                                   defer: false)

        self.mainWindow.title = "Core Engine"

        self.mainWindow.center()
        self.mainWindow.makeKeyAndOrderFront(nil)

        let view = self.mainWindow.contentView! as NSView
        let scale = self.mainWindow.screen!.backingScaleFactor

        var size = view.frame.size
        size.width *= scale;
        size.height *= scale;

        self.renderer = MacOSMetalRenderer(view: view, renderWidth: Int(size.width), renderHeight: Int(size.height))

        self.mainWindowDelegate = MacOSWindowDelegate(window: self.mainWindow, renderer: self.renderer)
        self.mainWindow.delegate = self.mainWindowDelegate
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
    let window: NSWindow
    let renderer: MacOSMetalRenderer

    init(window: NSWindow, renderer: MacOSMetalRenderer) {
        self.window = window
        self.renderer = renderer
    }

    func windowDidEnterFullScreen(_ notification: Notification) {
        // TODO: Re-hide cursor when it is moved after 2 seconds
        print("Swift fullscreen")
        NSCursor.setHiddenUntilMouseMoves(true)
    }

    func windowDidResize(_ notification: Notification) {
        let view = self.window.contentView! as NSView
        let scale = self.window.screen!.backingScaleFactor
        
        var size = view.frame.size
        size.width *= scale;
        size.height *= scale;

        self.renderer.changeRenderSize(renderWidth: Int(size.width), renderHeight: Int(size.height))
    }
}

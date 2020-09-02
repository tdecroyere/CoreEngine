import Cocoa

class MacOSAppDelegate: NSObject, NSApplicationDelegate {
    public var mainWindow: NSWindow!
    var metalView: MacOSMetalView!
    // var mainWindowDelegate: MacOSWindowDelegate!
    // public var renderer: MetalGraphicsService!

    // TODO: Catch activate/inactivate events to interrupt game update

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        self.buildMainMenu()

        // self.mainWindow = NSWindow(contentRect: NSMakeRect(0, 0, 1280, 720), 
        //                            styleMask: [.resizable, .titled, .miniaturizable, .closable], 
        //                            backing: .buffered, 
        //                            defer: false)

        // self.mainWindow.title = "Core Engine"

        // let contentView = self.mainWindow.contentView! as NSView
        // self.metalView = MacOSMetalView()
        // self.metalView.frame = contentView.frame
        // contentView.addSubview(self.metalView)

        // self.mainWindow.center()
        // self.mainWindow.makeKeyAndOrderFront(nil)

        // let scale = self.mainWindow.screen!.backingScaleFactor
        // var size = contentView.frame.size
        // size.width *= scale;
        // size.height *= scale;

        // self.mainWindowDelegate = MacOSWindowDelegate(window: self.mainWindow, metalView: self.metalView, renderer: self.renderer)
        // self.mainWindow.delegate = self.mainWindowDelegate
    }
    
    func buildMainMenu() {
        let mainMenu = NSMenu(title: "MainMenu")
        
        let menuItem = mainMenu.addItem(withTitle: "ApplicationMenu", action: nil, keyEquivalent: "")
        let subMenu = NSMenu(title: "Application")
        mainMenu.setSubmenu(subMenu, for: menuItem)
        
        subMenu.addItem(withTitle: "About CoreEngine", action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)), keyEquivalent: "")
        subMenu.addItem(NSMenuItem.separator())

        let servicesMenuSub = subMenu.addItem(withTitle: "Services", action: nil, keyEquivalent: "")
		let servicesMenu = NSMenu(title:"Services")
        mainMenu.setSubmenu(servicesMenu, for: servicesMenuSub)
		NSApp.servicesMenu = servicesMenu
        subMenu.addItem(NSMenuItem.separator())
        
        var menuItemAdded = subMenu.addItem(withTitle: "Hide CoreEngine", action:#selector(NSApplication.hide(_:)), keyEquivalent:"h")
        menuItemAdded.target = NSApp

        menuItemAdded = subMenu.addItem(withTitle: "Hide Others", action:#selector(NSApplication.hideOtherApplications(_:)), keyEquivalent:"h")
        menuItemAdded.keyEquivalentModifierMask = [.command, .option]
        menuItemAdded.target = NSApp

        menuItemAdded = subMenu.addItem(withTitle: "Show All", action:#selector(NSApplication.unhideAllApplications(_:)), keyEquivalent:"")
        menuItemAdded.target = NSApp

        subMenu.addItem(NSMenuItem.separator())
        subMenu.addItem(withTitle: "Quit", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")

        let windowMenuItem = mainMenu.addItem(withTitle: "Window", action: nil, keyEquivalent: "")
        let windowSubMenu = NSMenu(title: "Window")
        mainMenu.setSubmenu(windowSubMenu, for: windowMenuItem)

        windowSubMenu.addItem(withTitle: "Minimize", action: #selector(NSWindow.performMiniaturize(_:)), keyEquivalent: "m")
        windowSubMenu.addItem(withTitle: "Zoom", action: #selector(NSWindow.performZoom), keyEquivalent: "")
        
        NSApp.mainMenu = mainMenu
        NSApp.windowsMenu = windowSubMenu
    }
}
/*
class MacOSWindowDelegate: NSObject, NSWindowDelegate {
    let window: NSWindow
    let metalView: MacOSMetalView
    let renderer: MetalGraphicsService

    init(window: NSWindow, metalView: MacOSMetalView, renderer: MetalGraphicsService) {
        self.window = window
        self.metalView = metalView
        self.renderer = renderer
    }

    func windowDidEnterFullScreen(_ notification: Notification) {
        // TODO: Re-hide cursor when it is moved after 2 seconds
        print("Swift fullscreen")
        NSCursor.setHiddenUntilMouseMoves(true)
    }

    func windowDidResize(_ notification: Notification) {
        guard let currentScreen = self.window.screen else {
            return
        }

        guard let contentView = self.window.contentView else {
            return
        }

        self.metalView.frame = contentView.frame
        let scale = currentScreen.backingScaleFactor

        var size = self.metalView.frame.size
        size.width *= scale;
        size.height *= scale;

        //self.renderer.changeRenderSize(renderWidth: Int(size.width), renderHeight: Int(size.height))
    }
}
*/
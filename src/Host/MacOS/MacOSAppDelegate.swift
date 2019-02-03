import Cocoa
import Metal
import MetalKit

class AppDelegate: NSObject, NSApplicationDelegate {
    var mainWindow: NSWindow?
    // var controller: ViewController?

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        print("Hello World")

        mainWindow = NSWindow(contentRect: NSMakeRect(0, 0, 1280, 720), 
                             styleMask: [.resizable, .titled, .miniaturizable, .closable], 
                             backing: .buffered, 
                             defer: false)

        mainWindow!.title = "Test"

        // controller = ViewController()
        // let content = newWindow!.contentView! as NSView
        // let view = controller!.view
        // content.addSubview(view)

        mainWindow!.makeKeyAndOrderFront(nil)
    }
}

import Cocoa

class AppDelegate: NSObject, NSApplicationDelegate {
    var newWindow: NSWindow?
    var controller: ViewController?

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        newWindow = NSWindow(contentRect: NSMakeRect(10, 10, 300, 300), styleMask: .resizable, backing: .buffered, defer: false)

        controller = ViewController()
        let content = newWindow!.contentView! as NSView
        let view = controller!.view
        content.addSubview(view)

        newWindow!.makeKeyAndOrderFront(nil)
    }
}
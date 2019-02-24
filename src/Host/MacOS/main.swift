import Cocoa

let delegate = AppDelegate()
NSApplication.shared.delegate = delegate
NSApplication.shared.activate(ignoringOtherApps: true)

_ = NSApplicationMain(CommandLine.argc, CommandLine.unsafeArgv)

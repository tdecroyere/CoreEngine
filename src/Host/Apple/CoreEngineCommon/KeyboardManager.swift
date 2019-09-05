import IOKit
import IOKit.hid

class Keyboard {

}

func keyboardConnected(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, device: IOHIDDevice) {
    if (result != kIOReturnSuccess) {
        return
    }

    print("MacOS: Keyboard controller connected")

    let keyboardManager = Unmanaged<KeyboardManager>.fromOpaque(context!).takeUnretainedValue()
    let manufacturerName = IOHIDDeviceGetProperty(device, kIOHIDManufacturerKey as CFString)
    let productName  = IOHIDDeviceGetProperty(device, kIOHIDProductKey as CFString)

    print(manufacturerName)
    print(productName)
	//let controller = MacOSGamepad(gamepadManager: gamepadManager, device: device)
    //gamepadManager.registeredGamepads.append(controller)
}

class KeyboardManager {
    let hidManager: IOHIDManager
    let registeredKeyboards: [Keyboard]

    init() {
        self.registeredKeyboards = []
        self.hidManager = IOHIDManagerCreate(kCFAllocatorDefault, IOOptionBits(kIOHIDOptionsTypeNone))

        if (IOHIDManagerOpen(hidManager, IOOptionBits(kIOHIDOptionsTypeNone)) != kIOReturnSuccess) {
		    print("Error initializing ExtendedGCController");
            return;
        }

        let deviceCriteria = [
            [
                kIOHIDDeviceUsagePageKey: kHIDPage_GenericDesktop,
                kIOHIDDeviceUsageKey: kHIDUsage_GD_Keyboard
            ]
        ] as CFArray
        
	    IOHIDManagerRegisterDeviceMatchingCallback(hidManager, keyboardConnected, Unmanaged.passUnretained(self).toOpaque());
        IOHIDManagerSetDeviceMatchingMultiple(hidManager, deviceCriteria)

	    IOHIDManagerScheduleWithRunLoop(hidManager, CFRunLoopGetMain(), CFRunLoopMode.defaultMode.rawValue);
    }
}
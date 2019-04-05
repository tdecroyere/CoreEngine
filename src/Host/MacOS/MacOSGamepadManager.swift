import Foundation
import IOKit
import IOKit.hid

enum GameControllerVendor: Int {
    case microsoft = 0x045E
    case sony = 0x054C
}

enum GameControllerProduct: Int {
    case xboxOneWireless = 0x02FD
    case xbox360Wireless = 0x028F
    case xbox360Wired = 0x028E
    case dualShock4 = 0x5C4
}

protocol MacOSGamepadLayout {
    var button1UsageId: UInt32 { get }
	var button2UsageId: UInt32 { get }
	var button3UsageId: UInt32 { get }
	var button4UsageId: UInt32 { get }
    var buttonStartUsageID: UInt32 { get }
    var buttonBackUsageID: UInt32 { get }
	var leftShoulderUsageID: UInt32 { get }
	var rightShoulderUsageID: UInt32 { get }

    var leftTriggerUsageID: UInt32 { get }
    var leftTriggerMaxValue: Float { get }
	var rightTriggerUsageID: UInt32 { get }
    var rightTriggerMaxValue: Float { get }

    var leftThumbXUsageID: UInt32 { get }
    var leftThumbXMaxValue: Float { get }
	var leftThumbYUsageID: UInt32 { get }
    var leftThumbYMaxValue: Float { get }
    var rightThumbXUsageID: UInt32 { get }
    var rightThumbXMaxValue: Float { get }
	var rightThumbYUsageID: UInt32 { get }
    var rightThumbYMaxValue: Float { get }
	
	var dpadUsageID: UInt32 { get }
}

class MacOSXboxOneWirelessGamepadLayout: MacOSGamepadLayout {
    var button1UsageId: UInt32 { get { return 1 } }
    var button2UsageId: UInt32 { get { return 2 } }
    var button3UsageId: UInt32 { get { return 4 } }
    var button4UsageId: UInt32 { get { return 5 } }
    var buttonStartUsageID: UInt32 { get { return 12 } }
    var buttonBackUsageID: UInt32 { get { return 548 } }
    var leftShoulderUsageID: UInt32 { get { return 7 } }
    var rightShoulderUsageID: UInt32 { get { return 8 } }
    
    var leftTriggerUsageID: UInt32 { get { return 197 } }
    var leftTriggerMaxValue: Float { get { return 1023.0 } }
    var rightTriggerUsageID: UInt32 { get { return 196 } }
    var rightTriggerMaxValue: Float { get { return 1023.0 } }
    
    var leftThumbXUsageID: UInt32 { get { return 48 } }
    var leftThumbXMaxValue: Float { get { return 32767.0 } }
    var leftThumbYUsageID: UInt32 { get { return 49 } }
    var leftThumbYMaxValue: Float { get { return 32767.0 } }
    var rightThumbXUsageID: UInt32 { get { return 50 } }
    var rightThumbXMaxValue: Float { get { return 32767.0 } }
    var rightThumbYUsageID: UInt32 { get { return 53 } }
    var rightThumbYMaxValue: Float { get { return 32767.0 } }
    
    var dpadUsageID: UInt32 { get { return 57 } }
}

func controllerConnected(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, device: IOHIDDevice) {
    if (result != kIOReturnSuccess) {
        return
    }

    print("MacOS: Gamepad controller connected")

    let gamepadManager = Unmanaged<MacOSGamepadManager>.fromOpaque(context!).takeUnretainedValue()
	let controller = MacOSGamepad(gamepadManager: gamepadManager, device: device)
    gamepadManager.registeredGamepads.append(controller)
}

func controllerDisconnected(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?) {
    if (result != kIOReturnSuccess) {
        return
    }

    print("MacOS: Gamepad controller disconnected")

    let device = Unmanaged<MacOSGamepad>.fromOpaque(context!).takeUnretainedValue()
    let gamepadManager = device.gamepadManager

    if let index = gamepadManager.registeredGamepads.firstIndex(where: { $0 === device }) {
        gamepadManager.registeredGamepads.remove(at: index)
    }
}

func controllerInput(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, value: IOHIDValue) {
    let device = Unmanaged<MacOSGamepad>.fromOpaque(context!).takeUnretainedValue()

    if (result != kIOReturnSuccess) {
        return
    }

    autoreleasepool {
        let element = IOHIDValueGetElement(value)

 	    let usage = IOHIDElementGetUsage(element)
        let rawInputValue = Float(Int(IOHIDValueGetIntegerValue(value)));

        // TODO: This code is usefull to do reverse-engineering for gamepad bindings
        //print("============")
        //print("Usage: \(usage)")
        //print("RawInputValue: \(rawInputValue)")
        
        switch (usage) {
            case device.gamepadLayout.button1UsageId:
                device.button1 = rawInputValue
            case device.gamepadLayout.button2UsageId:
                device.button2 = rawInputValue
            case device.gamepadLayout.button3UsageId:
                device.button3 = rawInputValue
            case device.gamepadLayout.button4UsageId:
                device.button4 = rawInputValue
            case device.gamepadLayout.leftShoulderUsageID:
                device.leftShoulder = rawInputValue
            case device.gamepadLayout.rightShoulderUsageID:
                device.rightShoulder = rawInputValue
            case device.gamepadLayout.buttonBackUsageID:
                device.buttonBack = rawInputValue
            case device.gamepadLayout.buttonStartUsageID:
                device.buttonStart = rawInputValue
            case device.gamepadLayout.leftTriggerUsageID:
                device.leftTrigger = rawInputValue / device.gamepadLayout.leftTriggerMaxValue
            case device.gamepadLayout.rightTriggerUsageID:
                device.rightTrigger = rawInputValue / device.gamepadLayout.rightTriggerMaxValue
            case device.gamepadLayout.leftThumbXUsageID:
                device.leftThumbX = (rawInputValue - device.gamepadLayout.leftThumbXMaxValue) / device.gamepadLayout.leftThumbXMaxValue
            case device.gamepadLayout.leftThumbYUsageID:
                device.leftThumbY = (rawInputValue - device.gamepadLayout.leftThumbYMaxValue) / device.gamepadLayout.leftThumbYMaxValue
            case device.gamepadLayout.rightThumbXUsageID:
                device.rightThumbX = (rawInputValue - device.gamepadLayout.rightThumbXMaxValue) / device.gamepadLayout.rightThumbXMaxValue
            case device.gamepadLayout.rightThumbYUsageID:
                device.rightThumbY = (rawInputValue - device.gamepadLayout.rightThumbYMaxValue) / device.gamepadLayout.rightThumbYMaxValue
            case device.gamepadLayout.dpadUsageID:
                // TODO: Do something with the fact that Xbox One dpad return full circle angles for more precisision?
                device.dpadUp = 0.0
                device.dpadRight = 0.0
                device.dpadDown = 0.0
                device.dpadLeft = 0.0

                if (rawInputValue >= 0.0 && rawInputValue < 45.0) {
                    device.dpadUp = 1.0
                } else if (rawInputValue >= 45.0 && rawInputValue < 90.0) {
                    device.dpadUp = 1.0
                    device.dpadRight = 1.0
                } else if (rawInputValue >= 90.0 && rawInputValue < 135.0) {
                    device.dpadRight = 1.0
                } else if (rawInputValue >= 135.0 && rawInputValue < 180.0) {
                    device.dpadRight = 1.0
                    device.dpadDown = 1.0
                } else if (rawInputValue >= 180.0 && rawInputValue < 235.0) {
                    device.dpadDown = 1.0
                } else if (rawInputValue >= 235.0 && rawInputValue < 270.0) {
                    device.dpadDown = 1.0
                    device.dpadLeft = 1.0
                } else if (rawInputValue >= 270.0 && rawInputValue < 315.0) {
                    device.dpadLeft = 1.0
                } else if (rawInputValue >= 315.0 && rawInputValue < 365.0) {
                    device.dpadLeft = 1.0
                    device.dpadUp = 1.0
                }
            default:
                print("Warning: Unknown input element")
        }
	}
}

class MacOSGamepad {
    var gamepadManager: MacOSGamepadManager
    var gamepadLayout: MacOSGamepadLayout!
    var manufacturerName: String
    var productName: String

    var button1: Float = 0.0
    var button2: Float = 0.0
    var button3: Float = 0.0
    var button4: Float = 0.0
    var leftShoulder: Float = 0.0
    var rightShoulder: Float = 0.0
    var buttonStart: Float = 0.0
    var buttonBack: Float = 0.0
    var leftTrigger: Float = 0.0
    var rightTrigger: Float = 0.0
    var leftThumbX: Float = 0.0
    var leftThumbY: Float = 0.0
    var rightThumbX: Float = 0.0
    var rightThumbY: Float = 0.0
    var dpadUp: Float = 0.0
    var dpadRight: Float = 0.0
    var dpadDown: Float = 0.0
    var dpadLeft: Float = 0.0

    init(gamepadManager: MacOSGamepadManager, device: IOHIDDevice) {
        self.gamepadManager = gamepadManager
        self.manufacturerName = IOHIDDeviceGetProperty(device, kIOHIDManufacturerKey as CFString) as! String
        self.productName  = IOHIDDeviceGetProperty(device, kIOHIDProductKey as CFString) as! String
 
        let vendorId = GameControllerVendor(rawValue: Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDVendorIDKey as CFString) as! NSNumber))!
        let productId = GameControllerProduct(rawValue: Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDProductIDKey as CFString) as! NSNumber))!

        // TODO: PS4 Controller and other xbox controllers
        if (vendorId == .microsoft) {
            if (productId == .xboxOneWireless) {
                self.gamepadLayout = MacOSXboxOneWirelessGamepadLayout()
            }
        }

        IOHIDDeviceRegisterInputValueCallback(device, controllerInput, Unmanaged.passUnretained(self).toOpaque())	
		IOHIDDeviceRegisterRemovalCallback(device, controllerDisconnected, Unmanaged.passUnretained(self).toOpaque());
    }
}

class MacOSGamepadManager {
    let hidManager: IOHIDManager
    var registeredGamepads: [MacOSGamepad]

    init() {
        self.registeredGamepads = []
        self.hidManager = IOHIDManagerCreate(kCFAllocatorDefault, IOOptionBits(kIOHIDOptionsTypeNone))

        if (IOHIDManagerOpen(hidManager, IOOptionBits(kIOHIDOptionsTypeNone)) != kIOReturnSuccess) {
		    print("Error initializing ExtendedGCController");
            return;
        }

        let deviceCriteria = [
            [
                kIOHIDDeviceUsagePageKey: kHIDPage_GenericDesktop,
                kIOHIDDeviceUsageKey: kHIDUsage_GD_GamePad
            ],
            [
                kIOHIDDeviceUsagePageKey: kHIDPage_GenericDesktop,
                kIOHIDDeviceUsageKey: kHIDUsage_GD_MultiAxisController
            ]
        ] as CFArray
        
	    IOHIDManagerRegisterDeviceMatchingCallback(hidManager, controllerConnected, Unmanaged.passUnretained(self).toOpaque());
        IOHIDManagerSetDeviceMatchingMultiple(hidManager, deviceCriteria)

	    IOHIDManagerScheduleWithRunLoop(hidManager, CFRunLoopGetMain(), CFRunLoopMode.defaultMode.rawValue);
    }
}

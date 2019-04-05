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
            case device.button1UsageId:
                device.button1 = rawInputValue
            case device.button2UsageId:
                device.button2 = rawInputValue
            case device.button3UsageId:
                device.button3 = rawInputValue
            case device.button4UsageId:
                device.button4 = rawInputValue
            case device.leftShoulderUsageID:
                device.leftShoulder = rawInputValue
            case device.rightShoulderUsageID:
                device.rightShoulder = rawInputValue
            case device.buttonBackUsageID:
                device.buttonBack = rawInputValue
            case device.buttonStartUsageID:
                device.buttonStart = rawInputValue
            case device.leftTriggerUsageID:
                device.leftTrigger = rawInputValue / device.leftTriggerMaxValue
            case device.rightTriggerUsageID:
                device.rightTrigger = rawInputValue / device.rightTriggerMaxValue
            case device.leftThumbXUsageID:
                device.leftThumbX = (rawInputValue - device.leftThumbXMaxValue) / device.leftThumbXMaxValue
            case device.leftThumbYUsageID:
                device.leftThumbY = (rawInputValue - device.leftThumbYMaxValue) / device.leftThumbYMaxValue
            case device.rightThumbXUsageID:
                device.rightThumbX = (rawInputValue - device.rightThumbXMaxValue) / device.rightThumbXMaxValue
            case device.rightThumbYUsageID:
                device.rightThumbY = (rawInputValue - device.rightThumbYMaxValue) / device.rightThumbYMaxValue
            case device.dpadUsageID:
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
    var button1UsageId: UInt32 = 0
	var button2UsageId: UInt32 = 0
	var button3UsageId: UInt32 = 0
	var button4UsageId: UInt32 = 0
	var leftShoulderUsageID: UInt32 = 0
	var rightShoulderUsageID: UInt32 = 0
    var buttonStartUsageID: UInt32 = 0
    var buttonBackUsageID: UInt32 = 0

    var leftTriggerUsageID: UInt32 = 0
    var leftTriggerMaxValue: Float = 1023.0
	var rightTriggerUsageID: UInt32 = 0
    var rightTriggerMaxValue: Float = 1023.0

    var leftThumbXUsageID: UInt32 = 0
    var leftThumbXMaxValue: Float = 32767.0
	var leftThumbYUsageID: UInt32 = 0
    var leftThumbYMaxValue: Float = 32767.0
    var rightThumbXUsageID: UInt32 = 0
    var rightThumbXMaxValue: Float = 32767.0
	var rightThumbYUsageID: UInt32 = 0
    var rightThumbYMaxValue: Float = 32767.0
	
	var dpadUsageID: UInt32 = 0
	
    var gamepadManager: MacOSGamepadManager
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
                self.button1UsageId = 1
				self.button2UsageId = 2
				self.button3UsageId = 4
				self.button4UsageId = 5

                self.leftShoulderUsageID = 7
				self.rightShoulderUsageID = 8

                self.leftTriggerUsageID = 197
				self.rightTriggerUsageID = 196

                self.buttonStartUsageID = 12
                self.buttonBackUsageID = 548

                self.leftThumbXUsageID = 48
				self.leftThumbYUsageID = 49

				self.rightThumbXUsageID = 50
				self.rightThumbYUsageID = 53
				
				self.dpadUsageID = 57
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

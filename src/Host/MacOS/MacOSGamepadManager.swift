// TODO: Huge bug: controllerConnected is not called anymore. The workaround is to unregister the xbox controller
// from MacOS and register it again

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
    let gamepadManager = Unmanaged<MacOSGamepadManager>.fromOpaque(context!).takeUnretainedValue()
    print("controller connected")

    if(result == kIOReturnSuccess) {
		let controller = MacOSGamepad(device: device)
        gamepadManager.registeredGamepads.append(controller)
    }
}

func controllerInput(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, value: IOHIDValue) {
    let device = Unmanaged<MacOSGamepad>.fromOpaque(context!).takeUnretainedValue()

    if (result != kIOReturnSuccess) {
        return
    }

    //autoreleasepool {
        let element = IOHIDValueGetElement(value)

        let usagePage = IOHIDElementGetUsagePage(element)
 	    let usage = IOHIDElementGetUsage(element)
        //print("============")
        //print("UsagePage: \(usagePage) - Usage: \(usage)")

        //let rawInputValue = Float(Int(IOHIDValueGetIntegerValue(value)));
        let rawInputValue = Float(IOHIDValueGetScaledValue(value, IOHIDValueScaleType(kIOHIDValueScaleTypeCalibrated)))

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
            default:
                print("Warning: Unknown input element")
        }
	//}
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
	var _rThumbXUsageID: UInt32 = 0
	var _rThumbYUsageID: UInt32 = 0
	
	
	var _dpadLUsageID: UInt32 = 0
	var _dpadRUsageID: UInt32 = 0
	var _dpadDUsageID: UInt32 = 0
    var _dpadUUsageID: UInt32 = 0
	
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

    init(device: IOHIDDevice) {
        print("init device")

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

				self._rThumbXUsageID = 53
				self._rThumbYUsageID = 50
				
				self._dpadLUsageID = 0x0E
				self._dpadRUsageID = 0x0F
				self._dpadDUsageID = 0x0D
				self._dpadUUsageID = 0x0C
            }
        }

        IOHIDDeviceRegisterInputValueCallback(device, controllerInput, Unmanaged.passUnretained(self).toOpaque())	
		//IOHIDDeviceRegisterRemovalCallback(device, ControllerDisconnected, (void *)CFBridgingRetain(controller));
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

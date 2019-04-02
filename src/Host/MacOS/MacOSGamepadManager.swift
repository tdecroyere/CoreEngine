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
    print("controller value")
    let device = Unmanaged<MacOSGamepad>.fromOpaque(context!).takeUnretainedValue()

    if (result != kIOReturnSuccess) {
        return
    }

    //autoreleasepool {
        let element = IOHIDValueGetElement(value)

        let usagePage = IOHIDElementGetUsagePage(element)
 	    let usage = IOHIDElementGetUsage(element)
        print("============")
        print("UsagePage: \(usagePage) - Usage: \(usage)")

        let state = IOHIDValueGetIntegerValue(value);
        let analog = IOHIDValueGetScaledValue(value, IOHIDValueScaleType(kIOHIDValueScaleTypeCalibrated));

        print("State: \(state) - Analog: \(analog)")
        
        if (usage == 0x01) {
            device.button1 = Float(state)
            // device._snapshot.buttonA = Float(state)
            // print(device._snapshot.buttonA)
            //device._extendedGamepad!.snapshotData = NSDataFromGCExtendedGamepadSnapshotData(&device._snapshot)!
            //device.extendedGamepad!.buttonA.value = Float(state)
        }
	//}
}


class MacOSGamepad {
    var _lThumbXUsageID: CFIndex = 0
	var _lThumbYUsageID: CFIndex = 0
	var _rThumbXUsageID: CFIndex = 0
	var _rThumbYUsageID: CFIndex = 0
	var _lTriggerUsageID: CFIndex = 0
	var _rTriggerUsageID: CFIndex = 0
	
    var _usesHatSwitch: Bool = false
	var _dpadLUsageID: CFIndex = 0
	var _dpadRUsageID: CFIndex = 0
	var _dpadDUsageID: CFIndex = 0
    var _dpadUUsageID: CFIndex = 0
	
	var _buttonPauseUsageID: CFIndex = 0
	var _buttonAUsageID: CFIndex = 0
	var _buttonBUsageID: CFIndex = 0
	var _buttonXUsageID: CFIndex = 0
	var _buttonYUsageID: CFIndex = 0
	var _lShoulderUsageID: CFIndex = 0
	var _rShoulderUsageID: CFIndex = 0

    var deadZonePercent: Float = 0.25

    var manufacturerName: String
    var productName: String

    var button1: Float = 0.0

    init(device: IOHIDDevice) {
        print("init device")

        self.manufacturerName = IOHIDDeviceGetProperty(device, kIOHIDManufacturerKey as CFString) as! String
        self.productName  = IOHIDDeviceGetProperty(device, kIOHIDProductKey as CFString) as! String
 
        let vendorId = GameControllerVendor(rawValue: Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDVendorIDKey as CFString) as! NSNumber))!
        let productId = GameControllerProduct(rawValue: Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDProductIDKey as CFString) as! NSNumber))!

        // TODO: PS4 Controller and other xbox controllers
        if (vendorId == .microsoft) {
            if (productId == .xboxOneWireless) {
                self._lThumbXUsageID = 49
				self._lThumbYUsageID = 48
				self._rThumbXUsageID = 53
				self._rThumbYUsageID = 50
				self._lTriggerUsageID = 197
				self._rTriggerUsageID = 196
				
				self._dpadLUsageID = 0x0E
				self._dpadRUsageID = 0x0F
				self._dpadDUsageID = 0x0D
				self._dpadUUsageID = 0x0C
				
				self._buttonPauseUsageID = 0x09
				self._buttonAUsageID = 0x01
				self._buttonBUsageID = 0x02
				self._buttonXUsageID = 0x03
				self._buttonYUsageID = 0x04
				self._lShoulderUsageID = 0x05
				self._rShoulderUsageID = 0x06
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

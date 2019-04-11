import Foundation
import IOKit
import IOKit.hid

// TODO: Use HID reports to avoid internal thread loop?

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

    var leftStickXUsageID: UInt32 { get }
    var leftStickXMaxValue: Float { get }
	var leftStickYUsageID: UInt32 { get }
    var leftStickYMaxValue: Float { get }
    var rightStickXUsageID: UInt32 { get }
    var rightStickXMaxValue: Float { get }
	var rightStickYUsageID: UInt32 { get }
    var rightStickYMaxValue: Float { get }
	
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
    
    var leftStickXUsageID: UInt32 { get { return 48 } }
    var leftStickXMaxValue: Float { get { return 32767.0 } }
    var leftStickYUsageID: UInt32 { get { return 49 } }
    var leftStickYMaxValue: Float { get { return 32767.0 } }
    var rightStickXUsageID: UInt32 { get { return 50 } }
    var rightStickXMaxValue: Float { get { return 32767.0 } }
    var rightStickYUsageID: UInt32 { get { return 53 } }
    var rightStickYMaxValue: Float { get { return 32767.0 } }
    
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

func getHIDElement(_ device: IOHIDDevice, _ elementId: CFIndex) -> IOHIDElement {
    let elementCriteria = [
        kIOHIDElementUsageKey: elementId
    ] as CFDictionary
	
    let nsArray = IOHIDDeviceCopyMatchingElements(device, elementCriteria, 0)!
    let elements: Array<IOHIDElement> = nsArray as! Array<IOHIDElement>

    if (elements.count > 0) {
        print("=====================================")
        print("Found \(elements.count) elements")

        
        for element in elements {
            let usagePage = IOHIDElementGetUsagePage(element)
            let usage = IOHIDElementGetUsage(element)

            print("Element (UsagePage: \(usagePage), Usage: \(usage))")

            let hidValue = IOHIDValueCreateWithIntegerValue(kCFAllocatorDefault, element, mach_absolute_time(), 255)
            IOHIDDeviceSetValue(device, element, hidValue)
        }

        print("=====================================")
    }

	return elements[0];
}

protocol MacOSHIDReport {
    var hidReportId: Int8 { get }
}

struct MacOSXboxOneWirelessVibrationReport: MacOSHIDReport {
    var reportId: UInt8
	var enable: UInt8
	var leftTriggerMotor: UInt8
	var rightTriggerMotor: UInt8
	var leftStickMotor: UInt8
	var rightStickMotor: UInt8
	var duration10ms: UInt8
	var startDelay10ms: UInt8
	var loopCount: UInt8

    init() {
        self.reportId = 3
        self.enable = 0
        self.leftTriggerMotor = 0
        self.rightTriggerMotor = 0
        self.leftStickMotor = 0
        self.rightStickMotor = 0
        self.duration10ms = 0
        self.startDelay10ms = 0
        self.loopCount = 0
    }

	var hidReportId: Int8 { get { return Int8(self.reportId) } }
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
                device.sendVibrationCommand(1.0, 0.0, 0.0, 0.0, 5)
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
            case device.gamepadLayout.leftStickXUsageID:
                device.leftStickX = (rawInputValue - device.gamepadLayout.leftStickXMaxValue) / device.gamepadLayout.leftStickXMaxValue
            case device.gamepadLayout.leftStickYUsageID:
                device.leftStickY = (rawInputValue - device.gamepadLayout.leftStickYMaxValue) / device.gamepadLayout.leftStickYMaxValue
            case device.gamepadLayout.rightStickXUsageID:
                device.rightStickX = (rawInputValue - device.gamepadLayout.rightStickXMaxValue) / device.gamepadLayout.rightStickXMaxValue
            case device.gamepadLayout.rightStickYUsageID:
                device.rightStickY = (rawInputValue - device.gamepadLayout.rightStickYMaxValue) / device.gamepadLayout.rightStickYMaxValue
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
    var device: IOHIDDevice

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
    var leftStickX: Float = 0.0
    var leftStickY: Float = 0.0
    var rightStickX: Float = 0.0
    var rightStickY: Float = 0.0
    var dpadUp: Float = 0.0
    var dpadRight: Float = 0.0
    var dpadDown: Float = 0.0
    var dpadLeft: Float = 0.0

    init(gamepadManager: MacOSGamepadManager, device: IOHIDDevice) {
        self.gamepadManager = gamepadManager
        self.manufacturerName = IOHIDDeviceGetProperty(device, kIOHIDManufacturerKey as CFString) as! String
        self.productName  = IOHIDDeviceGetProperty(device, kIOHIDProductKey as CFString) as! String
        self.device = device
 
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

    func sendVibrationCommand(_ leftTriggerMotor: Float, _ rightTriggerMotor: Float, _ leftStickMotor: Float, _ rightStickMotor: Float, _ duration10ms: UInt8) {
        print("Send vibration effect (LeftTrigger: \(leftTriggerMotor), RightTrigger: \(rightTriggerMotor), LeftStick: \(leftStickMotor), RightStick: \(rightStickMotor))")
        
        var vibrationReport = MacOSXboxOneWirelessVibrationReport()
        vibrationReport.enable = 255
        vibrationReport.leftTriggerMotor = UInt8(leftTriggerMotor * 255.0)
        vibrationReport.rightTriggerMotor = UInt8(rightTriggerMotor * 255.0)
        vibrationReport.leftStickMotor = UInt8(leftStickMotor * 255.0)
        vibrationReport.rightStickMotor = UInt8(rightStickMotor * 255.0)
        vibrationReport.loopCount = 0
        vibrationReport.duration10ms = duration10ms

        let reportSize = MemoryLayout.size(ofValue: vibrationReport)
        
        withUnsafeBytes(of: vibrationReport) {rbp in
            let bufferPtr = rbp.baseAddress!.assumingMemoryBound(to: UInt8.self)
            IOHIDDeviceSetReport(self.device, kIOHIDReportTypeOutput, Int(vibrationReport.reportId), bufferPtr, reportSize)
        }
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

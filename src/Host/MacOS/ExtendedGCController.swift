/*

    This code is base on: https://github.com/slembcke/CCController

*/


// TODO: Huge bug: controllerConnected is not called anymore. The workaround is to unregister the xbox controller
// from MacOS and register it again


import GameController

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

var hidManagerGlobal: IOHIDManager? = nil

func controllerConnected(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, device: IOHIDDevice) {
    print("controller connected")

    if(result == kIOReturnSuccess) {
		let controller = ExtendedGCController(device: device)
        ExtendedGCController.controllersArray.append(controller)
    }
}

func controllerInput(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, value: IOHIDValue) {
    print("controller value")
    let device = Unmanaged<ExtendedGCController>.fromOpaque(context!).takeUnretainedValue()

    if (result != kIOReturnSuccess) {
        return
    }

    autoreleasepool {
        let element = IOHIDValueGetElement(value)

        let usagePage = IOHIDElementGetUsagePage(element)
 	    let usage = IOHIDElementGetUsage(element)
        print("============")
        print("UsagePage: \(usagePage) - Usage: \(usage)")

        let state = IOHIDValueGetIntegerValue(value);
        let analog = IOHIDValueGetScaledValue(value, IOHIDValueScaleType(kIOHIDValueScaleTypeCalibrated));

         print("State: \(state) - Analog: \(analog)")
    }

// 	@autoreleasepool {
// 		CCController *controller = (__bridge CCController *)context;
// 		GCExtendedGamepadSnapShotDataV100 *snapshot = &controller->_snapshot;
		
// 		IOHIDElementRef element = IOHIDValueGetElement(value);
		
// 		uint32_t usagePage = IOHIDElementGetUsagePage(element);
// 		uint32_t usage = IOHIDElementGetUsage(element);
		
// 		CFIndex state = (int)IOHIDValueGetIntegerValue(value);
// 		float analog = IOHIDValueGetScaledValue(value, kIOHIDValueScaleTypeCalibrated);
		
// //		NSLog(@"usagePage: 0x%02X, usage 0x%02X, value: %d / %f", usagePage, usage, state, analog);
		
// 		if(usagePage == kHIDPage_Button){
// 			if(usage == controller->_buttonPauseUsageID){if(state) controller.controllerPausedHandler(controller);}
// 			if(usage == controller->_buttonAUsageID){snapshot->buttonA = state;}
// 			if(usage == controller->_buttonBUsageID){snapshot->buttonB = state;}
// 			if(usage == controller->_buttonXUsageID){snapshot->buttonX = state;}
// 			if(usage == controller->_buttonYUsageID){snapshot->buttonY = state;}
// 			if(usage == controller->_lShoulderUsageID){snapshot->leftShoulder = state;}
// 			if(usage == controller->_rShoulderUsageID){snapshot->rightShoulder = state;}

// 			if(!controller->_usesHatSwitch){
// 				if(usage == controller->_dpadLUsageID){snapshot->dpadX = Clamp(snapshot->dpadX - (state ? 1.0f : -1.0f));}
// 				if(usage == controller->_dpadRUsageID){snapshot->dpadX = Clamp(snapshot->dpadX + (state ? 1.0f : -1.0f));}
// 				if(usage == controller->_dpadDUsageID){snapshot->dpadY = Clamp(snapshot->dpadY - (state ? 1.0f : -1.0f));}
// 				if(usage == controller->_dpadUUsageID){snapshot->dpadY = Clamp(snapshot->dpadY + (state ? 1.0f : -1.0f));}
// 			}
// 		}
		
// 		if(usagePage == kHIDPage_GenericDesktop){
// 			if(usage == controller->_lThumbXUsageID ){snapshot->leftThumbstickX  = analog;}
// 			if(usage == controller->_lThumbYUsageID ){snapshot->leftThumbstickY  = analog;}
// 			if(usage == controller->_rThumbXUsageID ){snapshot->rightThumbstickX = analog;}
// 			if(usage == controller->_rThumbYUsageID ){snapshot->rightThumbstickY = analog;}
// 			if(usage == controller->_lTriggerUsageID){snapshot->leftTrigger     = analog;}
// 			if(usage == controller->_rTriggerUsageID){snapshot->rightTrigger    = analog;}
			
// 			if(controller->_usesHatSwitch && usage == kHIDUsage_GD_Hatswitch){
// 				switch(state){
// 					case  0: snapshot->dpadX =  0.0; snapshot->dpadY =  1.0; break;
// 					case  1: snapshot->dpadX =  1.0; snapshot->dpadY =  1.0; break;
// 					case  2: snapshot->dpadX =  1.0; snapshot->dpadY =  0.0; break;
// 					case  3: snapshot->dpadX =  1.0; snapshot->dpadY = -1.0; break;
// 					case  4: snapshot->dpadX =  0.0; snapshot->dpadY = -1.0; break;
// 					case  5: snapshot->dpadX = -1.0; snapshot->dpadY = -1.0; break;
// 					case  6: snapshot->dpadX = -1.0; snapshot->dpadY =  0.0; break;
// 					case  7: snapshot->dpadX = -1.0; snapshot->dpadY =  1.0; break;
// 					default: snapshot->dpadX =  0.0; snapshot->dpadY =  0.0; break;
// 				}
// 			}
// 		}
		
// 		ControllerUpdateSnapshot(controller);
//	}
}

class ExtendedGCController: GCController {
    static let shared : ExtendedGCController = ExtendedGCController()
    static var controllersArray: [GCController] = []
    
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

    private var _vendorName: String?

    func initialize() {
        GCController.initialize()
        
        hidManagerGlobal = IOHIDManagerCreate(kCFAllocatorDefault, IOOptionBits(kIOHIDOptionsTypeNone))
        
        guard let hidManager = hidManagerGlobal else {
            print("HID Manager is nil")
            return
        }

        if (IOHIDManagerOpen(hidManager, IOOptionBits(kIOHIDOptionsTypeNone)) != kIOReturnSuccess) {
		    print("Error initializing ExtendedGCController");
            return;
        }

        print("ok")

        let hidContext = unsafeBitCast(self, to: UnsafeMutableRawPointer.self)

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
        
	    IOHIDManagerRegisterDeviceMatchingCallback(hidManager, controllerConnected, nil);
        IOHIDManagerSetDeviceMatchingMultiple(hidManager, deviceCriteria)

        // Pump the event loop to initially fill the [CCController +controllers] list.
	    // Otherwise the list would be empty, immediately followed by didConnect events.
	    // Not really a problem, but quite how the iOS API works.
        let mode = "CCControllerPollGamepads" as CFString;
	    IOHIDManagerScheduleWithRunLoop(hidManager, CFRunLoopGetCurrent(), mode);
	    while(CFRunLoopRunInMode(CFRunLoopMode(mode), 0, true) == .handledSource) { }
        IOHIDManagerUnscheduleFromRunLoop(hidManager, CFRunLoopGetCurrent(), mode);
	
	    // Schedule the HID manager normally to get callbacks during runtime.
	    IOHIDManagerScheduleWithRunLoop(hidManager, CFRunLoopGetMain(), CFRunLoopMode.defaultMode.rawValue);
    }

    override class func controllers() -> [GCController] {
        // TODO: Find a way to replace the console controllers added by the default system
        // and remove the duplicates so that the system behave normally

        // return super.controllers() + controllersArray
        return controllersArray
    }

    override init() {
        super.init()
    }

    override var vendorName: String? {
        return self._vendorName
    }

    init(device: IOHIDDevice) {
        super.init()
        print("init device")
        
        let manufacturer = IOHIDDeviceGetProperty(device, kIOHIDManufacturerKey as CFString)!
        let product = IOHIDDeviceGetProperty(device, kIOHIDProductKey as CFString)!
        
        self._vendorName = "\(manufacturer) \(product)"

        let vendorId = GameControllerVendor(rawValue: Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDVendorIDKey as CFString) as! NSNumber))!
        let productId = GameControllerProduct(rawValue: Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDProductIDKey as CFString) as! NSNumber))!

        var axisMin = 0
		var axisMax = 256

        // TODO: PS4 Controller and other xbox controllers
        if (vendorId == .microsoft) {
            if (productId == .xboxOneWireless) {
                axisMin = -(1<<15)
		 		axisMax =  (1<<15)

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

        // setupAxis(device, getAxis(device, self._lThumbXUsageID), -1.0,  1.0, axisMin, axisMax, deadZonePercent)
		// setupAxis(device, getAxis(device, self._lThumbYUsageID),  1.0, -1.0, axisMin, axisMax, deadZonePercent)
		// setupAxis(device, getAxis(device, self._rThumbXUsageID), -1.0,  1.0, axisMin, axisMax, deadZonePercent)
		// setupAxis(device, getAxis(device, self._rThumbYUsageID),  1.0, -1.0, axisMin, axisMax, deadZonePercent)
		
		setupAxis(device, getAxis(device, self._lTriggerUsageID), -1.0,  1.0, 0, 1023, 30)
		setupAxis(device, getAxis(device, self._rTriggerUsageID), -1.0,  1.0, 0, 1023, 30)

        IOHIDDeviceRegisterInputValueCallback(device, controllerInput, Unmanaged.passUnretained(self).toOpaque())

        let inputValueMatch = [
            [
                kIOHIDElementUsagePageKey: kHIDPage_GenericDesktop,
            ],
            [
                kIOHIDElementUsagePageKey: kHIDPage_Button,
            ]
        ] as CFArray

		//IOHIDDeviceSetInputValueMatchingMultiple(device, inputValueMatch)
		
		//IOHIDDeviceRegisterRemovalCallback(device, ControllerDisconnected, (void *)CFBridgingRetain(controller));
    }

    private func getAxis(_ device: IOHIDDevice, _ axis: CFIndex) -> IOHIDElement {
        let match = [
            // kIOHIDElementUsagePageKey: kHIDPage_GenericDesktop,
            kIOHIDElementUsageKey: axis
        ] as CFDictionary

        print(match)
        
        let nsArray = IOHIDDeviceCopyMatchingElements(device, match, 0)!
        let elements: Array<IOHIDElement> = nsArray as! Array<IOHIDElement>

        if (elements.count != 1) {
            print("Warning. Oops, didn't find exactly one axis?\(elements.count)")
        }

        return elements[0]
    }

    private func setupAxis(_ device: IOHIDDevice, _ element: IOHIDElement, _ dmin: Float, _ dmax: Float, _ rmin: Int, _ rmax: Int, _ deadZoneValue: Int) {
        IOHIDElementSetProperty(element, kIOHIDElementCalibrationMinKey as CFString, dmin as CFTypeRef)
        IOHIDElementSetProperty(element, kIOHIDElementCalibrationMaxKey as CFString, dmax as CFTypeRef)
        
        IOHIDElementSetProperty(element, kIOHIDElementCalibrationSaturationMinKey as CFString, rmin as CFTypeRef)
        IOHIDElementSetProperty(element, kIOHIDElementCalibrationSaturationMaxKey as CFString, rmax as CFTypeRef)
        
        if (deadZoneValue > 0) {
            // let midRawValue = Float(rmin + rmax) / 2.0
            // let mid: CFIndex = Int(midRawValue)
            // let deadZoneRawValue = Float(rmax - rmin) * deadZoneValue
            // let deadZone: CFIndex = Int(deadZoneRawValue)
            
            IOHIDElementSetProperty(element, kIOHIDElementCalibrationDeadZoneMinKey as CFString, -deadZoneValue as CFTypeRef)
            IOHIDElementSetProperty(element, kIOHIDElementCalibrationDeadZoneMaxKey as CFString, deadZoneValue as CFTypeRef)
        }
    }
}

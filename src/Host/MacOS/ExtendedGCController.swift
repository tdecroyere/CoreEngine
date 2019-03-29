import GameController

import IOKit
import IOKit.hid

func controllerConnected(context: UnsafeMutableRawPointer?, result: IOReturn, sender: UnsafeMutableRawPointer?, device: IOHIDDevice) -> Void {
    print("controller connected")

    if(result == kIOReturnSuccess) {
//		NSURL *url = [[NSBundle mainBundle] URLForResource:@"CCControllerConfig.plist" withExtension:nil];
//		NSDictionary *config = [NSDictionary dictionaryWithContentsOfURL:url];
//		
//		NSAssert(@"CCControllerConfig.plist not found.");
		
		let controller = ExtendedGCController(device: device)
        ExtendedGCController.controllersArray.append(controller)
    }
}

class ExtendedGCController: GCController {
    static let shared : ExtendedGCController = ExtendedGCController()
    static var controllersArray: [GCController] = []

    private var _vendorName: String?

    func initialize() {
        GCController.initialize()
        
        let hidManager = IOHIDManagerCreate(kCFAllocatorDefault, IOOptionBits(kIOHIDOptionsTypeNone))

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
        ]
        
	    IOHIDManagerRegisterDeviceMatchingCallback(hidManager, controllerConnected, nil);
        IOHIDManagerSetDeviceMatchingMultiple(hidManager, deviceCriteria as CFArray)

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

        let vendorId = Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDVendorIDKey as CFString) as! NSNumber)
        let productId = Int(truncating: IOHIDDeviceGetProperty(device, kIOHIDProductIDKey as CFString) as! NSNumber)

        print(String(format:"%02X", productId))
    
		// NSUInteger pid = [(__bridge NSNumber *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDProductIDKey)) unsignedIntegerValue];
		
		// CFIndex axisMin = 0;
		// CFIndex axisMax = 256;
		
		// if(vid == 0x054C){ // Sony
		// 	if(pid == 0x5C4){ // DualShock 4
		// 		NSLog(@"[CCController initWithDevice:] Sony Dualshock 4 detected.");
				
		// 		controller->_lThumbXUsageID = kHIDUsage_GD_X;
		// 		controller->_lThumbYUsageID = kHIDUsage_GD_Y;
		// 		controller->_rThumbXUsageID = kHIDUsage_GD_Z;
		// 		controller->_rThumbYUsageID = kHIDUsage_GD_Rz;
		// 		controller->_lTriggerUsageID = kHIDUsage_GD_Rx;
		// 		controller->_rTriggerUsageID = kHIDUsage_GD_Ry;
				
		// 		controller->_usesHatSwitch = YES;
				
		// 		controller->_buttonPauseUsageID = 0x0A;
		// 		controller->_buttonAUsageID = 0x02;
		// 		controller->_buttonBUsageID = 0x03;
		// 		controller->_buttonXUsageID = 0x01;
		// 		controller->_buttonYUsageID = 0x04;
		// 		controller->_lShoulderUsageID = 0x05;
		// 		controller->_rShoulderUsageID = 0x06;
		// 	}
		// } else if(vid == 0x045E){ // Microsoft
		// 	if(pid == 0x028E || pid == 0x028F || pid == 0x02FD){ // 360 wired/wireless/Xbox one wireless controller
		// 		NSLog(@"[CCController initWithDevice:] Microsoft Xbox 360 controller detected.");
				
		// 		axisMin = -(1<<15);
		// 		axisMax =  (1<<15);
				
		// 		controller->_lThumbXUsageID = kHIDUsage_GD_X;
		// 		controller->_lThumbYUsageID = kHIDUsage_GD_Y;
		// 		controller->_rThumbXUsageID = kHIDUsage_GD_Rx;
		// 		controller->_rThumbYUsageID = kHIDUsage_GD_Ry;
		// 		controller->_lTriggerUsageID = kHIDUsage_GD_Z;
		// 		controller->_rTriggerUsageID = kHIDUsage_GD_Rz;
				
		// 		controller->_dpadLUsageID = 0x0E;
		// 		controller->_dpadRUsageID = 0x0F;
		// 		controller->_dpadDUsageID = 0x0D;
		// 		controller->_dpadUUsageID = 0x0C;
				
		// 		controller->_buttonPauseUsageID = 0x09;
		// 		controller->_buttonAUsageID = 0x01;
		// 		controller->_buttonBUsageID = 0x02;
		// 		controller->_buttonXUsageID = 0x03;
		// 		controller->_buttonYUsageID = 0x04;
		// 		controller->_lShoulderUsageID = 0x05;
		// 		controller->_rShoulderUsageID = 0x06;
		// 	}
		// }
		// NSString *product = (__bridge NSString *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDProductKey));
		// _vendorName = [NSString stringWithFormat:@"%@ %@", manufacturer, product];
		
		// _snapshot.version = 0x0100;
		// _snapshot.size = sizeof(_snapshot);
    }
}

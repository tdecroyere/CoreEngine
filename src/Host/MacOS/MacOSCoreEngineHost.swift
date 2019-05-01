import Cocoa
import CoreEngineInterop
import simd

func getTestBuffer() -> MemoryBuffer {
    let bufferPtr = UnsafeMutablePointer<UInt8>.allocate(capacity: 5)
    let buffer = UnsafeMutableBufferPointer(start: bufferPtr, count: 5)
    buffer[0] = 1
    buffer[1] = 2
    buffer[2] = 3
    buffer[3] = 45
    buffer[4] = 5

	return MemoryBuffer(Id: 1, Pointer: bufferPtr, Length: 5)
}

func addTestHostMethod(_ a: Int32, _ b: Int32) -> Int32 {
	return a + b + 40
}

class MacOSCoreEngineHost {
    public var hostPlatform: HostPlatform!
    var memoryManager: MacOSMemoryManager!
    var renderer: MacOSMetalRenderer!
    var inputsManager: MacOSInputsManager!

    var startEnginePointer: StartEnginePtr?
    var updateEnginePointer: UpdateEnginePtr?

    init(memoryManager: MacOSMemoryManager, renderer: MacOSMetalRenderer, inputsManager: MacOSInputsManager) {
        self.hostPlatform = HostPlatform()
        self.memoryManager = memoryManager
        self.renderer = renderer
        self.inputsManager = inputsManager
    }

    func startEngine(_ appName: String? = nil) {
        initCoreClrSwift()

        var appNameUnsafe: UnsafeMutablePointer<Int8>? = nil

        if (CommandLine.arguments.count > 1) {
            appNameUnsafe = strdup(CommandLine.arguments[1])
        }
        
        self.hostPlatform.TestParameter = 5
        self.hostPlatform.AddTestHostMethod = addTestHostMethod
        self.hostPlatform.GetTestBuffer = getTestBuffer

        self.hostPlatform.MemoryService.MemoryManagerContext = Unmanaged.passUnretained(self.memoryManager).toOpaque()
        self.hostPlatform.MemoryService.CreateMemoryBuffer = createMemoryBuffer
        self.hostPlatform.MemoryService.DestroyMemoryBuffer = destroyMemoryBuffer

        self.hostPlatform.GraphicsService.GraphicsContext = Unmanaged.passUnretained(self.renderer).toOpaque()
        self.hostPlatform.GraphicsService.CreateShader = createShader
        self.hostPlatform.GraphicsService.DebugDrawTriangle = debugDrawTriangle

        self.hostPlatform.InputsService.InputsContext = Unmanaged.passUnretained(self.inputsManager).toOpaque()
        self.hostPlatform.InputsService.GetInputsState = getInputsState
        self.hostPlatform.InputsService.SendVibrationCommand = sendVibrationCommand

        guard let startEngineInterop = self.startEnginePointer else {
            print("CoreEngine StartEngine method is not initialized")
            return
        }

        //let hostPlatformPtr = Unmanaged.passUnretained(self.hostPlatform).toOpaque()

        // TODO: The struct seems to be passed by value instead of this address
        startEngineInterop(appNameUnsafe, &self.hostPlatform)
    }

    func updateEngine(_ deltaTime: Float) {
        guard let updateEngineInterop = self.updateEnginePointer else {
            print("CoreEngine UpdatEngine method is not initialized")
            return
        }

        updateEngineInterop(deltaTime)
    }

    func initCoreClrSwift() {
        let appPath = Bundle.main.bundleURL.appendingPathComponent("Contents/CoreClr")
        let coreLibPath = appPath.appendingPathComponent("libcoreclr.dylib").path
        
        let handle = dlopen(coreLibPath, RTLD_NOW | RTLD_LOCAL)
        
        let coreClrInitializeHandle = dlsym(handle, "coreclr_initialize")
        let coreclr_initialize = unsafeBitCast(coreClrInitializeHandle, to: coreclr_initialize_ptr.self)
        
        let coreClrCreateDelegateHandle = dlsym(handle, "coreclr_create_delegate")
        let coreclr_create_delegate = unsafeBitCast(coreClrCreateDelegateHandle, to: coreclr_create_delegate_ptr.self)

        // TPA List
        let fileManager = FileManager.default
        var tpaList: [String] = []
        
        do {
            var assembliesList = try fileManager.contentsOfDirectory(at: appPath, includingPropertiesForKeys: nil, options: [])
            assembliesList = assembliesList.filter{ $0.pathExtension == "dll" }
            
            for assembly in assembliesList {
                tpaList.append(assembly.path)
                //print(assembly.path)
            }
            //print("TPA list:", tpaList.joined(separator: ":") )

        } catch {
            print("ERROR: Could not find CoreCLR path")
        }
        
        let propertyKeys = [ "TRUSTED_PLATFORM_ASSEMBLIES" ]
        let propertyValues = [ tpaList.joined(separator: ":") ]
        var hostHandle: UnsafeMutableRawPointer? = nil
        var domainId: UInt32 = 0
        var managedDelegate: UnsafeMutableRawPointer? = nil
        
        var cargs = propertyKeys.map { UnsafePointer(strdup($0)) }
        var cargs2 = propertyValues.map { UnsafePointer(strdup($0)) }
        
        defer {
            //cargs.forEach { free($0) }
        }
        var result = coreclr_initialize(appPath.path,
                                        "CoreEngineAppDomain",
                                        1,
                                        &cargs,
                                        &cargs2,
                                        &hostHandle,
                                        &domainId)

        if (result == 0) {
            result = coreclr_create_delegate(hostHandle!, 
                                            domainId,
                                            "CoreEngine",
                                            "CoreEngine.Bootloader",
                                            "StartEngine",
                                            &managedDelegate)

            if (result == 0) {
                self.startEnginePointer = unsafeBitCast(managedDelegate!, to: StartEnginePtr.self)
            }

            result = coreclr_create_delegate(hostHandle!, 
                                            domainId,
                                            "CoreEngine",
                                            "CoreEngine.Bootloader",
                                            "UpdateEngine",
                                            &managedDelegate)

            if (result == 0) {
                self.updateEnginePointer = unsafeBitCast(managedDelegate!, to: UpdateEnginePtr.self)
            }
        }
        
        // TODO: Do not forget to call the shutdownCoreClr method
        
        //dlclose(handle)
    }
}
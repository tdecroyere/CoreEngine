import Foundation
import CoreEngineCommonInterop

public class CoreEngineHost {
    public var hostPlatform: HostPlatform!
    let renderer: MetalRenderer
    let inputsManager: InputsManager

    var startEnginePointer: StartEnginePtr?
    var updateEnginePointer: UpdateEnginePtr?
    var renderPointer: RenderPtr?

    public init(renderer: MetalRenderer, inputsManager: InputsManager) {
        self.hostPlatform = HostPlatform()
        self.renderer = renderer
        self.inputsManager = inputsManager
    }

    public func startEngine(_ appName: String? = nil) {
        initCoreClrSwift()

        var appNameUnsafe: UnsafeMutablePointer<Int8>? = nil

        if (appName != nil) {
            appNameUnsafe = strdup(appName)
        }
        
        initGraphicsService(self.renderer, &self.hostPlatform.GraphicsService)
        initInputsService(self.inputsManager, &self.hostPlatform.InputsService)

        guard let startEngineInterop = self.startEnginePointer else {
            print("CoreEngine StartEngine method is not initialized")
            return
        }

        //let hostPlatformPtr = Unmanaged.passUnretained(self.hostPlatform).toOpaque()

        // TODO: The struct seems to be passed by value instead of this address
        startEngineInterop(appNameUnsafe, &self.hostPlatform)
    }

    public func updateEngine(_ deltaTime: Float) {
        guard let updateEngineInterop = self.updateEnginePointer else {
            print("CoreEngine UpdatEngine method is not initialized")
            return
        }

        updateEngineInterop(deltaTime)
    }

    public func render() {
        guard let renderPointer = self.renderPointer else {
            print("CoreEngine Render method is not initialized")
            return
        }

        renderPointer()
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

            result = coreclr_create_delegate(hostHandle!, 
                                            domainId,
                                            "CoreEngine",
                                            "CoreEngine.Bootloader",
                                            "Render",
                                            &managedDelegate)

            if (result == 0) {
                self.renderPointer = unsafeBitCast(managedDelegate!, to: RenderPtr.self)
            }
        }
        
        // TODO: Do not forget to call the shutdownCoreClr method
        
        //dlclose(handle)
    }
}
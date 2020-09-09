import Foundation
import CoreEngineCommonInterop

public class CoreEngineHost {
    public var hostPlatform: HostPlatform!
    let nativeUIService: MacOSNativeUIService
    let graphicsService: MetalGraphicsService
    let inputsManager: InputsManager

    var startEnginePointer: StartEnginePtr?

    public init(nativeUIService: MacOSNativeUIService, graphicsService: MetalGraphicsService, inputsManager: InputsManager) {
        self.hostPlatform = HostPlatform()
        self.nativeUIService = nativeUIService
        self.graphicsService = graphicsService
        self.inputsManager = inputsManager
    }

    public func startEngine(_ assemblyName: String) {
        initCoreClrSwift(assemblyName)

        initNativeUIService(self.nativeUIService, &self.hostPlatform.NativeUIService)
        initGraphicsService(self.graphicsService, &self.hostPlatform.GraphicsService)
        initInputsService(self.inputsManager, &self.hostPlatform.InputsService)

        guard let startEngineInterop = self.startEnginePointer else {
            print("CoreEngine StartEngine method is not initialized")
            return
        }

        startEngineInterop(self.hostPlatform)
    }

    func initCoreClrSwift(_ assemblyName: String) {
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
                                            assemblyName,
                                            "Program",
                                            "Main",
                                            &managedDelegate)

            if (result == 0) {
                self.startEnginePointer = unsafeBitCast(managedDelegate!, to: StartEnginePtr.self)
            }
        }
        
        // TODO: Do not forget to call the shutdownCoreClr method
        
        //dlclose(handle)
    }
}
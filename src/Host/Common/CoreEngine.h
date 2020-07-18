#pragma once

struct Vector2
{
    float X, Y;
};

struct Vector3
{
    float X, Y, Z;
};

struct Vector4
{
    float X, Y, Z, W;
};

struct Matrix4x4
{
    float M11, M12, M13, M14;
    float M21, M22, M23, M24;
    float M31, M32, M33, M34;
    float M41, M42, M43, M44;
};

struct Nullableint
{
    int HasValue;
    int Value;
};

struct Nullableuint
{
    int HasValue;
    unsigned int Value;
};

struct NullableVector4
{
    int HasValue;
    struct Vector4 Value;
};

enum GraphicsTextureFormat : int;

struct NullableGraphicsTextureFormat
{
    int HasValue;
    enum GraphicsTextureFormat Value;
};

enum GraphicsBlendOperation : int;

struct NullableGraphicsBlendOperation
{
    int HasValue;
    enum GraphicsBlendOperation Value;
};

#include "GraphicsService.h"
#include "InputsService.h"


struct HostPlatform
{
    struct GraphicsService GraphicsService;
    struct InputsService InputsService;
};

typedef void (*StartEnginePtr)(const char* appName, struct HostPlatform* hostPlatform);
typedef void (*UpdateEnginePtr)(float deltaTime);

typedef int (*coreclr_initialize_ptr)(const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

typedef int (*coreclr_create_delegate_ptr)(void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate);

typedef int (*coreclr_shutdown_ptr)(void* hostHandle,
            unsigned int domainId);
  
#ifdef _WINDOWS_
using namespace std;

string CoreEngineHost_BuildTpaList(string path)
{
    string tpaList = "";

    string searchPath = path;
    searchPath = searchPath + "\\*.dll";

    WIN32_FIND_DATAA findData;
    HANDLE fileHandle = FindFirstFile(searchPath.c_str(), &findData);

    if (fileHandle != INVALID_HANDLE_VALUE)
    {
        do
        {
            tpaList = tpaList + (path) + "\\" + string(findData.cFileName) + ";";
        }
        while (FindNextFileA(fileHandle, &findData));
        FindClose(fileHandle);
    }

    return tpaList;
}

bool CoreEngineHost_InitCoreClr(StartEnginePtr* startEnginePointer, UpdateEnginePtr* updateEnginePointer)
{
    string hostPath;

#ifdef _WINDOWS_
    printf("Windows CoreEngine Host\n");
    TCHAR tmp[MAX_PATH];
    GetModuleFileName(NULL, tmp, MAX_PATH);

    hostPath = string(tmp);
    hostPath = hostPath.substr(0, hostPath.find_last_of( "\\/" ));
    // hostPath = hostPath.substr(0, hostPath.find_last_of( "\\/" ));

#else
    printf("Unix CoreEngine Host\n");
    // auto resolved = realpath(argv[0], host_path);
    // assert(resolved != nullptr);
#endif

    const string tpaList = CoreEngineHost_BuildTpaList(hostPath);

    HMODULE coreClr = LoadLibraryA("CoreClr.dll");

    coreclr_initialize_ptr initializeCoreClr = (coreclr_initialize_ptr)GetProcAddress(coreClr, "coreclr_initialize");
    coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)GetProcAddress(coreClr, "coreclr_create_delegate");
    coreclr_shutdown_ptr shutdownCoreClr = (coreclr_shutdown_ptr)GetProcAddress(coreClr, "coreclr_shutdown");

    const char* propertyKeys[1] = {
        "TRUSTED_PLATFORM_ASSEMBLIES"
    };

    // TODO: Delete temp memory
    const char* tpaListPtr = tpaList.c_str();
    const char* appPathPtr = hostPath.c_str();

    const char* propertyValues[1] = {
        tpaListPtr
    };

    void* hostHandle;
    unsigned int domainId;

    int result = initializeCoreClr(appPathPtr,
                                    "CoreEngineAppDomain",
                                    1,
                                    propertyKeys,
                                    propertyValues,
                                    &hostHandle,
                                    &domainId);

    if (result == 0)
    {
        void* managedDelegate;

        result = createManagedDelegate(hostHandle, 
                                        domainId,
                                        "CoreEngine",
                                        "CoreEngine.Bootloader",
                                        "StartEngine",
                                        (void**)&managedDelegate);

        if (result == 0)
        {
            *startEnginePointer = (StartEnginePtr)managedDelegate;
        }

        result = createManagedDelegate(hostHandle, 
                                        domainId,
                                        "CoreEngine",
                                        "CoreEngine.Bootloader",
                                        "UpdateEngine",
                                        (void**)&managedDelegate);

        if (result == 0)
        {
            *updateEnginePointer = (UpdateEnginePtr)managedDelegate;
        }
    }

    return true;

    // TODO: Do not forget to call the shutdownCoreClr method
}    
#endif
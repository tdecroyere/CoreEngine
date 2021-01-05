#pragma once
#include "CoreEngine.h"
#include "inc/hostfxr.h"
#include "inc/coreclr_delegates.h"
#include "inc/nethost.h"

#include <string>

using namespace std;

#ifdef _WINDOWS_
    #include <Windows.h>

    #define STR(s) L ## s
    #define CH(c) L ## c
    #define DIR_SEPARATOR L'\\'

    void* NativeHost_LoadLibrary(const char_t* path)
    {
        auto module = ::LoadLibraryW(path);
        assert(module != nullptr);
        return (void*)module;
    }
    void* NativeHost_GetExport(void* module, const char* name)
    {
        void* functionPointer = ::GetProcAddress((HMODULE)module, name);
        assert(functionPointer != nullptr);
        return functionPointer;
    }
#else
    #include <dlfcn.h>
    #include <limits.h>

    #define STR(s) s
    #define CH(c) c
    #define DIR_SEPARATOR '/'
    #define MAX_PATH PATH_MAX

    void* NativeHost_LoadLibrary(const char_t *path)
    {
        void *h = dlopen(path, RTLD_LAZY | RTLD_LOCAL);
        assert(h != nullptr);
        return h;
    }
    void* NativeHost_GetExport(void *h, const char *name)
    {
        void *f = dlsym(h, name);
        assert(f != nullptr);
        return f;
    }
#endif

// Globals to hold hostfxr exports
hostfxr_initialize_for_runtime_config_fn init_fptr;
hostfxr_get_runtime_delegate_fn get_delegate_fptr;
hostfxr_close_fn close_fptr;

using string_t = std::basic_string<char_t>;

bool NativeHost_LoadHostfxr()
{
    // Pre-allocate a large buffer for the path to hostfxr
    char_t buffer[MAX_PATH];
    size_t buffer_size = sizeof(buffer) / sizeof(char_t);
    int rc = get_hostfxr_path(buffer, &buffer_size, nullptr);
    if (rc != 0)
        return false;

    // Load hostfxr and get desired exports
    void *lib = NativeHost_LoadLibrary(buffer);
    init_fptr = (hostfxr_initialize_for_runtime_config_fn)NativeHost_GetExport(lib, "hostfxr_initialize_for_runtime_config");
    get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)NativeHost_GetExport(lib, "hostfxr_get_runtime_delegate");
    close_fptr = (hostfxr_close_fn)NativeHost_GetExport(lib, "hostfxr_close");

    return (init_fptr && get_delegate_fptr && close_fptr);
}

load_assembly_and_get_function_pointer_fn NativeHost_GetDotnetLoadAssembly(const char_t *config_path)
{
    // Load .NET Core
    void *load_assembly_and_get_function_pointer = nullptr;
    hostfxr_handle cxt = nullptr;
    int rc = init_fptr(config_path, nullptr, &cxt);
    if (rc != 0 || cxt == nullptr)
    {
        close_fptr(cxt);
        return nullptr;
    }

    // Get the load assembly function pointer
    rc = get_delegate_fptr(
        cxt,
        hdt_load_assembly_and_get_function_pointer,
        &load_assembly_and_get_function_pointer);
    // if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
    //     std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

    close_fptr(cxt);
    return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
}

bool NativeHost_LoadEngine(StartEnginePtr* startEnginePointer, wstring assemblyName, bool nativeLoad)
{
    char_t hostPath[MAX_PATH];
    
#ifdef _WINDOWS_
    auto size = GetModuleFileNameW(NULL, hostPath, MAX_PATH);
    assert(size != 0);
#else
    auto resolved = realpath(argv[0], host_path);
    assert(resolved != nullptr);
#endif

    wstring root_path = hostPath;
    auto pos = root_path.find_last_of(DIR_SEPARATOR);
    assert(pos != wstring::npos);
    root_path = root_path.substr(0, pos + 1);

    const string_t dotnetlib_path = root_path + assemblyName + L".dll";

    if (nativeLoad)
    {
        void *lib = NativeHost_LoadLibrary(dotnetlib_path.c_str());
        *startEnginePointer = (StartEnginePtr)NativeHost_GetExport(lib, "main");
    }

    else
    {
        if (!NativeHost_LoadHostfxr())
        {
            assert(false && "Failure: load_hostfxr()");
            return EXIT_FAILURE;
        }

        const string_t config_path = root_path + assemblyName + STR(".runtimeconfig.json");
        load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
        load_assembly_and_get_function_pointer = NativeHost_GetDotnetLoadAssembly(config_path.c_str());
        assert(load_assembly_and_get_function_pointer != nullptr && "Failure: get_dotnet_load_assembly()");

        const char_t* dotnetlib_pathPtr = dotnetlib_path.c_str();
        const string_t dotnetType = (L"Program, " + assemblyName);
        const char_t *dotnet_type = dotnetType.c_str();
        const char_t *dotnet_type_method = STR("Main");

        void* outputPointer = nullptr;

        auto rc = load_assembly_and_get_function_pointer(
            dotnetlib_pathPtr,
            dotnet_type,
            dotnet_type_method /*method_name*/,
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            (void**)&outputPointer);

        assert(rc == 0 && outputPointer != nullptr && "Failure: load_assembly_and_get_function_pointer()");

        *startEnginePointer = (StartEnginePtr)outputPointer;
    }
    return true;
}    
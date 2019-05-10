#pragma once

using namespace Windows::ApplicationModel;
using namespace Windows::ApplicationModel::Core;

InputsState inputsState = {};

int AddTestHostMethod(int a, int b)
{
	return a + b;
}

::MemoryBuffer GetTestBuffer()
{
	unsigned char* testBuffer = new unsigned char[5];

	testBuffer[0] = 1;
	testBuffer[1] = 2;
	testBuffer[2] = 3;
	testBuffer[3] = 4;
	testBuffer[4] = 5;

    ::MemoryBuffer span = {};
    span.Pointer = testBuffer;
    span.Length = 5;

	return span;
}

InputsState GetInputsState(void* inputsContext)
{
    printf("GetInputsState\n");
    return inputsState;
}


MemoryBuffer CreateMemoryBuffer(void* memoryManagerContext, int length)
{
    unsigned char* buffer = new unsigned char[length];

    MemoryBuffer span = {};
    span.Pointer = buffer;
    span.Length = length;

	return span;
}

void DestroyMemoryBuffer(void* memoryManagerContext, unsigned int memoryBufferId)
{

}

unsigned int CreateGraphicsBuffer(void* graphicsContext, MemoryBuffer data)
{
    return 0;
}

void DrawPrimitives(void* graphicsContext, int primitiveCount, unsigned int vertexBufferId, unsigned int indexBufferId, struct Matrix4x4 worldMatrix)
{

}



class WindowsCoreEngineHost
{
public:
    WindowsCoreEngineHost()
    {

    }

    void StartEngine(std::string appName)
    {
        InitCoreClr();

        // Add asserts to check for null values

        HostPlatform hostPlatform = {};
        hostPlatform.TestParameter = 5;

        hostPlatform.MemoryService.CreateMemoryBuffer = CreateMemoryBuffer;
        hostPlatform.MemoryService.DestroyMemoryBuffer = DestroyMemoryBuffer;

        hostPlatform.AddTestHostMethod = AddTestHostMethod;
        hostPlatform.GetTestBuffer = GetTestBuffer;

        hostPlatform.GraphicsService.CreateGraphicsBuffer = CreateGraphicsBuffer;
        hostPlatform.GraphicsService.DrawPrimitives = DrawPrimitives;

        hostPlatform.InputsService.GetInputsState = GetInputsState;

        char* appNamePtr = nullptr;
        
        if (!appName.empty())
        {
            appNamePtr = (char*)appName.c_str();
        }

        this->startEnginePointer(appNamePtr, &hostPlatform);
    }

    void UpdateEngine(float deltaTime) 
    {
        this->updateEnginePointer(deltaTime);
    }

private:
    StartEnginePtr startEnginePointer;    
    UpdateEnginePtr updateEnginePointer;

    void InitCoreClr()
    {
        std::string appPath = to_string(Package::Current().InstalledLocation().Path());

	    const std::string tpaList = BuildTpaList(appPath);

        HMODULE coreClr = LoadPackagedLibrary(L"CoreClr.dll", 0);

	    coreclr_initialize_ptr initializeCoreClr = (coreclr_initialize_ptr)GetProcAddress(coreClr, "coreclr_initialize");
	    coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)GetProcAddress(coreClr, "coreclr_create_delegate");
	    coreclr_shutdown_ptr shutdownCoreClr = (coreclr_shutdown_ptr)GetProcAddress(coreClr, "coreclr_shutdown");

        const char* propertyKeys[1] = {
            "TRUSTED_PLATFORM_ASSEMBLIES"
        };

        const char* propertyValues[1] = {
            tpaList.c_str()
        };

	    void* hostHandle;
        unsigned int domainId;

        int result = initializeCoreClr(appPath.c_str(),
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
                this->startEnginePointer = (StartEnginePtr)managedDelegate;
            }

            result = createManagedDelegate(hostHandle, 
                                           domainId,
                                           "CoreEngine",
                                           "CoreEngine.Bootloader",
                                           "UpdateEngine",
                                           (void**)&managedDelegate);

            if (result == 0)
            {
                this->updateEnginePointer = (UpdateEnginePtr)managedDelegate;
            }
        }

        // TODO: Do not forget to call the shutdownCoreClr method
    }    

    std::string BuildTpaList(std::string path)
    {
        std::string tpaList = "";

        std::string searchPath = path;
        searchPath = searchPath + "\\*.dll";

        WIN32_FIND_DATAA findData;
        HANDLE fileHandle = FindFirstFile(searchPath.c_str(), &findData);

        if (fileHandle != INVALID_HANDLE_VALUE)
        {
            do
            {
                tpaList = tpaList + (path) + "\\" + findData.cFileName + ";";
            }
            while (FindNextFileA(fileHandle, &findData));
            FindClose(fileHandle);
        }

        return tpaList;
    }
};
#pragma once

#include "WindowsCommon.h"

using namespace std;


InputsState inputsState = {};


int AddTestHostMethod(int a, int b)
{
	return a + b;
}

Span GetTestBuffer()
{
	unsigned char* testBuffer = new unsigned char[5];

	testBuffer[0] = 1;
	testBuffer[1] = 2;
	testBuffer[2] = 3;
	testBuffer[3] = 4;
	testBuffer[4] = 5;

    Span span = {};
    span.Buffer = testBuffer;
    span.Length = 5;

	return span;
}

void DebugDrawTriangle(void* graphicsContext, Vector4 color1, Vector4 color2, Vector4 color3, Matrix4x4 worldMatrix)
{
    printf("DebugDrawTriangle Color1(%f, %f, %f, %f)\n", color1.X, color1.Y, color1.Z, color1.W);
    printf("DebugDrawTriangle Color2(%f, %f, %f, %f)\n", color2.X, color2.Y, color2.Z, color2.W);
    printf("DebugDrawTriangle Color3(%f, %f, %f, %f)\n", color3.X, color3.Y, color3.Z, color3.W);
}

InputsState GetInputsState(void* inputsContext)
{
    printf("GetInputsState\n");
    return inputsState;
}


class WindowsCoreEngineHost
{
public:
    void StartEngine(string appName) 
    {
        InitCoreClr();

        // Add asserts to check for null values

        HostPlatform hostPlatform = {};
        hostPlatform.TestParameter = 5;

        hostPlatform.AddTestHostMethod = AddTestHostMethod;
        hostPlatform.GetTestBuffer = GetTestBuffer;

        hostPlatform.GraphicsService.DebugDrawTriangle = DebugDrawTriangle;

        hostPlatform.InputsService.GetInputsState = GetInputsState;

        char* appNamePtr = nullptr;
        
        if (!appName.empty())
        {
            appNamePtr = (char*)appName.c_str();
        }

        this->startEnginePointer(appNamePtr, &hostPlatform);
    }

    void UpdateEngine(real32 deltaTime) 
    {
        this->updateEnginePointer(deltaTime);
    }

private:
    StartEnginePtr startEnginePointer;    
    UpdateEnginePtr updateEnginePointer;

    void InitCoreClr()
    {
        string appPath = "C:\\Projects\\perso\\CoreEngine\\build\\Windows";
        string coreClrPath = appPath + "\\CoreClr.dll";

	    string tpaList = BuildTpaList(appPath);

        HMODULE coreClr = LoadLibraryExA(coreClrPath.c_str(), NULL, 0);

	    coreclr_initialize_ptr initializeCoreClr = (coreclr_initialize_ptr)GetProcAddress(coreClr, "coreclr_initialize");
	    coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)GetProcAddress(coreClr, "coreclr_create_delegate");
	    coreclr_shutdown_ptr shutdownCoreClr = (coreclr_shutdown_ptr)GetProcAddress(coreClr, "coreclr_shutdown");

        const char* propertyKeys[] = {
            "TRUSTED_PLATFORM_ASSEMBLIES"
        };

        const char* propertyValues[] = {
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

    string BuildTpaList(string path)
    {
        string tpaList = "";

        string searchPath = path;
        searchPath.append("\\*.dll");

        WIN32_FIND_DATAA findData;
        HANDLE fileHandle = FindFirstFileA(searchPath.c_str(), &findData);

        if (fileHandle != INVALID_HANDLE_VALUE)
        {
            do
            {
                tpaList.append(path);
                tpaList.append("\\");
                tpaList.append(findData.cFileName);
                tpaList.append(";");
            }
            while (FindNextFileA(fileHandle, &findData));
            FindClose(fileHandle);
        }

        return tpaList;
    }
};
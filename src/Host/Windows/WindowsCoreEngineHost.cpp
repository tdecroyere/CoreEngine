#pragma once

#include "WindowsCommon.h"
#include "WindowsCoreEngineHost.h"

using namespace winrt;
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

void SendVibrationCommand(void* inputsContext, unsigned char playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned char duration10ms)
{

}


::MemoryBuffer CreateMemoryBuffer(void* memoryManagerContext, int length)
{
    unsigned char* buffer = new unsigned char[length];

    ::MemoryBuffer span = {};
    span.Pointer = buffer;
    span.Length = length;

	return span;
}

void DestroyMemoryBuffer(void* memoryManagerContext, unsigned int memoryBufferId)
{

}

Vector2 GetRenderSize(void* graphicsContext)
{
    return Vector2();
}

unsigned int CreateShader(void* graphicsContext, struct MemoryBuffer shaderByteCode)
{
    return 0;
}

unsigned int CreateGraphicsBuffer(void* graphicsContext, ::MemoryBuffer data)
{
    return 0;
}

void SetRenderPassConstants(void* graphicsContext, struct MemoryBuffer data)
{

}

void DrawPrimitives(void* graphicsContext, int primitiveCount, unsigned int vertexBufferId, unsigned int indexBufferId, struct Matrix4x4 worldMatrix)
{
    OutputDebugString("Draw Primitives\n");
}

WindowsCoreEngineHost::WindowsCoreEngineHost()
{

}

void WindowsCoreEngineHost::StartEngine(hstring appName)
{
    InitCoreClr();

    // Add asserts to check for null values

    HostPlatform hostPlatform = {};
    hostPlatform.TestParameter = 5;
    hostPlatform.AddTestHostMethod = AddTestHostMethod;
    hostPlatform.GetTestBuffer = GetTestBuffer;

    hostPlatform.MemoryService.CreateMemoryBuffer = CreateMemoryBuffer;
    hostPlatform.MemoryService.DestroyMemoryBuffer = DestroyMemoryBuffer;

    hostPlatform.GraphicsService.GetRenderSize = GetRenderSize;
    hostPlatform.GraphicsService.CreateShader = CreateShader;
    hostPlatform.GraphicsService.CreateGraphicsBuffer = CreateGraphicsBuffer;
    hostPlatform.GraphicsService.SetRenderPassConstants = SetRenderPassConstants;
    hostPlatform.GraphicsService.DrawPrimitives = DrawPrimitives;

    hostPlatform.InputsService.GetInputsState = GetInputsState;
    hostPlatform.InputsService.SendVibrationCommand = SendVibrationCommand;

    // TODO: Delete temp memory
    char* appNamePtr = ConvertHStringToCharPtr(appName);
    this->startEnginePointer(appNamePtr, &hostPlatform);
}

void WindowsCoreEngineHost::UpdateEngine(float deltaTime) 
{
    this->updateEnginePointer(deltaTime);
}

void WindowsCoreEngineHost::InitCoreClr()
{
    hstring appPath = Package::Current().InstalledLocation().Path();
    const hstring tpaList = BuildTpaList(appPath);

    HMODULE coreClr = LoadPackagedLibrary(L"CoreClr.dll", 0);

    coreclr_initialize_ptr initializeCoreClr = (coreclr_initialize_ptr)GetProcAddress(coreClr, "coreclr_initialize");
    coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)GetProcAddress(coreClr, "coreclr_create_delegate");
    coreclr_shutdown_ptr shutdownCoreClr = (coreclr_shutdown_ptr)GetProcAddress(coreClr, "coreclr_shutdown");

    const char* propertyKeys[1] = {
        "TRUSTED_PLATFORM_ASSEMBLIES"
    };

    // TODO: Delete temp memory
    char* tpaListPtr = ConvertHStringToCharPtr(tpaList);
    char* appPathPtr = ConvertHStringToCharPtr(appPath);

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

hstring WindowsCoreEngineHost::BuildTpaList(hstring path)
{
    hstring tpaList = L"";

    hstring searchPath = path;
    searchPath = searchPath + L"\\*.dll";

    WIN32_FIND_DATAA findData;
    HANDLE fileHandle = FindFirstFile(to_string(searchPath).c_str(), &findData);

    if (fileHandle != INVALID_HANDLE_VALUE)
    {
        do
        {
            tpaList = tpaList + (path) + L"\\" + to_hstring(findData.cFileName) + L";";
        }
        while (FindNextFileA(fileHandle, &findData));
        FindClose(fileHandle);
    }

    return tpaList;
}

char* WindowsCoreEngineHost::ConvertHStringToCharPtr(hstring value)
{
    char* resultPtr = new char[value.size() + 1];
    to_string(value).copy(resultPtr, value.size());
	resultPtr[value.size()] = '\0';

    return resultPtr;
}
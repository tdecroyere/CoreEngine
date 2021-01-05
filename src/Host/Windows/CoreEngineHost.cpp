#pragma once
#include "WindowsCommon.h"
#include "CoreEngineHost.h"
#include "HostServices/NativeUIServiceInterop.h"
#include "HostServices/GraphicsServiceInterop.h"
#include "HostServices/InputsServiceInterop.h"

using namespace std;

CoreEngineHost::CoreEngineHost(const wstring assemblyName, const WindowsNativeUIService& nativeUIService, const Direct3D12GraphicsService& graphicsService, const WindowsInputsService& inputsService) : nativeUIService(nativeUIService), graphicsService(graphicsService), inputsService(inputsService)
{
    NativeHost_LoadEngine(&this->startEnginePointer, assemblyName, false);
}

void CoreEngineHost::StartEngine(string appName)
{
    // Add asserts to check for null values

    HostPlatform hostPlatform = {};

    InitNativeUIService(this->nativeUIService, &hostPlatform.NativeUIService);
    InitGraphicsService(this->graphicsService, &hostPlatform.GraphicsService);
    InitInputsService(this->inputsService, &hostPlatform.InputsService);

    // TODO: Delete temp memory
    const char* appNamePtr = appName.c_str();
    this->startEnginePointer(hostPlatform);
}

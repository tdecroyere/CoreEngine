#pragma once
#include "WindowsCommon.h"
#include "CoreEngineHost.h"
#include "HostServices/GraphicsServiceInterop.h"
#include "HostServices/InputsServiceInterop.h"

using namespace std;

CoreEngineHost::CoreEngineHost(const Direct3D12GraphicsService& graphicsService, const WindowsInputsService& inputsService) : graphicsService(graphicsService), inputsService(inputsService)
{
    CoreEngineHost_InitCoreClr(&this->startEnginePointer, &this->updateEnginePointer);
}

void CoreEngineHost::StartEngine(string appName)
{

    // Add asserts to check for null values

    HostPlatform hostPlatform = {};

    InitGraphicsService(this->graphicsService, &hostPlatform.GraphicsService);
    InitInputsService(this->inputsService, &hostPlatform.InputsService);

    // TODO: Delete temp memory
    const char* appNamePtr = appName.c_str();
    this->startEnginePointer(appNamePtr, &hostPlatform);
}

void CoreEngineHost::UpdateEngine(float deltaTime) 
{
    this->updateEnginePointer(deltaTime);
}

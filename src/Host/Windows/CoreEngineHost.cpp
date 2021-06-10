#pragma once
#include "WindowsCommon.h"
#include "CoreEngineHost.h"
#include "HostServices/WindowsNativeUIServiceInterop.h"
#include "HostServices/Direct3D12GraphicsServiceInterop.h"
#include "HostServices/VulkanGraphicsServiceInterop.h"
#include "HostServices/WindowsInputsServiceInterop.h"

using namespace std;

CoreEngineHost::CoreEngineHost(const wstring assemblyName, const WindowsNativeUIService* nativeUIService, const Direct3D12GraphicsService* direct3d12GraphicsService, const VulkanGraphicsService* vulkanGraphicsService, const WindowsInputsService* inputsService) : nativeUIService(nativeUIService), direct3dGraphicsService(direct3d12GraphicsService), vulkanGraphicsService(vulkanGraphicsService), inputsService(inputsService)
{
    NativeHost_LoadEngine(&this->startEnginePointer, assemblyName, false);
}

void CoreEngineHost::StartEngine()
{
    HostPlatform hostPlatform = {};

    InitWindowsNativeUIService(this->nativeUIService, &hostPlatform.NativeUIService);

    if (this->direct3dGraphicsService != nullptr)
    {
        InitDirect3D12GraphicsService(this->direct3dGraphicsService, &hostPlatform.GraphicsService);
    }

    else
    {
        InitVulkanGraphicsService(this->vulkanGraphicsService, &hostPlatform.GraphicsService);
    }

    InitWindowsInputsService(this->inputsService, &hostPlatform.InputsService);

    // TODO: Delete temp memory
    this->startEnginePointer(hostPlatform);
}

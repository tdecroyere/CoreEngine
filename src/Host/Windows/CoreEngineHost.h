#pragma once
#include "WindowsCommon.h"
#include "WindowsNativeUIService.h"
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "../Common/CoreEngine.h"
#include "../Common/NativeHost.cpp"

using namespace std;

class CoreEngineHost
{
public:
    CoreEngineHost(const wstring assemblyName, const WindowsNativeUIService& nativeUIService, const Direct3D12GraphicsService& graphicsService, const WindowsInputsService& inputsService);

    void StartEngine();

private:
    const WindowsNativeUIService& nativeUIService;
    const Direct3D12GraphicsService& graphicsService;
    const WindowsInputsService& inputsService;

    StartEnginePtr startEnginePointer;    
};
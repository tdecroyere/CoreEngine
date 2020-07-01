#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "../Common/CoreEngine.h"

using namespace std;

class CoreEngineHost
{
public:
    CoreEngineHost(const Direct3D12GraphicsService& graphicsService, const WindowsInputsService& inputsService);

    void StartEngine(string appName);
    void UpdateEngine(float deltaTime);

private:
    const Direct3D12GraphicsService& graphicsService;
    const WindowsInputsService& inputsService;

    StartEnginePtr startEnginePointer;    
    UpdateEnginePtr updateEnginePointer;
};
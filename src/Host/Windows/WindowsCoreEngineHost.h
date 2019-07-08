#pragma once

#include "../Common/CoreEngine.h"
#include "WindowsDirect3D12Renderer.h"

class WindowsCoreEngineHost
{
public:
    WindowsCoreEngineHost(WindowsDirect3D12Renderer* renderer);

    void StartEngine(winrt::hstring appName);
    void UpdateEngine(float deltaTime);

private:
    WindowsDirect3D12Renderer* renderer;

    StartEnginePtr startEnginePointer;    
    UpdateEnginePtr updateEnginePointer;

    void InitCoreClr();

    winrt::hstring BuildTpaList(winrt::hstring path);
    char* ConvertHStringToCharPtr(winrt::hstring value);
};
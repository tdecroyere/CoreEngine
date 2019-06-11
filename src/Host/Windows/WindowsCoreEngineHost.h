#pragma once

#include "../Common/CoreEngine.h"

class WindowsCoreEngineHost
{
public:
    WindowsCoreEngineHost();

    void StartEngine(winrt::hstring appName);
    void UpdateEngine(float deltaTime);

private:
    StartEnginePtr startEnginePointer;    
    UpdateEnginePtr updateEnginePointer;

    void InitCoreClr();

    winrt::hstring BuildTpaList(winrt::hstring path);
};
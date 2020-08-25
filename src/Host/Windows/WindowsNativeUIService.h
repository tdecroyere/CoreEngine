#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

#undef CreateWindow

class WindowsNativeUIService
{
    public:
        WindowsNativeUIService(HINSTANCE applicationInstance);
        ~WindowsNativeUIService();

        void* CreateWindow(char* title, int width, int height);
        struct Vector2 GetWindowRenderSize(void* windowPointer);

    private:
        HINSTANCE applicationInstance;
        uint32_t mainScreenDpi;
        float mainScreenScaling;
};
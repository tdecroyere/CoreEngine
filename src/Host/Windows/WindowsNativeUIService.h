#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

#undef CreateWindow

class WindowsNativeUIService
{
    public:
        WindowsNativeUIService(HINSTANCE applicationInstance);
        ~WindowsNativeUIService();

        void* CreateWindow(char* title, int width, int height, enum NativeWindowState windowState);
        void SetWindowTitle(void* windowPointer, char* title);
        struct Vector2 GetWindowRenderSize(void* windowPointer);
        struct NativeAppStatus ProcessSystemMessages();

    private:
        HINSTANCE applicationInstance;
        uint32_t mainScreenDpi;
        float mainScreenScaling;
};
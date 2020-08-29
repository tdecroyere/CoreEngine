#pragma once
#include "WindowsCommon.h"
#include "WindowsNativeUIService.h"
#include "WindowsNativeUIServiceUtils.h"

WindowsNativeUIService::WindowsNativeUIService(HINSTANCE applicationInstance)
{
    this->applicationInstance = applicationInstance;

	WNDCLASSA windowClass {};
	windowClass.style = CS_HREDRAW | CS_VREDRAW;
	windowClass.lpfnWndProc = Win32WindowCallBack;
	windowClass.hInstance = applicationInstance;
	windowClass.lpszClassName = "CoreEngineClass";
	windowClass.hCursor = LoadCursorA(NULL, IDC_ARROW);

	if (RegisterClassA(&windowClass))
	{
		HMODULE shcoreLibrary = LoadLibrary("shcore.dll");

		SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        this->mainScreenDpi = GetDpiForWindow(GetDesktopWindow());
        this->mainScreenScaling = static_cast<float>(this->mainScreenDpi) / 96.0f;
    }
}

WindowsNativeUIService::~WindowsNativeUIService()
{
}

void* WindowsNativeUIService::CreateWindow(char* title, int width, int height, enum NativeWindowState windowState)
{
    RECT clientRectangle;
    clientRectangle.left = 0;
    clientRectangle.top = 0;
    clientRectangle.right = static_cast<LONG>(width * this->mainScreenScaling);
    clientRectangle.bottom = static_cast<LONG>(height * this->mainScreenScaling);

    AdjustWindowRectExForDpi(&clientRectangle, WS_OVERLAPPEDWINDOW, false, 0, this->mainScreenDpi);

    // RECT clientRectangle = { 0, 0, width, height };
    // AdjustWindowRect(&clientRectangle, WS_OVERLAPPEDWINDOW, false);
    width = clientRectangle.right - clientRectangle.left;
    height = clientRectangle.bottom - clientRectangle.top;

    // Compute the position of the window to center it 
    RECT desktopRectangle;
    GetClientRect(GetDesktopWindow(), &desktopRectangle);
    int x = (desktopRectangle.right / 2) - (width / 2);
    int y = (desktopRectangle.bottom / 2) - (height / 2);

    // Create the window
    HWND window = CreateWindowExA(0,
        "CoreEngineClass",
        title,
        WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        x,
        y,
        width,
        height,
        0,
        0,
        applicationInstance,
        0);

    if (windowState == NativeWindowState::Maximized)
    {
        ShowWindow(window, SW_MAXIMIZE);
    }

    return window;
}

struct Vector2 WindowsNativeUIService::GetWindowRenderSize(void* windowPointer)
{
    RECT windowRectangle;
	GetClientRect((HWND)windowPointer, &windowRectangle);

    auto renderSize = Vector2();
    renderSize.X = windowRectangle.right - windowRectangle.left;
    renderSize.Y = windowRectangle.bottom - windowRectangle.top;

    return renderSize;
}

struct NativeAppStatus WindowsNativeUIService::ProcessSystemMessages()
{
    auto result = Win32ProcessPendingMessages();

    auto status = NativeAppStatus();
    status.IsActive = isAppActive;
    status.IsRunning = result;

    return status;
}
#pragma once
#include "../WindowsNativeUIService.h"

void* WindowsNativeUIServiceCreateWindowInterop(void* context, char* title, int width, int height, enum NativeWindowState windowState)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->CreateWindow(title, width, height, windowState);
}

void WindowsNativeUIServiceSetWindowTitleInterop(void* context, void* windowPointer, char* title)
{
    auto contextObject = (WindowsNativeUIService*)context;
    contextObject->SetWindowTitle(windowPointer, title);
}

struct Vector2 WindowsNativeUIServiceGetWindowRenderSizeInterop(void* context, void* windowPointer)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->GetWindowRenderSize(windowPointer);
}

struct NativeAppStatus WindowsNativeUIServiceProcessSystemMessagesInterop(void* context)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->ProcessSystemMessages();
}

void InitWindowsNativeUIService(const WindowsNativeUIService* context, NativeUIService* service)
{
    service->Context = (void*)context;
    service->NativeUIService_CreateWindow = WindowsNativeUIServiceCreateWindowInterop;
    service->NativeUIService_SetWindowTitle = WindowsNativeUIServiceSetWindowTitleInterop;
    service->NativeUIService_GetWindowRenderSize = WindowsNativeUIServiceGetWindowRenderSizeInterop;
    service->NativeUIService_ProcessSystemMessages = WindowsNativeUIServiceProcessSystemMessagesInterop;
}

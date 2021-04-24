#pragma once
#include "../WindowsNativeUIService.h"

void* CreateWindowInterop(void* context, char* title, int width, int height, enum NativeWindowState windowState)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->CreateWindow(title, width, height, windowState);
}

void SetWindowTitleInterop(void* context, void* windowPointer, char* title)
{
    auto contextObject = (WindowsNativeUIService*)context;
    contextObject->SetWindowTitle(windowPointer, title);
}

struct Vector2 GetWindowRenderSizeInterop(void* context, void* windowPointer)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->GetWindowRenderSize(windowPointer);
}

struct NativeAppStatus ProcessSystemMessagesInterop(void* context)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->ProcessSystemMessages();
}

void InitNativeUIService(const WindowsNativeUIService& context, NativeUIService* service)
{
    service->Context = (void*)&context;
    service->NativeUIService_CreateWindow = CreateWindowInterop;
    service->NativeUIService_SetWindowTitle = SetWindowTitleInterop;
    service->NativeUIService_GetWindowRenderSize = GetWindowRenderSizeInterop;
    service->NativeUIService_ProcessSystemMessages = ProcessSystemMessagesInterop;
}

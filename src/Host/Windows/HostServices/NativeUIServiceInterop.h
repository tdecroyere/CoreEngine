#pragma once
#include "../WindowsNativeUIService.h"

void* CreateWindowInterop(void* context, char* title, int width, int height)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->CreateWindow(title, width, height);
}

struct Vector2 GetWindowRenderSizeInterop(void* context, void* windowPointer)
{
    auto contextObject = (WindowsNativeUIService*)context;
    return contextObject->GetWindowRenderSize(windowPointer);
}

void InitNativeUIService(const WindowsNativeUIService& context, NativeUIService* service)
{
    service->Context = (void*)&context;
    service->NativeUIService_CreateWindow = CreateWindowInterop;
    service->NativeUIService_GetWindowRenderSize = GetWindowRenderSizeInterop;
}

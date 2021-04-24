#pragma once
#include "CoreEngine.h"

enum NativeWindowState : int
{
    Normal, 
    Maximized
};

struct NativeAppStatus
{
    int IsRunning;
    int IsActive;
};

struct NullableNativeAppStatus
{
    int HasValue;
    struct NativeAppStatus Value;
};

typedef void* (*NativeUIService_CreateWindowPtr)(void* context, char* title, int width, int height, enum NativeWindowState windowState);
typedef void (*NativeUIService_SetWindowTitlePtr)(void* context, void* windowPointer, char* title);
typedef struct Vector2 (*NativeUIService_GetWindowRenderSizePtr)(void* context, void* windowPointer);
typedef struct NativeAppStatus (*NativeUIService_ProcessSystemMessagesPtr)(void* context);

struct NativeUIService
{
    void* Context;
    NativeUIService_CreateWindowPtr NativeUIService_CreateWindow;
    NativeUIService_SetWindowTitlePtr NativeUIService_SetWindowTitle;
    NativeUIService_GetWindowRenderSizePtr NativeUIService_GetWindowRenderSize;
    NativeUIService_ProcessSystemMessagesPtr NativeUIService_ProcessSystemMessages;
};

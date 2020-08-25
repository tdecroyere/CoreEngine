#pragma once
#include "CoreEngine.h"

typedef void* (*NativeUIService_CreateWindowPtr)(void* context, char* title, int width, int height);
typedef struct Vector2 (*NativeUIService_GetWindowRenderSizePtr)(void* context, void* windowPointer);

struct NativeUIService
{
    void* Context;
    NativeUIService_CreateWindowPtr NativeUIService_CreateWindow;
    NativeUIService_GetWindowRenderSizePtr NativeUIService_GetWindowRenderSize;
};

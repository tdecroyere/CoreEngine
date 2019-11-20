#pragma once
#include "CoreEngine.h"

typedef struct Vector2 (*GetRenderSizePtr)(void* context);
typedef unsigned int (*CreateShaderPtr)(void* context, void* shaderByteCode, int shaderByteCodeLength);
typedef unsigned int (*CreateShaderParametersPtr)(void* context, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3);
typedef unsigned int (*CreateStaticGraphicsBufferPtr)(void* context, void* data, int dataLength);
typedef unsigned int (*CreateDynamicGraphicsBufferPtr)(void* context, int length);
typedef void (*UploadDataToGraphicsBufferPtr)(void* context, unsigned int graphicsBufferId, void* data, int dataLength);
typedef void (*BeginCopyGpuDataPtr)(void* context);
typedef void (*EndCopyGpuDataPtr)(void* context);
typedef void (*BeginRenderPtr)(void* context);
typedef void (*EndRenderPtr)(void* context);
typedef void (*DrawPrimitivesPtr)(void* context, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId);

struct GraphicsService
{
    void* Context;
    GetRenderSizePtr GetRenderSize;
    CreateShaderPtr CreateShader;
    CreateShaderParametersPtr CreateShaderParameters;
    CreateStaticGraphicsBufferPtr CreateStaticGraphicsBuffer;
    CreateDynamicGraphicsBufferPtr CreateDynamicGraphicsBuffer;
    UploadDataToGraphicsBufferPtr UploadDataToGraphicsBuffer;
    BeginCopyGpuDataPtr BeginCopyGpuData;
    EndCopyGpuDataPtr EndCopyGpuData;
    BeginRenderPtr BeginRender;
    EndRenderPtr EndRender;
    DrawPrimitivesPtr DrawPrimitives;
};

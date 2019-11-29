#pragma once
#include "WindowsDirect3D12Renderer.h"
#include "../../Common/CoreEngine.h"

struct Vector2 GetRenderSizeInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->GetRenderSize()
}

unsigned int CreateShaderInterop(void* context, void* shaderByteCode, int shaderByteCodeLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateShader(shaderByteCode, shaderByteCodeLength)
}

unsigned int CreateShaderParametersInterop(void* context, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateShaderParameters(graphicsBuffer1, graphicsBuffer2, graphicsBuffer3)
}

unsigned int CreateGraphicsBufferInterop(void* context, int length)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateGraphicsBuffer(length)
}

unsigned int CreateCopyCommandListInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateCopyCommandList()
}

void ExecuteCopyCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteCopyCommandList(commandListId)
}

void UploadDataToGraphicsBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->UploadDataToGraphicsBuffer(commandListId, graphicsBufferId, data, dataLength)
}

unsigned int CreateRenderCommandListInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateRenderCommandList()
}

void ExecuteRenderCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteRenderCommandList(commandListId)
}

void DrawPrimitivesInterop(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DrawPrimitives(commandListId, primitiveType, startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId)
}

void InitGraphicsService(WindowsDirect3D12Renderer* context, GraphicsService* service)
{
    service->Context = context;
    service->GetRenderSize = GetRenderSizeInterop;
    service->CreateShader = CreateShaderInterop;
    service->CreateShaderParameters = CreateShaderParametersInterop;
    service->CreateGraphicsBuffer = CreateGraphicsBufferInterop;
    service->CreateCopyCommandList = CreateCopyCommandListInterop;
    service->ExecuteCopyCommandList = ExecuteCopyCommandListInterop;
    service->UploadDataToGraphicsBuffer = UploadDataToGraphicsBufferInterop;
    service->CreateRenderCommandList = CreateRenderCommandListInterop;
    service->ExecuteRenderCommandList = ExecuteRenderCommandListInterop;
    service->DrawPrimitives = DrawPrimitivesInterop;
}

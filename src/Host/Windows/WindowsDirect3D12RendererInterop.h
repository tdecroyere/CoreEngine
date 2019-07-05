#pragma once

#include "WindowsDirect3D12Renderer.h"
#include "../Common/CoreEngine.h"

Vector2 GetRenderSizeHandle(void* graphicsContext)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->GetRenderSize();
}

unsigned int CreateShaderHandle(void* graphicsContext, ::MemoryBuffer shaderByteCode)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->CreateShader(shaderByteCode);
}

unsigned int CreateGraphicsBufferHandle(void* graphicsContext, ::MemoryBuffer data)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->CreateGraphicsBuffer(data);
}

unsigned int CreateShaderParametersHandle(void* graphicsContext, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->CreateShaderParameters(graphicsBuffer1, graphicsBuffer2, graphicsBuffer3);
}

void UploadDataToGraphicsBufferHandle(void* graphicsContext, unsigned int graphicsBufferId, ::MemoryBuffer data)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->UploadDataToGraphicsBuffer(graphicsBufferId, data);
}

void DrawPrimitivesHandle(void* graphicsContext, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, int objectPropertyIndex)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->DrawPrimitives(startIndex, indexCount, vertexBufferId, indexBufferId, objectPropertyIndex);
}
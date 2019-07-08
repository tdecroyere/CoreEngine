#pragma once

#include "WindowsDirect3D12Renderer.h"
#include "../Common/CoreEngine.h"

Vector2 GetRenderSizeHandle(void* graphicsContext)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->GetRenderSize();
}

unsigned int CreateShaderHandle(void* graphicsContext, HostMemoryBuffer shaderByteCode)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->CreateShader(shaderByteCode);
}

unsigned int CreateShaderParametersHandle(void* graphicsContext, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->CreateShaderParameters(graphicsBuffer1, graphicsBuffer2, graphicsBuffer3);
}

unsigned int CreateStaticGraphicsBufferHandle(void* graphicsContext, HostMemoryBuffer data)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;    
    return renderer->CreateStaticGraphicsBuffer(data);
}

HostMemoryBuffer CreateDynamicGraphicsBufferHandle(void* graphicsContext, unsigned int length)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;    
    return renderer->CreateDynamicGraphicsBuffer(length);
}

void UploadDataToGraphicsBufferHandle(void* graphicsContext, unsigned int graphicsBufferId, HostMemoryBuffer data)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->UploadDataToGraphicsBuffer(graphicsBufferId, data);
}

void BeginCopyGpuDataHandle(void* graphicsContext)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->BeginCopyGpuData();
}

void EndCopyGpuDataHandle(void* graphicsContext)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->EndCopyGpuData();
}

void BeginRenderHandle(void* graphicsContext)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->BeginRender();
}

void EndRenderHandle(void* graphicsContext)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->EndRender();
}

void DrawPrimitivesHandle(void* graphicsContext, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId)
{
    auto renderer = (WindowsDirect3D12Renderer*)graphicsContext;
    return renderer->DrawPrimitives(startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId);
}

void InitGraphicsService(WindowsDirect3D12Renderer* renderer, GraphicsService* graphicsService) 
{
	graphicsService->GraphicsContext = renderer;
	graphicsService->GetRenderSize = GetRenderSizeHandle;
	graphicsService->CreateShader = CreateShaderHandle;
	graphicsService->CreateShaderParameters = CreateShaderParametersHandle;
    graphicsService->CreateStaticGraphicsBuffer = CreateStaticGraphicsBufferHandle;
    graphicsService->CreateDynamicGraphicsBuffer = CreateDynamicGraphicsBufferHandle;
	graphicsService->UploadDataToGraphicsBuffer = UploadDataToGraphicsBufferHandle;
    graphicsService->BeginCopyGpuData = BeginCopyGpuDataHandle;
    graphicsService->EndCopyGpuData = EndCopyGpuDataHandle;
    graphicsService->BeginRender = BeginRenderHandle;
    graphicsService->EndRender = EndRenderHandle;
	graphicsService->DrawPrimitives = DrawPrimitivesHandle;
}
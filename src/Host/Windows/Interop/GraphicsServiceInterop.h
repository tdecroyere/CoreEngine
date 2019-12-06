#pragma once
#include "WindowsDirect3D12Renderer.h"
#include "../../Common/CoreEngine.h"

struct Vector2 GetRenderSizeInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->GetRenderSize()
}

unsigned int CreatePipelineStateInterop(void* context, void* shaderByteCode, int shaderByteCodeLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreatePipelineState(shaderByteCode, shaderByteCodeLength)
}

void RemovePipelineStateInterop(void* context, unsigned int pipelineStateId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->RemovePipelineState(pipelineStateId)
}

unsigned int CreateShaderParametersInterop(void* context, unsigned int pipelineStateId, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateShaderParameters(pipelineStateId, graphicsBuffer1, graphicsBuffer2, graphicsBuffer3)
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

void SetPipelineStateInterop(void* context, unsigned int commandListId, unsigned int pipelineStateId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetPipelineState(commandListId, pipelineStateId)
}

void SetGraphicsBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, enum GraphicsBindStage graphicsBindStage, unsigned int slot)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetGraphicsBuffer(commandListId, graphicsBufferId, graphicsBindStage, slot)
}

void DrawPrimitivesInterop(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DrawPrimitives(commandListId, primitiveType, startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId)
}

void PresentScreenBufferInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->PresentScreenBuffer()
}

void InitGraphicsService(WindowsDirect3D12Renderer* context, GraphicsService* service)
{
    service->Context = context;
    service->GetRenderSize = GetRenderSizeInterop;
    service->CreatePipelineState = CreatePipelineStateInterop;
    service->RemovePipelineState = RemovePipelineStateInterop;
    service->CreateShaderParameters = CreateShaderParametersInterop;
    service->CreateGraphicsBuffer = CreateGraphicsBufferInterop;
    service->CreateCopyCommandList = CreateCopyCommandListInterop;
    service->ExecuteCopyCommandList = ExecuteCopyCommandListInterop;
    service->UploadDataToGraphicsBuffer = UploadDataToGraphicsBufferInterop;
    service->CreateRenderCommandList = CreateRenderCommandListInterop;
    service->ExecuteRenderCommandList = ExecuteRenderCommandListInterop;
    service->SetPipelineState = SetPipelineStateInterop;
    service->SetGraphicsBuffer = SetGraphicsBufferInterop;
    service->DrawPrimitives = DrawPrimitivesInterop;
    service->PresentScreenBuffer = PresentScreenBufferInterop;
}

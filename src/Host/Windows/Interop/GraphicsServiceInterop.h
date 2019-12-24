#pragma once
#include "WindowsDirect3D12Renderer.h"
#include "../../Common/CoreEngine.h"

struct Vector2 GetRenderSizeInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->GetRenderSize()
}

int CreateGraphicsBufferInterop(void* context, unsigned int graphicsBufferId, int length, struct string? debugName)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateGraphicsBuffer(graphicsBufferId, length, debugName)
}

int CreateTextureInterop(void* context, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int mipLevels, int isRenderTarget, struct string? debugName)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateTexture(textureId, textureFormat, width, height, mipLevels, isRenderTarget, debugName)
}

void RemoveTextureInterop(void* context, unsigned int textureId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->RemoveTexture(textureId)
}

int CreateShaderInterop(void* context, unsigned int shaderId, struct string? computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, int useDepthBuffer, struct string? debugName)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateShader(shaderId, computeShaderFunction, shaderByteCode, shaderByteCodeLength, useDepthBuffer, debugName)
}

void RemoveShaderInterop(void* context, unsigned int shaderId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->RemoveShader(shaderId)
}

int CreateCopyCommandListInterop(void* context, unsigned int commandListId, struct string? debugName, int createNewCommandBuffer)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateCopyCommandList(commandListId, debugName, createNewCommandBuffer)
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

void UploadDataToTextureInterop(void* context, unsigned int commandListId, unsigned int textureId, int width, int height, int mipLevel, void* data, int dataLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->UploadDataToTexture(commandListId, textureId, width, height, mipLevel, data, dataLength)
}

void ResetIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ResetIndirectCommandList(commandListId, indirectCommandListId, maxCommandCount)
}

void OptimizeIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->OptimizeIndirectCommandList(commandListId, indirectCommandListId, maxCommandCount)
}

int CreateComputeCommandListInterop(void* context, unsigned int commandListId, struct string? debugName, int createNewCommandBuffer)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateComputeCommandList(commandListId, debugName, createNewCommandBuffer)
}

void ExecuteComputeCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteComputeCommandList(commandListId)
}

void DispatchThreadsInterop(void* context, unsigned int commandListId, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DispatchThreads(commandListId, threadGroupCountX, threadGroupCountY, threadGroupCountZ)
}

int CreateRenderCommandListInterop(void* context, unsigned int commandListId, struct GraphicsRenderPassDescriptor renderDescriptor, struct string? debugName, int createNewCommandBuffer)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateRenderCommandList(commandListId, renderDescriptor, debugName, createNewCommandBuffer)
}

void ExecuteRenderCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteRenderCommandList(commandListId)
}

int CreateIndirectCommandListInterop(void* context, unsigned int commandListId, int maxCommandCount, struct string? debugName)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateIndirectCommandList(commandListId, maxCommandCount, debugName)
}

void SetShaderInterop(void* context, unsigned int commandListId, unsigned int shaderId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShader(commandListId, shaderId)
}

void SetShaderBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderBuffer(commandListId, graphicsBufferId, slot, index)
}

void SetShaderBuffersInterop(void* context, unsigned int commandListId, struct ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderBuffers(commandListId, graphicsBufferIdList, slot, index)
}

void SetShaderTextureInterop(void* context, unsigned int commandListId, unsigned int textureId, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderTexture(commandListId, textureId, slot, index)
}

void SetShaderTexturesInterop(void* context, unsigned int commandListId, struct ReadOnlySpan<uint> textureIdList, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderTextures(commandListId, textureIdList, slot, index)
}

void SetShaderIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderIndirectCommandList(commandListId, indirectCommandListId, slot, index)
}

void ExecuteIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteIndirectCommandList(commandListId, indirectCommandListId, maxCommandCount)
}

void SetIndexBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetIndexBuffer(commandListId, graphicsBufferId)
}

void DrawIndexedPrimitivesInterop(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DrawIndexedPrimitives(commandListId, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId)
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
    service->CreateGraphicsBuffer = CreateGraphicsBufferInterop;
    service->CreateTexture = CreateTextureInterop;
    service->RemoveTexture = RemoveTextureInterop;
    service->CreateShader = CreateShaderInterop;
    service->RemoveShader = RemoveShaderInterop;
    service->CreateCopyCommandList = CreateCopyCommandListInterop;
    service->ExecuteCopyCommandList = ExecuteCopyCommandListInterop;
    service->UploadDataToGraphicsBuffer = UploadDataToGraphicsBufferInterop;
    service->UploadDataToTexture = UploadDataToTextureInterop;
    service->ResetIndirectCommandList = ResetIndirectCommandListInterop;
    service->OptimizeIndirectCommandList = OptimizeIndirectCommandListInterop;
    service->CreateComputeCommandList = CreateComputeCommandListInterop;
    service->ExecuteComputeCommandList = ExecuteComputeCommandListInterop;
    service->DispatchThreads = DispatchThreadsInterop;
    service->CreateRenderCommandList = CreateRenderCommandListInterop;
    service->ExecuteRenderCommandList = ExecuteRenderCommandListInterop;
    service->CreateIndirectCommandList = CreateIndirectCommandListInterop;
    service->SetShader = SetShaderInterop;
    service->SetShaderBuffer = SetShaderBufferInterop;
    service->SetShaderBuffers = SetShaderBuffersInterop;
    service->SetShaderTexture = SetShaderTextureInterop;
    service->SetShaderTextures = SetShaderTexturesInterop;
    service->SetShaderIndirectCommandList = SetShaderIndirectCommandListInterop;
    service->ExecuteIndirectCommandList = ExecuteIndirectCommandListInterop;
    service->SetIndexBuffer = SetIndexBufferInterop;
    service->DrawIndexedPrimitives = DrawIndexedPrimitivesInterop;
    service->PresentScreenBuffer = PresentScreenBufferInterop;
}

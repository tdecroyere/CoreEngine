#pragma once
#include "WindowsDirect3D12Renderer.h"
#include "../../Common/CoreEngine.h"

struct Vector2 GetRenderSizeInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->GetRenderSize()
}

struct string GetGraphicsAdapterNameInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->GetGraphicsAdapterName()
}

int CreateGraphicsBufferInterop(void* context, unsigned int graphicsBufferId, int length, int isWriteOnly, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateGraphicsBuffer(graphicsBufferId, length, isWriteOnly, label)
}

int CreateTextureInterop(void* context, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateTexture(textureId, textureFormat, width, height, faceCount, mipLevels, multisampleCount, isRenderTarget, label)
}

void DeleteTextureInterop(void* context, unsigned int textureId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DeleteTexture(textureId)
}

int CreateIndirectCommandBufferInterop(void* context, unsigned int indirectCommandBufferId, int maxCommandCount, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateIndirectCommandBuffer(indirectCommandBufferId, maxCommandCount, label)
}

int CreateShaderInterop(void* context, unsigned int shaderId, struct string? computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateShader(shaderId, computeShaderFunction, shaderByteCode, shaderByteCodeLength, label)
}

void DeleteShaderInterop(void* context, unsigned int shaderId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DeleteShader(shaderId)
}

int CreatePipelineStateInterop(void* context, unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreatePipelineState(pipelineStateId, shaderId, renderPassDescriptor, label)
}

void DeletePipelineStateInterop(void* context, unsigned int pipelineStateId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DeletePipelineState(pipelineStateId)
}

int CreateCommandBufferInterop(void* context, unsigned int commandBufferId, enum GraphicsCommandBufferType commandBufferType, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateCommandBuffer(commandBufferId, commandBufferType, label)
}

void DeleteCommandBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DeleteCommandBuffer(commandBufferId)
}

void ResetCommandBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ResetCommandBuffer(commandBufferId)
}

void ExecuteCommandBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteCommandBuffer(commandBufferId)
}

struct GraphicsCommandBufferStatus? GetCommandBufferStatusInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->GetCommandBufferStatus(commandBufferId)
}

void SetShaderBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderBuffer(commandListId, graphicsBufferId, slot, isReadOnly, index)
}

void SetShaderBuffersInterop(void* context, unsigned int commandListId, struct ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderBuffers(commandListId, graphicsBufferIdList, slot, index)
}

void SetShaderTextureInterop(void* context, unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderTexture(commandListId, textureId, slot, isReadOnly, index)
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

void SetShaderIndirectCommandListsInterop(void* context, unsigned int commandListId, struct ReadOnlySpan<uint> indirectCommandListIdList, int slot, int index)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShaderIndirectCommandLists(commandListId, indirectCommandListIdList, slot, index)
}

int CreateCopyCommandListInterop(void* context, unsigned int commandListId, unsigned int commandBufferId, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateCopyCommandList(commandListId, commandBufferId, label)
}

void CommitCopyCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->CommitCopyCommandList(commandListId)
}

void UploadDataToGraphicsBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->UploadDataToGraphicsBuffer(commandListId, graphicsBufferId, data, dataLength)
}

void CopyGraphicsBufferDataToCpuInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int length)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->CopyGraphicsBufferDataToCpu(commandListId, graphicsBufferId, length)
}

void ReadGraphicsBufferDataInterop(void* context, unsigned int graphicsBufferId, void* data, int dataLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ReadGraphicsBufferData(graphicsBufferId, data, dataLength)
}

void UploadDataToTextureInterop(void* context, unsigned int commandListId, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, void* data, int dataLength)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->UploadDataToTexture(commandListId, textureId, textureFormat, width, height, slice, mipLevel, data, dataLength)
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

int CreateComputeCommandListInterop(void* context, unsigned int commandListId, unsigned int commandBufferId, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateComputeCommandList(commandListId, commandBufferId, label)
}

void CommitComputeCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->CommitComputeCommandList(commandListId)
}

struct Vector3 DispatchThreadsInterop(void* context, unsigned int commandListId, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->DispatchThreads(commandListId, threadCountX, threadCountY, threadCountZ)
}

int CreateRenderCommandListInterop(void* context, unsigned int commandListId, unsigned int commandBufferId, struct GraphicsRenderPassDescriptor renderDescriptor, struct string label)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    return contextObject->CreateRenderCommandList(commandListId, commandBufferId, renderDescriptor, label)
}

void CommitRenderCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->CommitRenderCommandList(commandListId)
}

void SetPipelineStateInterop(void* context, unsigned int commandListId, unsigned int pipelineStateId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetPipelineState(commandListId, pipelineStateId)
}

void SetShaderInterop(void* context, unsigned int commandListId, unsigned int shaderId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->SetShader(commandListId, shaderId)
}

void ExecuteIndirectCommandBufferInterop(void* context, unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->ExecuteIndirectCommandBuffer(commandListId, indirectCommandBufferId, maxCommandCount)
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

void DrawPrimitivesInterop(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->DrawPrimitives(commandListId, primitiveType, startVertex, vertexCount)
}

void WaitForCommandListInterop(void* context, unsigned int commandListId, unsigned int commandListToWaitId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->WaitForCommandList(commandListId, commandListToWaitId)
}

void PresentScreenBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->PresentScreenBuffer(commandBufferId)
}

void WaitForAvailableScreenBufferInterop(void* context)
{
    auto contextObject = (WindowsDirect3D12Renderer*)context;
    contextObject->WaitForAvailableScreenBuffer()
}

void InitGraphicsService(WindowsDirect3D12Renderer* context, GraphicsService* service)
{
    service->Context = context;
    service->GetRenderSize = GetRenderSizeInterop;
    service->GetGraphicsAdapterName = GetGraphicsAdapterNameInterop;
    service->CreateGraphicsBuffer = CreateGraphicsBufferInterop;
    service->CreateTexture = CreateTextureInterop;
    service->DeleteTexture = DeleteTextureInterop;
    service->CreateIndirectCommandBuffer = CreateIndirectCommandBufferInterop;
    service->CreateShader = CreateShaderInterop;
    service->DeleteShader = DeleteShaderInterop;
    service->CreatePipelineState = CreatePipelineStateInterop;
    service->DeletePipelineState = DeletePipelineStateInterop;
    service->CreateCommandBuffer = CreateCommandBufferInterop;
    service->DeleteCommandBuffer = DeleteCommandBufferInterop;
    service->ResetCommandBuffer = ResetCommandBufferInterop;
    service->ExecuteCommandBuffer = ExecuteCommandBufferInterop;
    service->GetCommandBufferStatus = GetCommandBufferStatusInterop;
    service->SetShaderBuffer = SetShaderBufferInterop;
    service->SetShaderBuffers = SetShaderBuffersInterop;
    service->SetShaderTexture = SetShaderTextureInterop;
    service->SetShaderTextures = SetShaderTexturesInterop;
    service->SetShaderIndirectCommandList = SetShaderIndirectCommandListInterop;
    service->SetShaderIndirectCommandLists = SetShaderIndirectCommandListsInterop;
    service->CreateCopyCommandList = CreateCopyCommandListInterop;
    service->CommitCopyCommandList = CommitCopyCommandListInterop;
    service->UploadDataToGraphicsBuffer = UploadDataToGraphicsBufferInterop;
    service->CopyGraphicsBufferDataToCpu = CopyGraphicsBufferDataToCpuInterop;
    service->ReadGraphicsBufferData = ReadGraphicsBufferDataInterop;
    service->UploadDataToTexture = UploadDataToTextureInterop;
    service->ResetIndirectCommandList = ResetIndirectCommandListInterop;
    service->OptimizeIndirectCommandList = OptimizeIndirectCommandListInterop;
    service->CreateComputeCommandList = CreateComputeCommandListInterop;
    service->CommitComputeCommandList = CommitComputeCommandListInterop;
    service->DispatchThreads = DispatchThreadsInterop;
    service->CreateRenderCommandList = CreateRenderCommandListInterop;
    service->CommitRenderCommandList = CommitRenderCommandListInterop;
    service->SetPipelineState = SetPipelineStateInterop;
    service->SetShader = SetShaderInterop;
    service->ExecuteIndirectCommandBuffer = ExecuteIndirectCommandBufferInterop;
    service->SetIndexBuffer = SetIndexBufferInterop;
    service->DrawIndexedPrimitives = DrawIndexedPrimitivesInterop;
    service->DrawPrimitives = DrawPrimitivesInterop;
    service->WaitForCommandList = WaitForCommandListInterop;
    service->PresentScreenBuffer = PresentScreenBufferInterop;
    service->WaitForAvailableScreenBuffer = WaitForAvailableScreenBufferInterop;
}

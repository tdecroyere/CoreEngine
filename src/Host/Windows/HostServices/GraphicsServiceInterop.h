#pragma once
#include "../Direct3D12GraphicsService.h"

void GetGraphicsAdapterNameInterop(void* context, char* output)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->GetGraphicsAdapterName(output);
}

struct Vector2 GetRenderSizeInterop(void* context)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetRenderSize();
}

struct GraphicsAllocationInfos GetTextureAllocationInfosInterop(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetTextureAllocationInfos(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

int CreateGraphicsHeapInterop(void* context, unsigned int graphicsHeapId, enum GraphicsServiceHeapType type, unsigned long length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsHeap(graphicsHeapId, type, length);
}

void SetGraphicsHeapLabelInterop(void* context, unsigned int graphicsHeapId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetGraphicsHeapLabel(graphicsHeapId, label);
}

void DeleteGraphicsHeapInterop(void* context, unsigned int graphicsHeapId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteGraphicsHeap(graphicsHeapId);
}

int CreateGraphicsBufferInterop(void* context, unsigned int graphicsBufferId, unsigned int graphicsHeapId, unsigned long heapOffset, int isAliasable, int sizeInBytes)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsBuffer(graphicsBufferId, graphicsHeapId, heapOffset, isAliasable, sizeInBytes);
}

void SetGraphicsBufferLabelInterop(void* context, unsigned int graphicsBufferId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetGraphicsBufferLabel(graphicsBufferId, label);
}

void DeleteGraphicsBufferInterop(void* context, unsigned int graphicsBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteGraphicsBuffer(graphicsBufferId);
}

void* GetGraphicsBufferCpuPointerInterop(void* context, unsigned int graphicsBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetGraphicsBufferCpuPointer(graphicsBufferId);
}

int CreateTextureInterop(void* context, unsigned int textureId, unsigned int graphicsHeapId, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateTexture(textureId, graphicsHeapId, heapOffset, isAliasable, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void SetTextureLabelInterop(void* context, unsigned int textureId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetTextureLabel(textureId, label);
}

void DeleteTextureInterop(void* context, unsigned int textureId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteTexture(textureId);
}

int CreateIndirectCommandBufferInterop(void* context, unsigned int indirectCommandBufferId, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateIndirectCommandBuffer(indirectCommandBufferId, maxCommandCount);
}

void SetIndirectCommandBufferLabelInterop(void* context, unsigned int indirectCommandBufferId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetIndirectCommandBufferLabel(indirectCommandBufferId, label);
}

void DeleteIndirectCommandBufferInterop(void* context, unsigned int indirectCommandBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteIndirectCommandBuffer(indirectCommandBufferId);
}

int CreateShaderInterop(void* context, unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateShader(shaderId, computeShaderFunction, shaderByteCode, shaderByteCodeLength);
}

void SetShaderLabelInterop(void* context, unsigned int shaderId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderLabel(shaderId, label);
}

void DeleteShaderInterop(void* context, unsigned int shaderId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShader(shaderId);
}

int CreatePipelineStateInterop(void* context, unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreatePipelineState(pipelineStateId, shaderId, renderPassDescriptor);
}

void SetPipelineStateLabelInterop(void* context, unsigned int pipelineStateId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetPipelineStateLabel(pipelineStateId, label);
}

void DeletePipelineStateInterop(void* context, unsigned int pipelineStateId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeletePipelineState(pipelineStateId);
}

int CreateCommandBufferInterop(void* context, unsigned int commandBufferId, enum GraphicsCommandBufferType commandBufferType, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCommandBuffer(commandBufferId, commandBufferType, label);
}

void DeleteCommandBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteCommandBuffer(commandBufferId);
}

void ResetCommandBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetCommandBuffer(commandBufferId);
}

void ExecuteCommandBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ExecuteCommandBuffer(commandBufferId);
}

NullableGraphicsCommandBufferStatus GetCommandBufferStatusInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetCommandBufferStatus(commandBufferId);
}

void SetShaderBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderBuffer(commandListId, graphicsBufferId, slot, isReadOnly, index);
}

void SetShaderBuffersInterop(void* context, unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderBuffers(commandListId, graphicsBufferIdList, graphicsBufferIdListLength, slot, index);
}

void SetShaderTextureInterop(void* context, unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderTexture(commandListId, textureId, slot, isReadOnly, index);
}

void SetShaderTexturesInterop(void* context, unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderTextures(commandListId, textureIdList, textureIdListLength, slot, index);
}

void SetShaderIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderIndirectCommandList(commandListId, indirectCommandListId, slot, index);
}

void SetShaderIndirectCommandListsInterop(void* context, unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderIndirectCommandLists(commandListId, indirectCommandListIdList, indirectCommandListIdListLength, slot, index);
}

int CreateCopyCommandListInterop(void* context, unsigned int commandListId, unsigned int commandBufferId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCopyCommandList(commandListId, commandBufferId, label);
}

void CommitCopyCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CommitCopyCommandList(commandListId);
}

void CopyDataToGraphicsBufferInterop(void* context, unsigned int commandListId, unsigned int destinationGraphicsBufferId, unsigned int sourceGraphicsBufferId, int length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyDataToGraphicsBuffer(commandListId, destinationGraphicsBufferId, sourceGraphicsBufferId, length);
}

void CopyDataToTextureInterop(void* context, unsigned int commandListId, unsigned int destinationTextureId, unsigned int sourceGraphicsBufferId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyDataToTexture(commandListId, destinationTextureId, sourceGraphicsBufferId, textureFormat, width, height, slice, mipLevel);
}

void CopyTextureInterop(void* context, unsigned int commandListId, unsigned int destinationTextureId, unsigned int sourceTextureId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyTexture(commandListId, destinationTextureId, sourceTextureId);
}

void ResetIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetIndirectCommandList(commandListId, indirectCommandListId, maxCommandCount);
}

void OptimizeIndirectCommandListInterop(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->OptimizeIndirectCommandList(commandListId, indirectCommandListId, maxCommandCount);
}

int CreateComputeCommandListInterop(void* context, unsigned int commandListId, unsigned int commandBufferId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateComputeCommandList(commandListId, commandBufferId, label);
}

void CommitComputeCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CommitComputeCommandList(commandListId);
}

struct Vector3 DispatchThreadsInterop(void* context, unsigned int commandListId, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->DispatchThreads(commandListId, threadCountX, threadCountY, threadCountZ);
}

int CreateRenderCommandListInterop(void* context, unsigned int commandListId, unsigned int commandBufferId, struct GraphicsRenderPassDescriptor renderDescriptor, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateRenderCommandList(commandListId, commandBufferId, renderDescriptor, label);
}

void CommitRenderCommandListInterop(void* context, unsigned int commandListId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CommitRenderCommandList(commandListId);
}

void SetPipelineStateInterop(void* context, unsigned int commandListId, unsigned int pipelineStateId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetPipelineState(commandListId, pipelineStateId);
}

void SetShaderInterop(void* context, unsigned int commandListId, unsigned int shaderId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShader(commandListId, shaderId);
}

void ExecuteIndirectCommandBufferInterop(void* context, unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ExecuteIndirectCommandBuffer(commandListId, indirectCommandBufferId, maxCommandCount);
}

void SetIndexBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetIndexBuffer(commandListId, graphicsBufferId);
}

void DrawIndexedPrimitivesInterop(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DrawIndexedPrimitives(commandListId, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId);
}

void DrawPrimitivesInterop(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DrawPrimitives(commandListId, primitiveType, startVertex, vertexCount);
}

void WaitForCommandListInterop(void* context, unsigned int commandListId, unsigned int commandListToWaitId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForCommandList(commandListId, commandListToWaitId);
}

void PresentScreenBufferInterop(void* context, unsigned int commandBufferId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->PresentScreenBuffer(commandBufferId);
}

void WaitForAvailableScreenBufferInterop(void* context)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForAvailableScreenBuffer();
}

void InitGraphicsService(const Direct3D12GraphicsService& context, GraphicsService* service)
{
    service->Context = (void*)&context;
    service->GraphicsService_GetGraphicsAdapterName = GetGraphicsAdapterNameInterop;
    service->GraphicsService_GetRenderSize = GetRenderSizeInterop;
    service->GraphicsService_GetTextureAllocationInfos = GetTextureAllocationInfosInterop;
    service->GraphicsService_CreateGraphicsHeap = CreateGraphicsHeapInterop;
    service->GraphicsService_SetGraphicsHeapLabel = SetGraphicsHeapLabelInterop;
    service->GraphicsService_DeleteGraphicsHeap = DeleteGraphicsHeapInterop;
    service->GraphicsService_CreateGraphicsBuffer = CreateGraphicsBufferInterop;
    service->GraphicsService_SetGraphicsBufferLabel = SetGraphicsBufferLabelInterop;
    service->GraphicsService_DeleteGraphicsBuffer = DeleteGraphicsBufferInterop;
    service->GraphicsService_GetGraphicsBufferCpuPointer = GetGraphicsBufferCpuPointerInterop;
    service->GraphicsService_CreateTexture = CreateTextureInterop;
    service->GraphicsService_SetTextureLabel = SetTextureLabelInterop;
    service->GraphicsService_DeleteTexture = DeleteTextureInterop;
    service->GraphicsService_CreateIndirectCommandBuffer = CreateIndirectCommandBufferInterop;
    service->GraphicsService_SetIndirectCommandBufferLabel = SetIndirectCommandBufferLabelInterop;
    service->GraphicsService_DeleteIndirectCommandBuffer = DeleteIndirectCommandBufferInterop;
    service->GraphicsService_CreateShader = CreateShaderInterop;
    service->GraphicsService_SetShaderLabel = SetShaderLabelInterop;
    service->GraphicsService_DeleteShader = DeleteShaderInterop;
    service->GraphicsService_CreatePipelineState = CreatePipelineStateInterop;
    service->GraphicsService_SetPipelineStateLabel = SetPipelineStateLabelInterop;
    service->GraphicsService_DeletePipelineState = DeletePipelineStateInterop;
    service->GraphicsService_CreateCommandBuffer = CreateCommandBufferInterop;
    service->GraphicsService_DeleteCommandBuffer = DeleteCommandBufferInterop;
    service->GraphicsService_ResetCommandBuffer = ResetCommandBufferInterop;
    service->GraphicsService_ExecuteCommandBuffer = ExecuteCommandBufferInterop;
    service->GraphicsService_GetCommandBufferStatus = GetCommandBufferStatusInterop;
    service->GraphicsService_SetShaderBuffer = SetShaderBufferInterop;
    service->GraphicsService_SetShaderBuffers = SetShaderBuffersInterop;
    service->GraphicsService_SetShaderTexture = SetShaderTextureInterop;
    service->GraphicsService_SetShaderTextures = SetShaderTexturesInterop;
    service->GraphicsService_SetShaderIndirectCommandList = SetShaderIndirectCommandListInterop;
    service->GraphicsService_SetShaderIndirectCommandLists = SetShaderIndirectCommandListsInterop;
    service->GraphicsService_CreateCopyCommandList = CreateCopyCommandListInterop;
    service->GraphicsService_CommitCopyCommandList = CommitCopyCommandListInterop;
    service->GraphicsService_CopyDataToGraphicsBuffer = CopyDataToGraphicsBufferInterop;
    service->GraphicsService_CopyDataToTexture = CopyDataToTextureInterop;
    service->GraphicsService_CopyTexture = CopyTextureInterop;
    service->GraphicsService_ResetIndirectCommandList = ResetIndirectCommandListInterop;
    service->GraphicsService_OptimizeIndirectCommandList = OptimizeIndirectCommandListInterop;
    service->GraphicsService_CreateComputeCommandList = CreateComputeCommandListInterop;
    service->GraphicsService_CommitComputeCommandList = CommitComputeCommandListInterop;
    service->GraphicsService_DispatchThreads = DispatchThreadsInterop;
    service->GraphicsService_CreateRenderCommandList = CreateRenderCommandListInterop;
    service->GraphicsService_CommitRenderCommandList = CommitRenderCommandListInterop;
    service->GraphicsService_SetPipelineState = SetPipelineStateInterop;
    service->GraphicsService_SetShader = SetShaderInterop;
    service->GraphicsService_ExecuteIndirectCommandBuffer = ExecuteIndirectCommandBufferInterop;
    service->GraphicsService_SetIndexBuffer = SetIndexBufferInterop;
    service->GraphicsService_DrawIndexedPrimitives = DrawIndexedPrimitivesInterop;
    service->GraphicsService_DrawPrimitives = DrawPrimitivesInterop;
    service->GraphicsService_WaitForCommandList = WaitForCommandListInterop;
    service->GraphicsService_PresentScreenBuffer = PresentScreenBufferInterop;
    service->GraphicsService_WaitForAvailableScreenBuffer = WaitForAvailableScreenBufferInterop;
}

#pragma once
#include "../Direct3D12GraphicsService.h"

struct Vector2 GetRenderSizeInterop(void* context)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetRenderSize();
}

void GetGraphicsAdapterNameInterop(void* context, char* output)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->GetGraphicsAdapterName(output);
}

int CreateGraphicsBufferInterop(void* context, unsigned int graphicsBufferId, int length, int isWriteOnly, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsBuffer(graphicsBufferId, length, isWriteOnly, label);
}

int CreateTextureInterop(void* context, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateTexture(textureId, textureFormat, width, height, faceCount, mipLevels, multisampleCount, isRenderTarget, label);
}

void DeleteTextureInterop(void* context, unsigned int textureId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteTexture(textureId);
}

int CreateIndirectCommandBufferInterop(void* context, unsigned int indirectCommandBufferId, int maxCommandCount, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateIndirectCommandBuffer(indirectCommandBufferId, maxCommandCount, label);
}

int CreateShaderInterop(void* context, unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateShader(shaderId, computeShaderFunction, shaderByteCode, shaderByteCodeLength, label);
}

void DeleteShaderInterop(void* context, unsigned int shaderId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShader(shaderId);
}

int CreatePipelineStateInterop(void* context, unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreatePipelineState(pipelineStateId, shaderId, renderPassDescriptor, label);
}

void DeletePipelineStateInterop(void* context, unsigned int pipelineStateId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeletePipelineState(pipelineStateId);
}

int CreateCommandBufferInterop(void* context, unsigned int commandBufferId, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCommandBuffer(commandBufferId, label);
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

void UploadDataToGraphicsBufferInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->UploadDataToGraphicsBuffer(commandListId, graphicsBufferId, data, dataLength);
}

void CopyGraphicsBufferDataToCpuInterop(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyGraphicsBufferDataToCpu(commandListId, graphicsBufferId, length);
}

void ReadGraphicsBufferDataInterop(void* context, unsigned int graphicsBufferId, void* data, int dataLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ReadGraphicsBufferData(graphicsBufferId, data, dataLength);
}

void UploadDataToTextureInterop(void* context, unsigned int commandListId, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, void* data, int dataLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->UploadDataToTexture(commandListId, textureId, textureFormat, width, height, slice, mipLevel, data, dataLength);
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
    service->GraphicsService_GetRenderSize = GetRenderSizeInterop;
    service->GraphicsService_GetGraphicsAdapterName = GetGraphicsAdapterNameInterop;
    service->GraphicsService_CreateGraphicsBuffer = CreateGraphicsBufferInterop;
    service->GraphicsService_CreateTexture = CreateTextureInterop;
    service->GraphicsService_DeleteTexture = DeleteTextureInterop;
    service->GraphicsService_CreateIndirectCommandBuffer = CreateIndirectCommandBufferInterop;
    service->GraphicsService_CreateShader = CreateShaderInterop;
    service->GraphicsService_DeleteShader = DeleteShaderInterop;
    service->GraphicsService_CreatePipelineState = CreatePipelineStateInterop;
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
    service->GraphicsService_UploadDataToGraphicsBuffer = UploadDataToGraphicsBufferInterop;
    service->GraphicsService_CopyGraphicsBufferDataToCpu = CopyGraphicsBufferDataToCpuInterop;
    service->GraphicsService_ReadGraphicsBufferData = ReadGraphicsBufferDataInterop;
    service->GraphicsService_UploadDataToTexture = UploadDataToTextureInterop;
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

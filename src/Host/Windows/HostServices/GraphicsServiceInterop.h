#pragma once
#include "../Direct3D12GraphicsService.h"

void GetGraphicsAdapterNameInterop(void* context, char* output)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->GetGraphicsAdapterName(output);
}

struct GraphicsAllocationInfos GetTextureAllocationInfosInterop(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetTextureAllocationInfos(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void* CreateCommandQueueInterop(void* context, enum GraphicsServiceCommandType commandQueueType)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCommandQueue(commandQueueType);
}

void SetCommandQueueLabelInterop(void* context, void* commandQueuePointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetCommandQueueLabel(commandQueuePointer, label);
}

void DeleteCommandQueueInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteCommandQueue(commandQueuePointer);
}

void ResetCommandQueueInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetCommandQueue(commandQueuePointer);
}

unsigned long GetCommandQueueTimestampFrequencyInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetCommandQueueTimestampFrequency(commandQueuePointer);
}

unsigned long ExecuteCommandListsInterop(void* context, void* commandQueuePointer, void** commandLists, int commandListsLength, int isAwaitable)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->ExecuteCommandLists(commandQueuePointer, commandLists, commandListsLength, isAwaitable);
}

void WaitForCommandQueueInterop(void* context, void* commandQueuePointer, void* commandQueueToWaitPointer, unsigned long fenceValue)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForCommandQueue(commandQueuePointer, commandQueueToWaitPointer, fenceValue);
}

void WaitForCommandQueueOnCpuInterop(void* context, void* commandQueueToWaitPointer, unsigned long fenceValue)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForCommandQueueOnCpu(commandQueueToWaitPointer, fenceValue);
}

void* CreateCommandListInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCommandList(commandQueuePointer);
}

void SetCommandListLabelInterop(void* context, void* commandListPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetCommandListLabel(commandListPointer, label);
}

void DeleteCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteCommandList(commandListPointer);
}

void ResetCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetCommandList(commandListPointer);
}

void CommitCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CommitCommandList(commandListPointer);
}

void* CreateGraphicsHeapInterop(void* context, enum GraphicsServiceHeapType type, unsigned long length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsHeap(type, length);
}

void SetGraphicsHeapLabelInterop(void* context, void* graphicsHeapPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetGraphicsHeapLabel(graphicsHeapPointer, label);
}

void DeleteGraphicsHeapInterop(void* context, void* graphicsHeapPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteGraphicsHeap(graphicsHeapPointer);
}

void* CreateGraphicsBufferInterop(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsBuffer(graphicsHeapPointer, heapOffset, isAliasable, sizeInBytes);
}

void SetGraphicsBufferLabelInterop(void* context, void* graphicsBufferPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetGraphicsBufferLabel(graphicsBufferPointer, label);
}

void DeleteGraphicsBufferInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteGraphicsBuffer(graphicsBufferPointer);
}

void* GetGraphicsBufferCpuPointerInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetGraphicsBufferCpuPointer(graphicsBufferPointer);
}

void* CreateTextureInterop(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateTexture(graphicsHeapPointer, heapOffset, isAliasable, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void SetTextureLabelInterop(void* context, void* texturePointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetTextureLabel(texturePointer, label);
}

void DeleteTextureInterop(void* context, void* texturePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteTexture(texturePointer);
}

void* CreateSwapChainInterop(void* context, void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateSwapChain(windowPointer, commandQueuePointer, width, height, textureFormat);
}

void ResizeSwapChainInterop(void* context, void* swapChainPointer, int width, int height)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResizeSwapChain(swapChainPointer, width, height);
}

void* GetSwapChainBackBufferTextureInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetSwapChainBackBufferTexture(swapChainPointer);
}

void PresentSwapChainInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->PresentSwapChain(swapChainPointer);
}

void WaitForSwapChainOnCpuInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForSwapChainOnCpu(swapChainPointer);
}

void* CreateIndirectCommandBufferInterop(void* context, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateIndirectCommandBuffer(maxCommandCount);
}

void SetIndirectCommandBufferLabelInterop(void* context, void* indirectCommandBufferPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetIndirectCommandBufferLabel(indirectCommandBufferPointer, label);
}

void DeleteIndirectCommandBufferInterop(void* context, void* indirectCommandBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteIndirectCommandBuffer(indirectCommandBufferPointer);
}

void* CreateQueryBufferInterop(void* context, enum GraphicsQueryBufferType queryBufferType, int length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateQueryBuffer(queryBufferType, length);
}

void SetQueryBufferLabelInterop(void* context, void* queryBufferPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetQueryBufferLabel(queryBufferPointer, label);
}

void DeleteQueryBufferInterop(void* context, void* queryBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteQueryBuffer(queryBufferPointer);
}

void* CreateShaderInterop(void* context, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateShader(computeShaderFunction, shaderByteCode, shaderByteCodeLength);
}

void SetShaderLabelInterop(void* context, void* shaderPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderLabel(shaderPointer, label);
}

void DeleteShaderInterop(void* context, void* shaderPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShader(shaderPointer);
}

void* CreatePipelineStateInterop(void* context, void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreatePipelineState(shaderPointer, renderPassDescriptor);
}

void SetPipelineStateLabelInterop(void* context, void* pipelineStatePointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetPipelineStateLabel(pipelineStatePointer, label);
}

void DeletePipelineStateInterop(void* context, void* pipelineStatePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeletePipelineState(pipelineStatePointer);
}

void SetShaderBufferInterop(void* context, void* commandListPointer, void* graphicsBufferPointer, int slot, int isReadOnly, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderBuffer(commandListPointer, graphicsBufferPointer, slot, isReadOnly, index);
}

void SetShaderBuffersInterop(void* context, void* commandListPointer, void** graphicsBufferPointerList, int graphicsBufferPointerListLength, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderBuffers(commandListPointer, graphicsBufferPointerList, graphicsBufferPointerListLength, slot, index);
}

void SetShaderTextureInterop(void* context, void* commandListPointer, void* texturePointer, int slot, int isReadOnly, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderTexture(commandListPointer, texturePointer, slot, isReadOnly, index);
}

void SetShaderTexturesInterop(void* context, void* commandListPointer, void** texturePointerList, int texturePointerListLength, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderTextures(commandListPointer, texturePointerList, texturePointerListLength, slot, index);
}

void SetShaderIndirectCommandListInterop(void* context, void* commandListPointer, void* indirectCommandListPointer, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderIndirectCommandList(commandListPointer, indirectCommandListPointer, slot, index);
}

void SetShaderIndirectCommandListsInterop(void* context, void* commandListPointer, void** indirectCommandListPointerList, int indirectCommandListPointerListLength, int slot, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderIndirectCommandLists(commandListPointer, indirectCommandListPointerList, indirectCommandListPointerListLength, slot, index);
}

void CopyDataToGraphicsBufferInterop(void* context, void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyDataToGraphicsBuffer(commandListPointer, destinationGraphicsBufferPointer, sourceGraphicsBufferPointer, length);
}

void CopyDataToTextureInterop(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyDataToTexture(commandListPointer, destinationTexturePointer, sourceGraphicsBufferPointer, textureFormat, width, height, slice, mipLevel);
}

void CopyTextureInterop(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyTexture(commandListPointer, destinationTexturePointer, sourceTexturePointer);
}

void ResetIndirectCommandListInterop(void* context, void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetIndirectCommandList(commandListPointer, indirectCommandListPointer, maxCommandCount);
}

void OptimizeIndirectCommandListInterop(void* context, void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->OptimizeIndirectCommandList(commandListPointer, indirectCommandListPointer, maxCommandCount);
}

struct Vector3 DispatchThreadsInterop(void* context, void* commandListPointer, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->DispatchThreads(commandListPointer, threadCountX, threadCountY, threadCountZ);
}

void BeginRenderPassInterop(void* context, void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->BeginRenderPass(commandListPointer, renderPassDescriptor);
}

void EndRenderPassInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->EndRenderPass(commandListPointer);
}

void SetPipelineStateInterop(void* context, void* commandListPointer, void* pipelineStatePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetPipelineState(commandListPointer, pipelineStatePointer);
}

void SetShaderInterop(void* context, void* commandListPointer, void* shaderPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShader(commandListPointer, shaderPointer);
}

void ExecuteIndirectCommandBufferInterop(void* context, void* commandListPointer, void* indirectCommandBufferPointer, int maxCommandCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ExecuteIndirectCommandBuffer(commandListPointer, indirectCommandBufferPointer, maxCommandCount);
}

void SetIndexBufferInterop(void* context, void* commandListPointer, void* graphicsBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetIndexBuffer(commandListPointer, graphicsBufferPointer);
}

void DrawIndexedPrimitivesInterop(void* context, void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DrawIndexedPrimitives(commandListPointer, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId);
}

void DrawPrimitivesInterop(void* context, void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DrawPrimitives(commandListPointer, primitiveType, startVertex, vertexCount);
}

void QueryTimestampInterop(void* context, void* commandListPointer, void* queryBufferPointer, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->QueryTimestamp(commandListPointer, queryBufferPointer, index);
}

void ResolveQueryDataInterop(void* context, void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResolveQueryData(commandListPointer, queryBufferPointer, destinationBufferPointer, startIndex, endIndex);
}

void InitGraphicsService(const Direct3D12GraphicsService& context, GraphicsService* service)
{
    service->Context = (void*)&context;
    service->GraphicsService_GetGraphicsAdapterName = GetGraphicsAdapterNameInterop;
    service->GraphicsService_GetTextureAllocationInfos = GetTextureAllocationInfosInterop;
    service->GraphicsService_CreateCommandQueue = CreateCommandQueueInterop;
    service->GraphicsService_SetCommandQueueLabel = SetCommandQueueLabelInterop;
    service->GraphicsService_DeleteCommandQueue = DeleteCommandQueueInterop;
    service->GraphicsService_ResetCommandQueue = ResetCommandQueueInterop;
    service->GraphicsService_GetCommandQueueTimestampFrequency = GetCommandQueueTimestampFrequencyInterop;
    service->GraphicsService_ExecuteCommandLists = ExecuteCommandListsInterop;
    service->GraphicsService_WaitForCommandQueue = WaitForCommandQueueInterop;
    service->GraphicsService_WaitForCommandQueueOnCpu = WaitForCommandQueueOnCpuInterop;
    service->GraphicsService_CreateCommandList = CreateCommandListInterop;
    service->GraphicsService_SetCommandListLabel = SetCommandListLabelInterop;
    service->GraphicsService_DeleteCommandList = DeleteCommandListInterop;
    service->GraphicsService_ResetCommandList = ResetCommandListInterop;
    service->GraphicsService_CommitCommandList = CommitCommandListInterop;
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
    service->GraphicsService_CreateSwapChain = CreateSwapChainInterop;
    service->GraphicsService_ResizeSwapChain = ResizeSwapChainInterop;
    service->GraphicsService_GetSwapChainBackBufferTexture = GetSwapChainBackBufferTextureInterop;
    service->GraphicsService_PresentSwapChain = PresentSwapChainInterop;
    service->GraphicsService_WaitForSwapChainOnCpu = WaitForSwapChainOnCpuInterop;
    service->GraphicsService_CreateIndirectCommandBuffer = CreateIndirectCommandBufferInterop;
    service->GraphicsService_SetIndirectCommandBufferLabel = SetIndirectCommandBufferLabelInterop;
    service->GraphicsService_DeleteIndirectCommandBuffer = DeleteIndirectCommandBufferInterop;
    service->GraphicsService_CreateQueryBuffer = CreateQueryBufferInterop;
    service->GraphicsService_SetQueryBufferLabel = SetQueryBufferLabelInterop;
    service->GraphicsService_DeleteQueryBuffer = DeleteQueryBufferInterop;
    service->GraphicsService_CreateShader = CreateShaderInterop;
    service->GraphicsService_SetShaderLabel = SetShaderLabelInterop;
    service->GraphicsService_DeleteShader = DeleteShaderInterop;
    service->GraphicsService_CreatePipelineState = CreatePipelineStateInterop;
    service->GraphicsService_SetPipelineStateLabel = SetPipelineStateLabelInterop;
    service->GraphicsService_DeletePipelineState = DeletePipelineStateInterop;
    service->GraphicsService_SetShaderBuffer = SetShaderBufferInterop;
    service->GraphicsService_SetShaderBuffers = SetShaderBuffersInterop;
    service->GraphicsService_SetShaderTexture = SetShaderTextureInterop;
    service->GraphicsService_SetShaderTextures = SetShaderTexturesInterop;
    service->GraphicsService_SetShaderIndirectCommandList = SetShaderIndirectCommandListInterop;
    service->GraphicsService_SetShaderIndirectCommandLists = SetShaderIndirectCommandListsInterop;
    service->GraphicsService_CopyDataToGraphicsBuffer = CopyDataToGraphicsBufferInterop;
    service->GraphicsService_CopyDataToTexture = CopyDataToTextureInterop;
    service->GraphicsService_CopyTexture = CopyTextureInterop;
    service->GraphicsService_ResetIndirectCommandList = ResetIndirectCommandListInterop;
    service->GraphicsService_OptimizeIndirectCommandList = OptimizeIndirectCommandListInterop;
    service->GraphicsService_DispatchThreads = DispatchThreadsInterop;
    service->GraphicsService_BeginRenderPass = BeginRenderPassInterop;
    service->GraphicsService_EndRenderPass = EndRenderPassInterop;
    service->GraphicsService_SetPipelineState = SetPipelineStateInterop;
    service->GraphicsService_SetShader = SetShaderInterop;
    service->GraphicsService_ExecuteIndirectCommandBuffer = ExecuteIndirectCommandBufferInterop;
    service->GraphicsService_SetIndexBuffer = SetIndexBufferInterop;
    service->GraphicsService_DrawIndexedPrimitives = DrawIndexedPrimitivesInterop;
    service->GraphicsService_DrawPrimitives = DrawPrimitivesInterop;
    service->GraphicsService_QueryTimestamp = QueryTimestampInterop;
    service->GraphicsService_ResolveQueryData = ResolveQueryDataInterop;
}

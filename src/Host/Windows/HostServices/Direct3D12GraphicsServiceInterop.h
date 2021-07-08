#pragma once
#include "../Direct3D12GraphicsService.h"

void Direct3D12GraphicsServiceGetGraphicsAdapterNameInterop(void* context, char* output)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->GetGraphicsAdapterName(output);
}

struct GraphicsAllocationInfos Direct3D12GraphicsServiceGetBufferAllocationInfosInterop(void* context, int sizeInBytes)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetBufferAllocationInfos(sizeInBytes);
}

struct GraphicsAllocationInfos Direct3D12GraphicsServiceGetTextureAllocationInfosInterop(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetTextureAllocationInfos(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void* Direct3D12GraphicsServiceCreateCommandQueueInterop(void* context, enum GraphicsServiceCommandType commandQueueType)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCommandQueue(commandQueueType);
}

void Direct3D12GraphicsServiceSetCommandQueueLabelInterop(void* context, void* commandQueuePointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetCommandQueueLabel(commandQueuePointer, label);
}

void Direct3D12GraphicsServiceDeleteCommandQueueInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteCommandQueue(commandQueuePointer);
}

void Direct3D12GraphicsServiceResetCommandQueueInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetCommandQueue(commandQueuePointer);
}

unsigned long Direct3D12GraphicsServiceGetCommandQueueTimestampFrequencyInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetCommandQueueTimestampFrequency(commandQueuePointer);
}

unsigned long Direct3D12GraphicsServiceExecuteCommandListsInterop(void* context, void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->ExecuteCommandLists(commandQueuePointer, commandLists, commandListsLength, fencesToWait, fencesToWaitLength);
}

void Direct3D12GraphicsServiceWaitForCommandQueueOnCpuInterop(void* context, struct GraphicsFence fenceToWait)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForCommandQueueOnCpu(fenceToWait);
}

void* Direct3D12GraphicsServiceCreateCommandListInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateCommandList(commandQueuePointer);
}

void Direct3D12GraphicsServiceSetCommandListLabelInterop(void* context, void* commandListPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetCommandListLabel(commandListPointer, label);
}

void Direct3D12GraphicsServiceDeleteCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteCommandList(commandListPointer);
}

void Direct3D12GraphicsServiceResetCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetCommandList(commandListPointer);
}

void Direct3D12GraphicsServiceCommitCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CommitCommandList(commandListPointer);
}

void* Direct3D12GraphicsServiceCreateGraphicsHeapInterop(void* context, enum GraphicsServiceHeapType type, unsigned long sizeInBytes)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsHeap(type, sizeInBytes);
}

void Direct3D12GraphicsServiceSetGraphicsHeapLabelInterop(void* context, void* graphicsHeapPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetGraphicsHeapLabel(graphicsHeapPointer, label);
}

void Direct3D12GraphicsServiceDeleteGraphicsHeapInterop(void* context, void* graphicsHeapPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteGraphicsHeap(graphicsHeapPointer);
}

void* Direct3D12GraphicsServiceCreateShaderResourceHeapInterop(void* context, unsigned long length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateShaderResourceHeap(length);
}

void Direct3D12GraphicsServiceSetShaderResourceHeapLabelInterop(void* context, void* shaderResourceHeapPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderResourceHeapLabel(shaderResourceHeapPointer, label);
}

void Direct3D12GraphicsServiceDeleteShaderResourceHeapInterop(void* context, void* shaderResourceHeapPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShaderResourceHeap(shaderResourceHeapPointer);
}

void Direct3D12GraphicsServiceCreateShaderResourceTextureInterop(void* context, void* shaderResourceHeapPointer, unsigned int index, void* texturePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CreateShaderResourceTexture(shaderResourceHeapPointer, index, texturePointer);
}

void Direct3D12GraphicsServiceDeleteShaderResourceTextureInterop(void* context, void* shaderResourceHeapPointer, unsigned int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShaderResourceTexture(shaderResourceHeapPointer, index);
}

void Direct3D12GraphicsServiceCreateShaderResourceBufferInterop(void* context, void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer, int isWriteable)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CreateShaderResourceBuffer(shaderResourceHeapPointer, index, bufferPointer, isWriteable);
}

void Direct3D12GraphicsServiceDeleteShaderResourceBufferInterop(void* context, void* shaderResourceHeapPointer, unsigned int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShaderResourceBuffer(shaderResourceHeapPointer, index);
}

void* Direct3D12GraphicsServiceCreateGraphicsBufferInterop(void* context, void* graphicsHeapPointer, unsigned long heapOffset, enum GraphicsBufferUsage graphicsBufferUsage, int sizeInBytes)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateGraphicsBuffer(graphicsHeapPointer, heapOffset, graphicsBufferUsage, sizeInBytes);
}

void Direct3D12GraphicsServiceSetGraphicsBufferLabelInterop(void* context, void* graphicsBufferPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetGraphicsBufferLabel(graphicsBufferPointer, label);
}

void Direct3D12GraphicsServiceDeleteGraphicsBufferInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteGraphicsBuffer(graphicsBufferPointer);
}

void* Direct3D12GraphicsServiceGetGraphicsBufferCpuPointerInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetGraphicsBufferCpuPointer(graphicsBufferPointer);
}

void Direct3D12GraphicsServiceReleaseGraphicsBufferCpuPointerInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ReleaseGraphicsBufferCpuPointer(graphicsBufferPointer);
}

void* Direct3D12GraphicsServiceCreateTextureInterop(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateTexture(graphicsHeapPointer, heapOffset, isAliasable, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void Direct3D12GraphicsServiceSetTextureLabelInterop(void* context, void* texturePointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetTextureLabel(texturePointer, label);
}

void Direct3D12GraphicsServiceDeleteTextureInterop(void* context, void* texturePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteTexture(texturePointer);
}

void* Direct3D12GraphicsServiceCreateSwapChainInterop(void* context, void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateSwapChain(windowPointer, commandQueuePointer, width, height, textureFormat);
}

void Direct3D12GraphicsServiceDeleteSwapChainInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteSwapChain(swapChainPointer);
}

void Direct3D12GraphicsServiceResizeSwapChainInterop(void* context, void* swapChainPointer, int width, int height)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResizeSwapChain(swapChainPointer, width, height);
}

void* Direct3D12GraphicsServiceGetSwapChainBackBufferTextureInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->GetSwapChainBackBufferTexture(swapChainPointer);
}

unsigned long Direct3D12GraphicsServicePresentSwapChainInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->PresentSwapChain(swapChainPointer);
}

void Direct3D12GraphicsServiceWaitForSwapChainOnCpuInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->WaitForSwapChainOnCpu(swapChainPointer);
}

void* Direct3D12GraphicsServiceCreateQueryBufferInterop(void* context, enum GraphicsQueryBufferType queryBufferType, int length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateQueryBuffer(queryBufferType, length);
}

void Direct3D12GraphicsServiceResetQueryBufferInterop(void* context, void* queryBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResetQueryBuffer(queryBufferPointer);
}

void Direct3D12GraphicsServiceSetQueryBufferLabelInterop(void* context, void* queryBufferPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetQueryBufferLabel(queryBufferPointer, label);
}

void Direct3D12GraphicsServiceDeleteQueryBufferInterop(void* context, void* queryBufferPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteQueryBuffer(queryBufferPointer);
}

void* Direct3D12GraphicsServiceCreateShaderInterop(void* context, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateShader(computeShaderFunction, shaderByteCode, shaderByteCodeLength);
}

void Direct3D12GraphicsServiceSetShaderLabelInterop(void* context, void* shaderPointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderLabel(shaderPointer, label);
}

void Direct3D12GraphicsServiceDeleteShaderInterop(void* context, void* shaderPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeleteShader(shaderPointer);
}

void* Direct3D12GraphicsServiceCreateComputePipelineStateInterop(void* context, void* shaderPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreateComputePipelineState(shaderPointer);
}

void* Direct3D12GraphicsServiceCreatePipelineStateInterop(void* context, void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    return contextObject->CreatePipelineState(shaderPointer, renderPassDescriptor);
}

void Direct3D12GraphicsServiceSetPipelineStateLabelInterop(void* context, void* pipelineStatePointer, char* label)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetPipelineStateLabel(pipelineStatePointer, label);
}

void Direct3D12GraphicsServiceDeletePipelineStateInterop(void* context, void* pipelineStatePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DeletePipelineState(pipelineStatePointer);
}

void Direct3D12GraphicsServiceCopyDataToGraphicsBufferInterop(void* context, void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int length)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyDataToGraphicsBuffer(commandListPointer, destinationGraphicsBufferPointer, sourceGraphicsBufferPointer, length);
}

void Direct3D12GraphicsServiceCopyDataToTextureInterop(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyDataToTexture(commandListPointer, destinationTexturePointer, sourceGraphicsBufferPointer, textureFormat, width, height, slice, mipLevel);
}

void Direct3D12GraphicsServiceCopyTextureInterop(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->CopyTexture(commandListPointer, destinationTexturePointer, sourceTexturePointer);
}

void Direct3D12GraphicsServiceTransitionGraphicsBufferToStateInterop(void* context, void* commandListPointer, void* graphicsBufferPointer, enum GraphicsResourceState resourceState)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->TransitionGraphicsBufferToState(commandListPointer, graphicsBufferPointer, resourceState);
}

void Direct3D12GraphicsServiceDispatchThreadsInterop(void* context, void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DispatchThreads(commandListPointer, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
}

void Direct3D12GraphicsServiceBeginRenderPassInterop(void* context, void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->BeginRenderPass(commandListPointer, renderPassDescriptor);
}

void Direct3D12GraphicsServiceEndRenderPassInterop(void* context, void* commandListPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->EndRenderPass(commandListPointer);
}

void Direct3D12GraphicsServiceSetPipelineStateInterop(void* context, void* commandListPointer, void* pipelineStatePointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetPipelineState(commandListPointer, pipelineStatePointer);
}

void Direct3D12GraphicsServiceSetShaderResourceHeapInterop(void* context, void* commandListPointer, void* shaderResourceHeapPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderResourceHeap(commandListPointer, shaderResourceHeapPointer);
}

void Direct3D12GraphicsServiceSetShaderInterop(void* context, void* commandListPointer, void* shaderPointer)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShader(commandListPointer, shaderPointer);
}

void Direct3D12GraphicsServiceSetShaderParameterValuesInterop(void* context, void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->SetShaderParameterValues(commandListPointer, slot, values, valuesLength);
}

void Direct3D12GraphicsServiceDispatchMeshInterop(void* context, void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->DispatchMesh(commandListPointer, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
}

void Direct3D12GraphicsServiceExecuteIndirectInterop(void* context, void* commandListPointer, unsigned int maxCommandCount, void* commandGraphicsBufferPointer, unsigned int commandBufferOffset)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ExecuteIndirect(commandListPointer, maxCommandCount, commandGraphicsBufferPointer, commandBufferOffset);
}

void Direct3D12GraphicsServiceBeginQueryInterop(void* context, void* commandListPointer, void* queryBufferPointer, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->BeginQuery(commandListPointer, queryBufferPointer, index);
}

void Direct3D12GraphicsServiceEndQueryInterop(void* context, void* commandListPointer, void* queryBufferPointer, int index)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->EndQuery(commandListPointer, queryBufferPointer, index);
}

void Direct3D12GraphicsServiceResolveQueryDataInterop(void* context, void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex)
{
    auto contextObject = (Direct3D12GraphicsService*)context;
    contextObject->ResolveQueryData(commandListPointer, queryBufferPointer, destinationBufferPointer, startIndex, endIndex);
}

void InitDirect3D12GraphicsService(const Direct3D12GraphicsService* context, GraphicsService* service)
{
    service->Context = (void*)context;
    service->GraphicsService_GetGraphicsAdapterName = Direct3D12GraphicsServiceGetGraphicsAdapterNameInterop;
    service->GraphicsService_GetBufferAllocationInfos = Direct3D12GraphicsServiceGetBufferAllocationInfosInterop;
    service->GraphicsService_GetTextureAllocationInfos = Direct3D12GraphicsServiceGetTextureAllocationInfosInterop;
    service->GraphicsService_CreateCommandQueue = Direct3D12GraphicsServiceCreateCommandQueueInterop;
    service->GraphicsService_SetCommandQueueLabel = Direct3D12GraphicsServiceSetCommandQueueLabelInterop;
    service->GraphicsService_DeleteCommandQueue = Direct3D12GraphicsServiceDeleteCommandQueueInterop;
    service->GraphicsService_ResetCommandQueue = Direct3D12GraphicsServiceResetCommandQueueInterop;
    service->GraphicsService_GetCommandQueueTimestampFrequency = Direct3D12GraphicsServiceGetCommandQueueTimestampFrequencyInterop;
    service->GraphicsService_ExecuteCommandLists = Direct3D12GraphicsServiceExecuteCommandListsInterop;
    service->GraphicsService_WaitForCommandQueueOnCpu = Direct3D12GraphicsServiceWaitForCommandQueueOnCpuInterop;
    service->GraphicsService_CreateCommandList = Direct3D12GraphicsServiceCreateCommandListInterop;
    service->GraphicsService_SetCommandListLabel = Direct3D12GraphicsServiceSetCommandListLabelInterop;
    service->GraphicsService_DeleteCommandList = Direct3D12GraphicsServiceDeleteCommandListInterop;
    service->GraphicsService_ResetCommandList = Direct3D12GraphicsServiceResetCommandListInterop;
    service->GraphicsService_CommitCommandList = Direct3D12GraphicsServiceCommitCommandListInterop;
    service->GraphicsService_CreateGraphicsHeap = Direct3D12GraphicsServiceCreateGraphicsHeapInterop;
    service->GraphicsService_SetGraphicsHeapLabel = Direct3D12GraphicsServiceSetGraphicsHeapLabelInterop;
    service->GraphicsService_DeleteGraphicsHeap = Direct3D12GraphicsServiceDeleteGraphicsHeapInterop;
    service->GraphicsService_CreateShaderResourceHeap = Direct3D12GraphicsServiceCreateShaderResourceHeapInterop;
    service->GraphicsService_SetShaderResourceHeapLabel = Direct3D12GraphicsServiceSetShaderResourceHeapLabelInterop;
    service->GraphicsService_DeleteShaderResourceHeap = Direct3D12GraphicsServiceDeleteShaderResourceHeapInterop;
    service->GraphicsService_CreateShaderResourceTexture = Direct3D12GraphicsServiceCreateShaderResourceTextureInterop;
    service->GraphicsService_DeleteShaderResourceTexture = Direct3D12GraphicsServiceDeleteShaderResourceTextureInterop;
    service->GraphicsService_CreateShaderResourceBuffer = Direct3D12GraphicsServiceCreateShaderResourceBufferInterop;
    service->GraphicsService_DeleteShaderResourceBuffer = Direct3D12GraphicsServiceDeleteShaderResourceBufferInterop;
    service->GraphicsService_CreateGraphicsBuffer = Direct3D12GraphicsServiceCreateGraphicsBufferInterop;
    service->GraphicsService_SetGraphicsBufferLabel = Direct3D12GraphicsServiceSetGraphicsBufferLabelInterop;
    service->GraphicsService_DeleteGraphicsBuffer = Direct3D12GraphicsServiceDeleteGraphicsBufferInterop;
    service->GraphicsService_GetGraphicsBufferCpuPointer = Direct3D12GraphicsServiceGetGraphicsBufferCpuPointerInterop;
    service->GraphicsService_ReleaseGraphicsBufferCpuPointer = Direct3D12GraphicsServiceReleaseGraphicsBufferCpuPointerInterop;
    service->GraphicsService_CreateTexture = Direct3D12GraphicsServiceCreateTextureInterop;
    service->GraphicsService_SetTextureLabel = Direct3D12GraphicsServiceSetTextureLabelInterop;
    service->GraphicsService_DeleteTexture = Direct3D12GraphicsServiceDeleteTextureInterop;
    service->GraphicsService_CreateSwapChain = Direct3D12GraphicsServiceCreateSwapChainInterop;
    service->GraphicsService_DeleteSwapChain = Direct3D12GraphicsServiceDeleteSwapChainInterop;
    service->GraphicsService_ResizeSwapChain = Direct3D12GraphicsServiceResizeSwapChainInterop;
    service->GraphicsService_GetSwapChainBackBufferTexture = Direct3D12GraphicsServiceGetSwapChainBackBufferTextureInterop;
    service->GraphicsService_PresentSwapChain = Direct3D12GraphicsServicePresentSwapChainInterop;
    service->GraphicsService_WaitForSwapChainOnCpu = Direct3D12GraphicsServiceWaitForSwapChainOnCpuInterop;
    service->GraphicsService_CreateQueryBuffer = Direct3D12GraphicsServiceCreateQueryBufferInterop;
    service->GraphicsService_ResetQueryBuffer = Direct3D12GraphicsServiceResetQueryBufferInterop;
    service->GraphicsService_SetQueryBufferLabel = Direct3D12GraphicsServiceSetQueryBufferLabelInterop;
    service->GraphicsService_DeleteQueryBuffer = Direct3D12GraphicsServiceDeleteQueryBufferInterop;
    service->GraphicsService_CreateShader = Direct3D12GraphicsServiceCreateShaderInterop;
    service->GraphicsService_SetShaderLabel = Direct3D12GraphicsServiceSetShaderLabelInterop;
    service->GraphicsService_DeleteShader = Direct3D12GraphicsServiceDeleteShaderInterop;
    service->GraphicsService_CreateComputePipelineState = Direct3D12GraphicsServiceCreateComputePipelineStateInterop;
    service->GraphicsService_CreatePipelineState = Direct3D12GraphicsServiceCreatePipelineStateInterop;
    service->GraphicsService_SetPipelineStateLabel = Direct3D12GraphicsServiceSetPipelineStateLabelInterop;
    service->GraphicsService_DeletePipelineState = Direct3D12GraphicsServiceDeletePipelineStateInterop;
    service->GraphicsService_CopyDataToGraphicsBuffer = Direct3D12GraphicsServiceCopyDataToGraphicsBufferInterop;
    service->GraphicsService_CopyDataToTexture = Direct3D12GraphicsServiceCopyDataToTextureInterop;
    service->GraphicsService_CopyTexture = Direct3D12GraphicsServiceCopyTextureInterop;
    service->GraphicsService_TransitionGraphicsBufferToState = Direct3D12GraphicsServiceTransitionGraphicsBufferToStateInterop;
    service->GraphicsService_DispatchThreads = Direct3D12GraphicsServiceDispatchThreadsInterop;
    service->GraphicsService_BeginRenderPass = Direct3D12GraphicsServiceBeginRenderPassInterop;
    service->GraphicsService_EndRenderPass = Direct3D12GraphicsServiceEndRenderPassInterop;
    service->GraphicsService_SetPipelineState = Direct3D12GraphicsServiceSetPipelineStateInterop;
    service->GraphicsService_SetShaderResourceHeap = Direct3D12GraphicsServiceSetShaderResourceHeapInterop;
    service->GraphicsService_SetShader = Direct3D12GraphicsServiceSetShaderInterop;
    service->GraphicsService_SetShaderParameterValues = Direct3D12GraphicsServiceSetShaderParameterValuesInterop;
    service->GraphicsService_DispatchMesh = Direct3D12GraphicsServiceDispatchMeshInterop;
    service->GraphicsService_ExecuteIndirect = Direct3D12GraphicsServiceExecuteIndirectInterop;
    service->GraphicsService_BeginQuery = Direct3D12GraphicsServiceBeginQueryInterop;
    service->GraphicsService_EndQuery = Direct3D12GraphicsServiceEndQueryInterop;
    service->GraphicsService_ResolveQueryData = Direct3D12GraphicsServiceResolveQueryDataInterop;
}

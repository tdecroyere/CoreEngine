#pragma once
#include "../VulkanGraphicsService.h"

void VulkanGraphicsServiceGetGraphicsAdapterNameInterop(void* context, char* output)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->GetGraphicsAdapterName(output);
}

struct GraphicsAllocationInfos VulkanGraphicsServiceGetBufferAllocationInfosInterop(void* context, int sizeInBytes)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->GetBufferAllocationInfos(sizeInBytes);
}

struct GraphicsAllocationInfos VulkanGraphicsServiceGetTextureAllocationInfosInterop(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->GetTextureAllocationInfos(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void* VulkanGraphicsServiceCreateCommandQueueInterop(void* context, enum GraphicsServiceCommandType commandQueueType)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateCommandQueue(commandQueueType);
}

void VulkanGraphicsServiceSetCommandQueueLabelInterop(void* context, void* commandQueuePointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetCommandQueueLabel(commandQueuePointer, label);
}

void VulkanGraphicsServiceDeleteCommandQueueInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteCommandQueue(commandQueuePointer);
}

void VulkanGraphicsServiceResetCommandQueueInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ResetCommandQueue(commandQueuePointer);
}

unsigned long VulkanGraphicsServiceGetCommandQueueTimestampFrequencyInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->GetCommandQueueTimestampFrequency(commandQueuePointer);
}

unsigned long VulkanGraphicsServiceExecuteCommandListsInterop(void* context, void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->ExecuteCommandLists(commandQueuePointer, commandLists, commandListsLength, fencesToWait, fencesToWaitLength);
}

void VulkanGraphicsServiceWaitForCommandQueueOnCpuInterop(void* context, struct GraphicsFence fenceToWait)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->WaitForCommandQueueOnCpu(fenceToWait);
}

void* VulkanGraphicsServiceCreateCommandListInterop(void* context, void* commandQueuePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateCommandList(commandQueuePointer);
}

void VulkanGraphicsServiceSetCommandListLabelInterop(void* context, void* commandListPointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetCommandListLabel(commandListPointer, label);
}

void VulkanGraphicsServiceDeleteCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteCommandList(commandListPointer);
}

void VulkanGraphicsServiceResetCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ResetCommandList(commandListPointer);
}

void VulkanGraphicsServiceCommitCommandListInterop(void* context, void* commandListPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->CommitCommandList(commandListPointer);
}

void* VulkanGraphicsServiceCreateGraphicsHeapInterop(void* context, enum GraphicsServiceHeapType type, unsigned long sizeInBytes)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateGraphicsHeap(type, sizeInBytes);
}

void VulkanGraphicsServiceSetGraphicsHeapLabelInterop(void* context, void* graphicsHeapPointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetGraphicsHeapLabel(graphicsHeapPointer, label);
}

void VulkanGraphicsServiceDeleteGraphicsHeapInterop(void* context, void* graphicsHeapPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteGraphicsHeap(graphicsHeapPointer);
}

void* VulkanGraphicsServiceCreateShaderResourceHeapInterop(void* context, unsigned long length)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateShaderResourceHeap(length);
}

void VulkanGraphicsServiceSetShaderResourceHeapLabelInterop(void* context, void* shaderResourceHeapPointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetShaderResourceHeapLabel(shaderResourceHeapPointer, label);
}

void VulkanGraphicsServiceDeleteShaderResourceHeapInterop(void* context, void* shaderResourceHeapPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteShaderResourceHeap(shaderResourceHeapPointer);
}

void VulkanGraphicsServiceCreateShaderResourceTextureInterop(void* context, void* shaderResourceHeapPointer, unsigned int index, void* texturePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->CreateShaderResourceTexture(shaderResourceHeapPointer, index, texturePointer);
}

void VulkanGraphicsServiceDeleteShaderResourceTextureInterop(void* context, void* shaderResourceHeapPointer, unsigned int index)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteShaderResourceTexture(shaderResourceHeapPointer, index);
}

void VulkanGraphicsServiceCreateShaderResourceBufferInterop(void* context, void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer, int isWriteable)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->CreateShaderResourceBuffer(shaderResourceHeapPointer, index, bufferPointer, isWriteable);
}

void VulkanGraphicsServiceDeleteShaderResourceBufferInterop(void* context, void* shaderResourceHeapPointer, unsigned int index)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteShaderResourceBuffer(shaderResourceHeapPointer, index);
}

void* VulkanGraphicsServiceCreateGraphicsBufferInterop(void* context, void* graphicsHeapPointer, unsigned long heapOffset, enum GraphicsBufferUsage graphicsBufferUsage, int sizeInBytes)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateGraphicsBuffer(graphicsHeapPointer, heapOffset, graphicsBufferUsage, sizeInBytes);
}

void VulkanGraphicsServiceSetGraphicsBufferLabelInterop(void* context, void* graphicsBufferPointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetGraphicsBufferLabel(graphicsBufferPointer, label);
}

void VulkanGraphicsServiceDeleteGraphicsBufferInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteGraphicsBuffer(graphicsBufferPointer);
}

void* VulkanGraphicsServiceGetGraphicsBufferCpuPointerInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->GetGraphicsBufferCpuPointer(graphicsBufferPointer);
}

void VulkanGraphicsServiceReleaseGraphicsBufferCpuPointerInterop(void* context, void* graphicsBufferPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ReleaseGraphicsBufferCpuPointer(graphicsBufferPointer);
}

void* VulkanGraphicsServiceCreateTextureInterop(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateTexture(graphicsHeapPointer, heapOffset, isAliasable, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
}

void VulkanGraphicsServiceSetTextureLabelInterop(void* context, void* texturePointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetTextureLabel(texturePointer, label);
}

void VulkanGraphicsServiceDeleteTextureInterop(void* context, void* texturePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteTexture(texturePointer);
}

void* VulkanGraphicsServiceCreateSwapChainInterop(void* context, void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateSwapChain(windowPointer, commandQueuePointer, width, height, textureFormat);
}

void VulkanGraphicsServiceDeleteSwapChainInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteSwapChain(swapChainPointer);
}

void VulkanGraphicsServiceResizeSwapChainInterop(void* context, void* swapChainPointer, int width, int height)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ResizeSwapChain(swapChainPointer, width, height);
}

void* VulkanGraphicsServiceGetSwapChainBackBufferTextureInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->GetSwapChainBackBufferTexture(swapChainPointer);
}

unsigned long VulkanGraphicsServicePresentSwapChainInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->PresentSwapChain(swapChainPointer);
}

void VulkanGraphicsServiceWaitForSwapChainOnCpuInterop(void* context, void* swapChainPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->WaitForSwapChainOnCpu(swapChainPointer);
}

void* VulkanGraphicsServiceCreateQueryBufferInterop(void* context, enum GraphicsQueryBufferType queryBufferType, int length)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateQueryBuffer(queryBufferType, length);
}

void VulkanGraphicsServiceResetQueryBufferInterop(void* context, void* queryBufferPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ResetQueryBuffer(queryBufferPointer);
}

void VulkanGraphicsServiceSetQueryBufferLabelInterop(void* context, void* queryBufferPointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetQueryBufferLabel(queryBufferPointer, label);
}

void VulkanGraphicsServiceDeleteQueryBufferInterop(void* context, void* queryBufferPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteQueryBuffer(queryBufferPointer);
}

void* VulkanGraphicsServiceCreateShaderInterop(void* context, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateShader(computeShaderFunction, shaderByteCode, shaderByteCodeLength);
}

void VulkanGraphicsServiceSetShaderLabelInterop(void* context, void* shaderPointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetShaderLabel(shaderPointer, label);
}

void VulkanGraphicsServiceDeleteShaderInterop(void* context, void* shaderPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeleteShader(shaderPointer);
}

void* VulkanGraphicsServiceCreateComputePipelineStateInterop(void* context, void* shaderPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreateComputePipelineState(shaderPointer);
}

void* VulkanGraphicsServiceCreatePipelineStateInterop(void* context, void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (VulkanGraphicsService*)context;
    return contextObject->CreatePipelineState(shaderPointer, renderPassDescriptor);
}

void VulkanGraphicsServiceSetPipelineStateLabelInterop(void* context, void* pipelineStatePointer, char* label)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetPipelineStateLabel(pipelineStatePointer, label);
}

void VulkanGraphicsServiceDeletePipelineStateInterop(void* context, void* pipelineStatePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DeletePipelineState(pipelineStatePointer);
}

void VulkanGraphicsServiceCopyDataToGraphicsBufferInterop(void* context, void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int length)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->CopyDataToGraphicsBuffer(commandListPointer, destinationGraphicsBufferPointer, sourceGraphicsBufferPointer, length);
}

void VulkanGraphicsServiceCopyDataToTextureInterop(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->CopyDataToTexture(commandListPointer, destinationTexturePointer, sourceGraphicsBufferPointer, textureFormat, width, height, slice, mipLevel);
}

void VulkanGraphicsServiceCopyTextureInterop(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->CopyTexture(commandListPointer, destinationTexturePointer, sourceTexturePointer);
}

void VulkanGraphicsServiceTransitionGraphicsBufferToStateInterop(void* context, void* commandListPointer, void* graphicsBufferPointer, enum GraphicsResourceState resourceState)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->TransitionGraphicsBufferToState(commandListPointer, graphicsBufferPointer, resourceState);
}

void VulkanGraphicsServiceDispatchThreadsInterop(void* context, void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DispatchThreads(commandListPointer, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
}

void VulkanGraphicsServiceBeginRenderPassInterop(void* context, void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->BeginRenderPass(commandListPointer, renderPassDescriptor);
}

void VulkanGraphicsServiceEndRenderPassInterop(void* context, void* commandListPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->EndRenderPass(commandListPointer);
}

void VulkanGraphicsServiceSetPipelineStateInterop(void* context, void* commandListPointer, void* pipelineStatePointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetPipelineState(commandListPointer, pipelineStatePointer);
}

void VulkanGraphicsServiceSetShaderResourceHeapInterop(void* context, void* commandListPointer, void* shaderResourceHeapPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetShaderResourceHeap(commandListPointer, shaderResourceHeapPointer);
}

void VulkanGraphicsServiceSetShaderInterop(void* context, void* commandListPointer, void* shaderPointer)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetShader(commandListPointer, shaderPointer);
}

void VulkanGraphicsServiceSetShaderParameterValuesInterop(void* context, void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->SetShaderParameterValues(commandListPointer, slot, values, valuesLength);
}

void VulkanGraphicsServiceDispatchMeshInterop(void* context, void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->DispatchMesh(commandListPointer, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
}

void VulkanGraphicsServiceExecuteIndirectInterop(void* context, void* commandListPointer, unsigned int maxCommandCount, void* commandGraphicsBufferPointer, unsigned int commandBufferOffset)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ExecuteIndirect(commandListPointer, maxCommandCount, commandGraphicsBufferPointer, commandBufferOffset);
}

void VulkanGraphicsServiceBeginQueryInterop(void* context, void* commandListPointer, void* queryBufferPointer, int index)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->BeginQuery(commandListPointer, queryBufferPointer, index);
}

void VulkanGraphicsServiceEndQueryInterop(void* context, void* commandListPointer, void* queryBufferPointer, int index)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->EndQuery(commandListPointer, queryBufferPointer, index);
}

void VulkanGraphicsServiceResolveQueryDataInterop(void* context, void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex)
{
    auto contextObject = (VulkanGraphicsService*)context;
    contextObject->ResolveQueryData(commandListPointer, queryBufferPointer, destinationBufferPointer, startIndex, endIndex);
}

void InitVulkanGraphicsService(const VulkanGraphicsService* context, GraphicsService* service)
{
    service->Context = (void*)context;
    service->GraphicsService_GetGraphicsAdapterName = VulkanGraphicsServiceGetGraphicsAdapterNameInterop;
    service->GraphicsService_GetBufferAllocationInfos = VulkanGraphicsServiceGetBufferAllocationInfosInterop;
    service->GraphicsService_GetTextureAllocationInfos = VulkanGraphicsServiceGetTextureAllocationInfosInterop;
    service->GraphicsService_CreateCommandQueue = VulkanGraphicsServiceCreateCommandQueueInterop;
    service->GraphicsService_SetCommandQueueLabel = VulkanGraphicsServiceSetCommandQueueLabelInterop;
    service->GraphicsService_DeleteCommandQueue = VulkanGraphicsServiceDeleteCommandQueueInterop;
    service->GraphicsService_ResetCommandQueue = VulkanGraphicsServiceResetCommandQueueInterop;
    service->GraphicsService_GetCommandQueueTimestampFrequency = VulkanGraphicsServiceGetCommandQueueTimestampFrequencyInterop;
    service->GraphicsService_ExecuteCommandLists = VulkanGraphicsServiceExecuteCommandListsInterop;
    service->GraphicsService_WaitForCommandQueueOnCpu = VulkanGraphicsServiceWaitForCommandQueueOnCpuInterop;
    service->GraphicsService_CreateCommandList = VulkanGraphicsServiceCreateCommandListInterop;
    service->GraphicsService_SetCommandListLabel = VulkanGraphicsServiceSetCommandListLabelInterop;
    service->GraphicsService_DeleteCommandList = VulkanGraphicsServiceDeleteCommandListInterop;
    service->GraphicsService_ResetCommandList = VulkanGraphicsServiceResetCommandListInterop;
    service->GraphicsService_CommitCommandList = VulkanGraphicsServiceCommitCommandListInterop;
    service->GraphicsService_CreateGraphicsHeap = VulkanGraphicsServiceCreateGraphicsHeapInterop;
    service->GraphicsService_SetGraphicsHeapLabel = VulkanGraphicsServiceSetGraphicsHeapLabelInterop;
    service->GraphicsService_DeleteGraphicsHeap = VulkanGraphicsServiceDeleteGraphicsHeapInterop;
    service->GraphicsService_CreateShaderResourceHeap = VulkanGraphicsServiceCreateShaderResourceHeapInterop;
    service->GraphicsService_SetShaderResourceHeapLabel = VulkanGraphicsServiceSetShaderResourceHeapLabelInterop;
    service->GraphicsService_DeleteShaderResourceHeap = VulkanGraphicsServiceDeleteShaderResourceHeapInterop;
    service->GraphicsService_CreateShaderResourceTexture = VulkanGraphicsServiceCreateShaderResourceTextureInterop;
    service->GraphicsService_DeleteShaderResourceTexture = VulkanGraphicsServiceDeleteShaderResourceTextureInterop;
    service->GraphicsService_CreateShaderResourceBuffer = VulkanGraphicsServiceCreateShaderResourceBufferInterop;
    service->GraphicsService_DeleteShaderResourceBuffer = VulkanGraphicsServiceDeleteShaderResourceBufferInterop;
    service->GraphicsService_CreateGraphicsBuffer = VulkanGraphicsServiceCreateGraphicsBufferInterop;
    service->GraphicsService_SetGraphicsBufferLabel = VulkanGraphicsServiceSetGraphicsBufferLabelInterop;
    service->GraphicsService_DeleteGraphicsBuffer = VulkanGraphicsServiceDeleteGraphicsBufferInterop;
    service->GraphicsService_GetGraphicsBufferCpuPointer = VulkanGraphicsServiceGetGraphicsBufferCpuPointerInterop;
    service->GraphicsService_ReleaseGraphicsBufferCpuPointer = VulkanGraphicsServiceReleaseGraphicsBufferCpuPointerInterop;
    service->GraphicsService_CreateTexture = VulkanGraphicsServiceCreateTextureInterop;
    service->GraphicsService_SetTextureLabel = VulkanGraphicsServiceSetTextureLabelInterop;
    service->GraphicsService_DeleteTexture = VulkanGraphicsServiceDeleteTextureInterop;
    service->GraphicsService_CreateSwapChain = VulkanGraphicsServiceCreateSwapChainInterop;
    service->GraphicsService_DeleteSwapChain = VulkanGraphicsServiceDeleteSwapChainInterop;
    service->GraphicsService_ResizeSwapChain = VulkanGraphicsServiceResizeSwapChainInterop;
    service->GraphicsService_GetSwapChainBackBufferTexture = VulkanGraphicsServiceGetSwapChainBackBufferTextureInterop;
    service->GraphicsService_PresentSwapChain = VulkanGraphicsServicePresentSwapChainInterop;
    service->GraphicsService_WaitForSwapChainOnCpu = VulkanGraphicsServiceWaitForSwapChainOnCpuInterop;
    service->GraphicsService_CreateQueryBuffer = VulkanGraphicsServiceCreateQueryBufferInterop;
    service->GraphicsService_ResetQueryBuffer = VulkanGraphicsServiceResetQueryBufferInterop;
    service->GraphicsService_SetQueryBufferLabel = VulkanGraphicsServiceSetQueryBufferLabelInterop;
    service->GraphicsService_DeleteQueryBuffer = VulkanGraphicsServiceDeleteQueryBufferInterop;
    service->GraphicsService_CreateShader = VulkanGraphicsServiceCreateShaderInterop;
    service->GraphicsService_SetShaderLabel = VulkanGraphicsServiceSetShaderLabelInterop;
    service->GraphicsService_DeleteShader = VulkanGraphicsServiceDeleteShaderInterop;
    service->GraphicsService_CreateComputePipelineState = VulkanGraphicsServiceCreateComputePipelineStateInterop;
    service->GraphicsService_CreatePipelineState = VulkanGraphicsServiceCreatePipelineStateInterop;
    service->GraphicsService_SetPipelineStateLabel = VulkanGraphicsServiceSetPipelineStateLabelInterop;
    service->GraphicsService_DeletePipelineState = VulkanGraphicsServiceDeletePipelineStateInterop;
    service->GraphicsService_CopyDataToGraphicsBuffer = VulkanGraphicsServiceCopyDataToGraphicsBufferInterop;
    service->GraphicsService_CopyDataToTexture = VulkanGraphicsServiceCopyDataToTextureInterop;
    service->GraphicsService_CopyTexture = VulkanGraphicsServiceCopyTextureInterop;
    service->GraphicsService_TransitionGraphicsBufferToState = VulkanGraphicsServiceTransitionGraphicsBufferToStateInterop;
    service->GraphicsService_DispatchThreads = VulkanGraphicsServiceDispatchThreadsInterop;
    service->GraphicsService_BeginRenderPass = VulkanGraphicsServiceBeginRenderPassInterop;
    service->GraphicsService_EndRenderPass = VulkanGraphicsServiceEndRenderPassInterop;
    service->GraphicsService_SetPipelineState = VulkanGraphicsServiceSetPipelineStateInterop;
    service->GraphicsService_SetShaderResourceHeap = VulkanGraphicsServiceSetShaderResourceHeapInterop;
    service->GraphicsService_SetShader = VulkanGraphicsServiceSetShaderInterop;
    service->GraphicsService_SetShaderParameterValues = VulkanGraphicsServiceSetShaderParameterValuesInterop;
    service->GraphicsService_DispatchMesh = VulkanGraphicsServiceDispatchMeshInterop;
    service->GraphicsService_ExecuteIndirect = VulkanGraphicsServiceExecuteIndirectInterop;
    service->GraphicsService_BeginQuery = VulkanGraphicsServiceBeginQueryInterop;
    service->GraphicsService_EndQuery = VulkanGraphicsServiceEndQueryInterop;
    service->GraphicsService_ResolveQueryData = VulkanGraphicsServiceResolveQueryDataInterop;
}

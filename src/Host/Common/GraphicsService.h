#pragma once
#include "CoreEngine.h"

enum GraphicsServiceHeapType : int
{
    Gpu, 
    Upload, 
    ReadBack
};

enum GraphicsServiceCommandType : int
{
    Render, 
    Copy, 
    Compute, 
    Present
};

enum GraphicsTextureFormat : int
{
    Rgba8UnormSrgb, 
    Bgra8UnormSrgb, 
    Depth32Float, 
    Rgba16Float, 
    R16Float, 
    BC1Srgb, 
    BC2Srgb, 
    BC3Srgb, 
    BC4, 
    BC5, 
    BC6, 
    BC7Srgb, 
    Rgba32Float, 
    Rgba16Unorm
};

enum GraphicsTextureUsage : int
{
    ShaderRead, 
    ShaderWrite, 
    RenderTarget
};

enum GraphicsDepthBufferOperation : int
{
    DepthNone, 
    CompareEqual, 
    CompareGreater, 
    Write, 
    ClearWrite
};

enum GraphicsBlendOperation : int
{
    None, 
    AlphaBlending, 
    AddOneOne, 
    AddOneMinusSourceColor
};

enum GraphicsQueryBufferType : int
{
    Timestamp, 
    CopyTimestamp, 
    GraphicsPipelineStats
};

enum GraphicsPrimitiveType : int
{
    Triangle, 
    Line
};

enum GraphicsResourceState : int
{
    StateDestinationCopy, 
    StateShaderRead, 
    StateCommon
};

struct GraphicsAllocationInfos
{
    int SizeInBytes;
    int Alignment;
};

struct NullableGraphicsAllocationInfos
{
    int HasValue;
    struct GraphicsAllocationInfos Value;
};

struct GraphicsFence
{
    void* CommandQueuePointer;
    unsigned long Value;
};

struct NullableGraphicsFence
{
    int HasValue;
    struct GraphicsFence Value;
};

struct GraphicsRenderPassDescriptor
{
    int IsRenderShader;
    struct Nullableint MultiSampleCount;
    struct NullableIntPtr RenderTarget1TexturePointer;
    struct NullableGraphicsTextureFormat RenderTarget1TextureFormat;
    struct NullableVector4 RenderTarget1ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget1BlendOperation;
    struct NullableIntPtr RenderTarget2TexturePointer;
    struct NullableGraphicsTextureFormat RenderTarget2TextureFormat;
    struct NullableVector4 RenderTarget2ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget2BlendOperation;
    struct NullableIntPtr RenderTarget3TexturePointer;
    struct NullableGraphicsTextureFormat RenderTarget3TextureFormat;
    struct NullableVector4 RenderTarget3ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget3BlendOperation;
    struct NullableIntPtr RenderTarget4TexturePointer;
    struct NullableGraphicsTextureFormat RenderTarget4TextureFormat;
    struct NullableVector4 RenderTarget4ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget4BlendOperation;
    struct NullableIntPtr DepthTexturePointer;
    enum GraphicsDepthBufferOperation DepthBufferOperation;
    int BackfaceCulling;
    enum GraphicsPrimitiveType PrimitiveType;
};

struct NullableGraphicsRenderPassDescriptor
{
    int HasValue;
    struct GraphicsRenderPassDescriptor Value;
};

typedef void (*GraphicsService_GetGraphicsAdapterNamePtr)(void* context, char* output);
typedef struct GraphicsAllocationInfos (*GraphicsService_GetBufferAllocationInfosPtr)(void* context, int sizeInBytes);
typedef struct GraphicsAllocationInfos (*GraphicsService_GetTextureAllocationInfosPtr)(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
typedef void* (*GraphicsService_CreateCommandQueuePtr)(void* context, enum GraphicsServiceCommandType commandQueueType);
typedef void (*GraphicsService_SetCommandQueueLabelPtr)(void* context, void* commandQueuePointer, char* label);
typedef void (*GraphicsService_DeleteCommandQueuePtr)(void* context, void* commandQueuePointer);
typedef void (*GraphicsService_ResetCommandQueuePtr)(void* context, void* commandQueuePointer);
typedef unsigned long (*GraphicsService_GetCommandQueueTimestampFrequencyPtr)(void* context, void* commandQueuePointer);
typedef unsigned long (*GraphicsService_ExecuteCommandListsPtr)(void* context, void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength);
typedef void (*GraphicsService_WaitForCommandQueueOnCpuPtr)(void* context, struct GraphicsFence fenceToWait);
typedef void* (*GraphicsService_CreateCommandListPtr)(void* context, void* commandQueuePointer);
typedef void (*GraphicsService_SetCommandListLabelPtr)(void* context, void* commandListPointer, char* label);
typedef void (*GraphicsService_DeleteCommandListPtr)(void* context, void* commandListPointer);
typedef void (*GraphicsService_ResetCommandListPtr)(void* context, void* commandListPointer);
typedef void (*GraphicsService_CommitCommandListPtr)(void* context, void* commandListPointer);
typedef void* (*GraphicsService_CreateGraphicsHeapPtr)(void* context, enum GraphicsServiceHeapType type, unsigned long sizeInBytes);
typedef void (*GraphicsService_SetGraphicsHeapLabelPtr)(void* context, void* graphicsHeapPointer, char* label);
typedef void (*GraphicsService_DeleteGraphicsHeapPtr)(void* context, void* graphicsHeapPointer);
typedef void* (*GraphicsService_CreateShaderResourceHeapPtr)(void* context, unsigned long length);
typedef void (*GraphicsService_SetShaderResourceHeapLabelPtr)(void* context, void* shaderResourceHeapPointer, char* label);
typedef void (*GraphicsService_DeleteShaderResourceHeapPtr)(void* context, void* shaderResourceHeapPointer);
typedef void (*GraphicsService_CreateShaderResourceTexturePtr)(void* context, void* shaderResourceHeapPointer, unsigned int index, void* texturePointer);
typedef void (*GraphicsService_DeleteShaderResourceTexturePtr)(void* context, void* shaderResourceHeapPointer, unsigned int index);
typedef void (*GraphicsService_CreateShaderResourceBufferPtr)(void* context, void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer);
typedef void (*GraphicsService_DeleteShaderResourceBufferPtr)(void* context, void* shaderResourceHeapPointer, unsigned int index);
typedef void* (*GraphicsService_CreateGraphicsBufferPtr)(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes);
typedef void (*GraphicsService_SetGraphicsBufferLabelPtr)(void* context, void* graphicsBufferPointer, char* label);
typedef void (*GraphicsService_DeleteGraphicsBufferPtr)(void* context, void* graphicsBufferPointer);
typedef void* (*GraphicsService_GetGraphicsBufferCpuPointerPtr)(void* context, void* graphicsBufferPointer);
typedef void (*GraphicsService_ReleaseGraphicsBufferCpuPointerPtr)(void* context, void* graphicsBufferPointer);
typedef void* (*GraphicsService_CreateTexturePtr)(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
typedef void (*GraphicsService_SetTextureLabelPtr)(void* context, void* texturePointer, char* label);
typedef void (*GraphicsService_DeleteTexturePtr)(void* context, void* texturePointer);
typedef void* (*GraphicsService_CreateSwapChainPtr)(void* context, void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat);
typedef void (*GraphicsService_DeleteSwapChainPtr)(void* context, void* swapChainPointer);
typedef void (*GraphicsService_ResizeSwapChainPtr)(void* context, void* swapChainPointer, int width, int height);
typedef void* (*GraphicsService_GetSwapChainBackBufferTexturePtr)(void* context, void* swapChainPointer);
typedef unsigned long (*GraphicsService_PresentSwapChainPtr)(void* context, void* swapChainPointer);
typedef void (*GraphicsService_WaitForSwapChainOnCpuPtr)(void* context, void* swapChainPointer);
typedef void* (*GraphicsService_CreateQueryBufferPtr)(void* context, enum GraphicsQueryBufferType queryBufferType, int length);
typedef void (*GraphicsService_ResetQueryBufferPtr)(void* context, void* queryBufferPointer);
typedef void (*GraphicsService_SetQueryBufferLabelPtr)(void* context, void* queryBufferPointer, char* label);
typedef void (*GraphicsService_DeleteQueryBufferPtr)(void* context, void* queryBufferPointer);
typedef void* (*GraphicsService_CreateShaderPtr)(void* context, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength);
typedef void (*GraphicsService_SetShaderLabelPtr)(void* context, void* shaderPointer, char* label);
typedef void (*GraphicsService_DeleteShaderPtr)(void* context, void* shaderPointer);
typedef void* (*GraphicsService_CreatePipelineStatePtr)(void* context, void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
typedef void (*GraphicsService_SetPipelineStateLabelPtr)(void* context, void* pipelineStatePointer, char* label);
typedef void (*GraphicsService_DeletePipelineStatePtr)(void* context, void* pipelineStatePointer);
typedef void (*GraphicsService_CopyDataToGraphicsBufferPtr)(void* context, void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int length);
typedef void (*GraphicsService_CopyDataToTexturePtr)(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
typedef void (*GraphicsService_CopyTexturePtr)(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer);
typedef void (*GraphicsService_TransitionGraphicsBufferToStatePtr)(void* context, void* commandListPointer, void* graphicsBufferPointer, enum GraphicsResourceState resourceState);
typedef void (*GraphicsService_DispatchThreadsPtr)(void* context, void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);
typedef void (*GraphicsService_BeginRenderPassPtr)(void* context, void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
typedef void (*GraphicsService_EndRenderPassPtr)(void* context, void* commandListPointer);
typedef void (*GraphicsService_SetPipelineStatePtr)(void* context, void* commandListPointer, void* pipelineStatePointer);
typedef void (*GraphicsService_SetShaderResourceHeapPtr)(void* context, void* commandListPointer, void* shaderResourceHeapPointer);
typedef void (*GraphicsService_SetShaderPtr)(void* context, void* commandListPointer, void* shaderPointer);
typedef void (*GraphicsService_SetShaderParameterValuesPtr)(void* context, void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength);
typedef void (*GraphicsService_DispatchMeshPtr)(void* context, void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);
typedef void (*GraphicsService_BeginQueryPtr)(void* context, void* commandListPointer, void* queryBufferPointer, int index);
typedef void (*GraphicsService_EndQueryPtr)(void* context, void* commandListPointer, void* queryBufferPointer, int index);
typedef void (*GraphicsService_ResolveQueryDataPtr)(void* context, void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex);

struct GraphicsService
{
    void* Context;
    GraphicsService_GetGraphicsAdapterNamePtr GraphicsService_GetGraphicsAdapterName;
    GraphicsService_GetBufferAllocationInfosPtr GraphicsService_GetBufferAllocationInfos;
    GraphicsService_GetTextureAllocationInfosPtr GraphicsService_GetTextureAllocationInfos;
    GraphicsService_CreateCommandQueuePtr GraphicsService_CreateCommandQueue;
    GraphicsService_SetCommandQueueLabelPtr GraphicsService_SetCommandQueueLabel;
    GraphicsService_DeleteCommandQueuePtr GraphicsService_DeleteCommandQueue;
    GraphicsService_ResetCommandQueuePtr GraphicsService_ResetCommandQueue;
    GraphicsService_GetCommandQueueTimestampFrequencyPtr GraphicsService_GetCommandQueueTimestampFrequency;
    GraphicsService_ExecuteCommandListsPtr GraphicsService_ExecuteCommandLists;
    GraphicsService_WaitForCommandQueueOnCpuPtr GraphicsService_WaitForCommandQueueOnCpu;
    GraphicsService_CreateCommandListPtr GraphicsService_CreateCommandList;
    GraphicsService_SetCommandListLabelPtr GraphicsService_SetCommandListLabel;
    GraphicsService_DeleteCommandListPtr GraphicsService_DeleteCommandList;
    GraphicsService_ResetCommandListPtr GraphicsService_ResetCommandList;
    GraphicsService_CommitCommandListPtr GraphicsService_CommitCommandList;
    GraphicsService_CreateGraphicsHeapPtr GraphicsService_CreateGraphicsHeap;
    GraphicsService_SetGraphicsHeapLabelPtr GraphicsService_SetGraphicsHeapLabel;
    GraphicsService_DeleteGraphicsHeapPtr GraphicsService_DeleteGraphicsHeap;
    GraphicsService_CreateShaderResourceHeapPtr GraphicsService_CreateShaderResourceHeap;
    GraphicsService_SetShaderResourceHeapLabelPtr GraphicsService_SetShaderResourceHeapLabel;
    GraphicsService_DeleteShaderResourceHeapPtr GraphicsService_DeleteShaderResourceHeap;
    GraphicsService_CreateShaderResourceTexturePtr GraphicsService_CreateShaderResourceTexture;
    GraphicsService_DeleteShaderResourceTexturePtr GraphicsService_DeleteShaderResourceTexture;
    GraphicsService_CreateShaderResourceBufferPtr GraphicsService_CreateShaderResourceBuffer;
    GraphicsService_DeleteShaderResourceBufferPtr GraphicsService_DeleteShaderResourceBuffer;
    GraphicsService_CreateGraphicsBufferPtr GraphicsService_CreateGraphicsBuffer;
    GraphicsService_SetGraphicsBufferLabelPtr GraphicsService_SetGraphicsBufferLabel;
    GraphicsService_DeleteGraphicsBufferPtr GraphicsService_DeleteGraphicsBuffer;
    GraphicsService_GetGraphicsBufferCpuPointerPtr GraphicsService_GetGraphicsBufferCpuPointer;
    GraphicsService_ReleaseGraphicsBufferCpuPointerPtr GraphicsService_ReleaseGraphicsBufferCpuPointer;
    GraphicsService_CreateTexturePtr GraphicsService_CreateTexture;
    GraphicsService_SetTextureLabelPtr GraphicsService_SetTextureLabel;
    GraphicsService_DeleteTexturePtr GraphicsService_DeleteTexture;
    GraphicsService_CreateSwapChainPtr GraphicsService_CreateSwapChain;
    GraphicsService_DeleteSwapChainPtr GraphicsService_DeleteSwapChain;
    GraphicsService_ResizeSwapChainPtr GraphicsService_ResizeSwapChain;
    GraphicsService_GetSwapChainBackBufferTexturePtr GraphicsService_GetSwapChainBackBufferTexture;
    GraphicsService_PresentSwapChainPtr GraphicsService_PresentSwapChain;
    GraphicsService_WaitForSwapChainOnCpuPtr GraphicsService_WaitForSwapChainOnCpu;
    GraphicsService_CreateQueryBufferPtr GraphicsService_CreateQueryBuffer;
    GraphicsService_ResetQueryBufferPtr GraphicsService_ResetQueryBuffer;
    GraphicsService_SetQueryBufferLabelPtr GraphicsService_SetQueryBufferLabel;
    GraphicsService_DeleteQueryBufferPtr GraphicsService_DeleteQueryBuffer;
    GraphicsService_CreateShaderPtr GraphicsService_CreateShader;
    GraphicsService_SetShaderLabelPtr GraphicsService_SetShaderLabel;
    GraphicsService_DeleteShaderPtr GraphicsService_DeleteShader;
    GraphicsService_CreatePipelineStatePtr GraphicsService_CreatePipelineState;
    GraphicsService_SetPipelineStateLabelPtr GraphicsService_SetPipelineStateLabel;
    GraphicsService_DeletePipelineStatePtr GraphicsService_DeletePipelineState;
    GraphicsService_CopyDataToGraphicsBufferPtr GraphicsService_CopyDataToGraphicsBuffer;
    GraphicsService_CopyDataToTexturePtr GraphicsService_CopyDataToTexture;
    GraphicsService_CopyTexturePtr GraphicsService_CopyTexture;
    GraphicsService_TransitionGraphicsBufferToStatePtr GraphicsService_TransitionGraphicsBufferToState;
    GraphicsService_DispatchThreadsPtr GraphicsService_DispatchThreads;
    GraphicsService_BeginRenderPassPtr GraphicsService_BeginRenderPass;
    GraphicsService_EndRenderPassPtr GraphicsService_EndRenderPass;
    GraphicsService_SetPipelineStatePtr GraphicsService_SetPipelineState;
    GraphicsService_SetShaderResourceHeapPtr GraphicsService_SetShaderResourceHeap;
    GraphicsService_SetShaderPtr GraphicsService_SetShader;
    GraphicsService_SetShaderParameterValuesPtr GraphicsService_SetShaderParameterValues;
    GraphicsService_DispatchMeshPtr GraphicsService_DispatchMesh;
    GraphicsService_BeginQueryPtr GraphicsService_BeginQuery;
    GraphicsService_EndQueryPtr GraphicsService_EndQuery;
    GraphicsService_ResolveQueryDataPtr GraphicsService_ResolveQueryData;
};

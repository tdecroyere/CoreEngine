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
    Compute
};

enum GraphicsPrimitiveType : int
{
    Triangle, 
    TriangleStrip, 
    Line
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
    CopyTimestamp
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
typedef struct GraphicsAllocationInfos (*GraphicsService_GetTextureAllocationInfosPtr)(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
typedef void* (*GraphicsService_CreateCommandQueuePtr)(void* context, enum GraphicsServiceCommandType commandQueueType);
typedef void (*GraphicsService_SetCommandQueueLabelPtr)(void* context, void* commandQueuePointer, char* label);
typedef void (*GraphicsService_DeleteCommandQueuePtr)(void* context, void* commandQueuePointer);
typedef void (*GraphicsService_ResetCommandQueuePtr)(void* context, void* commandQueuePointer);
typedef unsigned long (*GraphicsService_GetCommandQueueTimestampFrequencyPtr)(void* context, void* commandQueuePointer);
typedef unsigned long (*GraphicsService_ExecuteCommandListsPtr)(void* context, void* commandQueuePointer, void** commandLists, int commandListsLength, int isAwaitable);
typedef void (*GraphicsService_WaitForCommandQueuePtr)(void* context, void* commandQueuePointer, void* commandQueueToWaitPointer, unsigned long fenceValue);
typedef void (*GraphicsService_WaitForCommandQueueOnCpuPtr)(void* context, void* commandQueueToWaitPointer, unsigned long fenceValue);
typedef void* (*GraphicsService_CreateCommandListPtr)(void* context, void* commandQueuePointer);
typedef void (*GraphicsService_SetCommandListLabelPtr)(void* context, void* commandListPointer, char* label);
typedef void (*GraphicsService_DeleteCommandListPtr)(void* context, void* commandListPointer);
typedef void (*GraphicsService_ResetCommandListPtr)(void* context, void* commandListPointer);
typedef void (*GraphicsService_CommitCommandListPtr)(void* context, void* commandListPointer);
typedef void* (*GraphicsService_CreateGraphicsHeapPtr)(void* context, enum GraphicsServiceHeapType type, unsigned long length);
typedef void (*GraphicsService_SetGraphicsHeapLabelPtr)(void* context, void* graphicsHeapPointer, char* label);
typedef void (*GraphicsService_DeleteGraphicsHeapPtr)(void* context, void* graphicsHeapPointer);
typedef void* (*GraphicsService_CreateGraphicsBufferPtr)(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes);
typedef void (*GraphicsService_SetGraphicsBufferLabelPtr)(void* context, void* graphicsBufferPointer, char* label);
typedef void (*GraphicsService_DeleteGraphicsBufferPtr)(void* context, void* graphicsBufferPointer);
typedef void* (*GraphicsService_GetGraphicsBufferCpuPointerPtr)(void* context, void* graphicsBufferPointer);
typedef void* (*GraphicsService_CreateTexturePtr)(void* context, void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
typedef void (*GraphicsService_SetTextureLabelPtr)(void* context, void* texturePointer, char* label);
typedef void (*GraphicsService_DeleteTexturePtr)(void* context, void* texturePointer);
typedef void* (*GraphicsService_CreateSwapChainPtr)(void* context, void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat);
typedef void* (*GraphicsService_GetSwapChainBackBufferTexturePtr)(void* context, void* swapChainPointer);
typedef unsigned long (*GraphicsService_PresentSwapChainPtr)(void* context, void* swapChainPointer);
typedef void* (*GraphicsService_CreateIndirectCommandBufferPtr)(void* context, int maxCommandCount);
typedef void (*GraphicsService_SetIndirectCommandBufferLabelPtr)(void* context, void* indirectCommandBufferPointer, char* label);
typedef void (*GraphicsService_DeleteIndirectCommandBufferPtr)(void* context, void* indirectCommandBufferPointer);
typedef void* (*GraphicsService_CreateQueryBufferPtr)(void* context, enum GraphicsQueryBufferType queryBufferType, int length);
typedef void (*GraphicsService_SetQueryBufferLabelPtr)(void* context, void* queryBufferPointer, char* label);
typedef void (*GraphicsService_DeleteQueryBufferPtr)(void* context, void* queryBufferPointer);
typedef void* (*GraphicsService_CreateShaderPtr)(void* context, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength);
typedef void (*GraphicsService_SetShaderLabelPtr)(void* context, void* shaderPointer, char* label);
typedef void (*GraphicsService_DeleteShaderPtr)(void* context, void* shaderPointer);
typedef void* (*GraphicsService_CreatePipelineStatePtr)(void* context, void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
typedef void (*GraphicsService_SetPipelineStateLabelPtr)(void* context, void* pipelineStatePointer, char* label);
typedef void (*GraphicsService_DeletePipelineStatePtr)(void* context, void* pipelineStatePointer);
typedef void (*GraphicsService_SetShaderBufferPtr)(void* context, void* commandListPointer, void* graphicsBufferPointer, int slot, int isReadOnly, int index);
typedef void (*GraphicsService_SetShaderBuffersPtr)(void* context, void* commandListPointer, void** graphicsBufferPointerList, int graphicsBufferPointerListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturePtr)(void* context, void* commandListPointer, void* texturePointer, int slot, int isReadOnly, int index);
typedef void (*GraphicsService_SetShaderTexturesPtr)(void* context, void* commandListPointer, void** texturePointerList, int texturePointerListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListPtr)(void* context, void* commandListPointer, void* indirectCommandListPointer, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListsPtr)(void* context, void* commandListPointer, void** indirectCommandListPointerList, int indirectCommandListPointerListLength, int slot, int index);
typedef void (*GraphicsService_CopyDataToGraphicsBufferPtr)(void* context, void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int length);
typedef void (*GraphicsService_CopyDataToTexturePtr)(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
typedef void (*GraphicsService_CopyTexturePtr)(void* context, void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer);
typedef void (*GraphicsService_ResetIndirectCommandListPtr)(void* context, void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount);
typedef void (*GraphicsService_OptimizeIndirectCommandListPtr)(void* context, void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount);
typedef struct Vector3 (*GraphicsService_DispatchThreadsPtr)(void* context, void* commandListPointer, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ);
typedef void (*GraphicsService_BeginRenderPassPtr)(void* context, void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
typedef void (*GraphicsService_EndRenderPassPtr)(void* context, void* commandListPointer);
typedef void (*GraphicsService_SetPipelineStatePtr)(void* context, void* commandListPointer, void* pipelineStatePointer);
typedef void (*GraphicsService_SetShaderPtr)(void* context, void* commandListPointer, void* shaderPointer);
typedef void (*GraphicsService_ExecuteIndirectCommandBufferPtr)(void* context, void* commandListPointer, void* indirectCommandBufferPointer, int maxCommandCount);
typedef void (*GraphicsService_SetIndexBufferPtr)(void* context, void* commandListPointer, void* graphicsBufferPointer);
typedef void (*GraphicsService_DrawIndexedPrimitivesPtr)(void* context, void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
typedef void (*GraphicsService_DrawPrimitivesPtr)(void* context, void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);
typedef void (*GraphicsService_QueryTimestampPtr)(void* context, void* commandListPointer, void* queryBufferPointer, int index);
typedef void (*GraphicsService_ResolveQueryDataPtr)(void* context, void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex);

struct GraphicsService
{
    void* Context;
    GraphicsService_GetGraphicsAdapterNamePtr GraphicsService_GetGraphicsAdapterName;
    GraphicsService_GetTextureAllocationInfosPtr GraphicsService_GetTextureAllocationInfos;
    GraphicsService_CreateCommandQueuePtr GraphicsService_CreateCommandQueue;
    GraphicsService_SetCommandQueueLabelPtr GraphicsService_SetCommandQueueLabel;
    GraphicsService_DeleteCommandQueuePtr GraphicsService_DeleteCommandQueue;
    GraphicsService_ResetCommandQueuePtr GraphicsService_ResetCommandQueue;
    GraphicsService_GetCommandQueueTimestampFrequencyPtr GraphicsService_GetCommandQueueTimestampFrequency;
    GraphicsService_ExecuteCommandListsPtr GraphicsService_ExecuteCommandLists;
    GraphicsService_WaitForCommandQueuePtr GraphicsService_WaitForCommandQueue;
    GraphicsService_WaitForCommandQueueOnCpuPtr GraphicsService_WaitForCommandQueueOnCpu;
    GraphicsService_CreateCommandListPtr GraphicsService_CreateCommandList;
    GraphicsService_SetCommandListLabelPtr GraphicsService_SetCommandListLabel;
    GraphicsService_DeleteCommandListPtr GraphicsService_DeleteCommandList;
    GraphicsService_ResetCommandListPtr GraphicsService_ResetCommandList;
    GraphicsService_CommitCommandListPtr GraphicsService_CommitCommandList;
    GraphicsService_CreateGraphicsHeapPtr GraphicsService_CreateGraphicsHeap;
    GraphicsService_SetGraphicsHeapLabelPtr GraphicsService_SetGraphicsHeapLabel;
    GraphicsService_DeleteGraphicsHeapPtr GraphicsService_DeleteGraphicsHeap;
    GraphicsService_CreateGraphicsBufferPtr GraphicsService_CreateGraphicsBuffer;
    GraphicsService_SetGraphicsBufferLabelPtr GraphicsService_SetGraphicsBufferLabel;
    GraphicsService_DeleteGraphicsBufferPtr GraphicsService_DeleteGraphicsBuffer;
    GraphicsService_GetGraphicsBufferCpuPointerPtr GraphicsService_GetGraphicsBufferCpuPointer;
    GraphicsService_CreateTexturePtr GraphicsService_CreateTexture;
    GraphicsService_SetTextureLabelPtr GraphicsService_SetTextureLabel;
    GraphicsService_DeleteTexturePtr GraphicsService_DeleteTexture;
    GraphicsService_CreateSwapChainPtr GraphicsService_CreateSwapChain;
    GraphicsService_GetSwapChainBackBufferTexturePtr GraphicsService_GetSwapChainBackBufferTexture;
    GraphicsService_PresentSwapChainPtr GraphicsService_PresentSwapChain;
    GraphicsService_CreateIndirectCommandBufferPtr GraphicsService_CreateIndirectCommandBuffer;
    GraphicsService_SetIndirectCommandBufferLabelPtr GraphicsService_SetIndirectCommandBufferLabel;
    GraphicsService_DeleteIndirectCommandBufferPtr GraphicsService_DeleteIndirectCommandBuffer;
    GraphicsService_CreateQueryBufferPtr GraphicsService_CreateQueryBuffer;
    GraphicsService_SetQueryBufferLabelPtr GraphicsService_SetQueryBufferLabel;
    GraphicsService_DeleteQueryBufferPtr GraphicsService_DeleteQueryBuffer;
    GraphicsService_CreateShaderPtr GraphicsService_CreateShader;
    GraphicsService_SetShaderLabelPtr GraphicsService_SetShaderLabel;
    GraphicsService_DeleteShaderPtr GraphicsService_DeleteShader;
    GraphicsService_CreatePipelineStatePtr GraphicsService_CreatePipelineState;
    GraphicsService_SetPipelineStateLabelPtr GraphicsService_SetPipelineStateLabel;
    GraphicsService_DeletePipelineStatePtr GraphicsService_DeletePipelineState;
    GraphicsService_SetShaderBufferPtr GraphicsService_SetShaderBuffer;
    GraphicsService_SetShaderBuffersPtr GraphicsService_SetShaderBuffers;
    GraphicsService_SetShaderTexturePtr GraphicsService_SetShaderTexture;
    GraphicsService_SetShaderTexturesPtr GraphicsService_SetShaderTextures;
    GraphicsService_SetShaderIndirectCommandListPtr GraphicsService_SetShaderIndirectCommandList;
    GraphicsService_SetShaderIndirectCommandListsPtr GraphicsService_SetShaderIndirectCommandLists;
    GraphicsService_CopyDataToGraphicsBufferPtr GraphicsService_CopyDataToGraphicsBuffer;
    GraphicsService_CopyDataToTexturePtr GraphicsService_CopyDataToTexture;
    GraphicsService_CopyTexturePtr GraphicsService_CopyTexture;
    GraphicsService_ResetIndirectCommandListPtr GraphicsService_ResetIndirectCommandList;
    GraphicsService_OptimizeIndirectCommandListPtr GraphicsService_OptimizeIndirectCommandList;
    GraphicsService_DispatchThreadsPtr GraphicsService_DispatchThreads;
    GraphicsService_BeginRenderPassPtr GraphicsService_BeginRenderPass;
    GraphicsService_EndRenderPassPtr GraphicsService_EndRenderPass;
    GraphicsService_SetPipelineStatePtr GraphicsService_SetPipelineState;
    GraphicsService_SetShaderPtr GraphicsService_SetShader;
    GraphicsService_ExecuteIndirectCommandBufferPtr GraphicsService_ExecuteIndirectCommandBuffer;
    GraphicsService_SetIndexBufferPtr GraphicsService_SetIndexBuffer;
    GraphicsService_DrawIndexedPrimitivesPtr GraphicsService_DrawIndexedPrimitives;
    GraphicsService_DrawPrimitivesPtr GraphicsService_DrawPrimitives;
    GraphicsService_QueryTimestampPtr GraphicsService_QueryTimestamp;
    GraphicsService_ResolveQueryDataPtr GraphicsService_ResolveQueryData;
};

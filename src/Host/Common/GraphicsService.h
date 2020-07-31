#pragma once
#include "CoreEngine.h"

enum GraphicsCommandBufferType : int
{
    RenderOld, 
    CopyOld, 
    ComputeOld
};

enum GraphicsServiceHeapType : int
{
    Gpu, 
    Upload, 
    ReadBack
};

enum GraphicsCommandType : int
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

enum GraphicsCommandBufferState : int
{
    Created, 
    Committed, 
    Scheduled, 
    Completed, 
    Error
};

enum GraphicsQueryBufferType : int
{
    Timestamp
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
    struct Nullableuint RenderTarget1TextureId;
    struct NullableGraphicsTextureFormat RenderTarget1TextureFormat;
    struct NullableVector4 RenderTarget1ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget1BlendOperation;
    struct Nullableuint RenderTarget2TextureId;
    struct NullableGraphicsTextureFormat RenderTarget2TextureFormat;
    struct NullableVector4 RenderTarget2ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget2BlendOperation;
    struct Nullableuint RenderTarget3TextureId;
    struct NullableGraphicsTextureFormat RenderTarget3TextureFormat;
    struct NullableVector4 RenderTarget3ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget3BlendOperation;
    struct Nullableuint RenderTarget4TextureId;
    struct NullableGraphicsTextureFormat RenderTarget4TextureFormat;
    struct NullableVector4 RenderTarget4ClearColor;
    struct NullableGraphicsBlendOperation RenderTarget4BlendOperation;
    struct Nullableuint DepthTextureId;
    enum GraphicsDepthBufferOperation DepthBufferOperation;
    int BackfaceCulling;
    enum GraphicsPrimitiveType PrimitiveType;
};

struct NullableGraphicsRenderPassDescriptor
{
    int HasValue;
    struct GraphicsRenderPassDescriptor Value;
};

struct GraphicsCommandBufferStatus
{
    enum GraphicsCommandBufferState State;
    double ScheduledStartTime;
    double ScheduledEndTime;
    double ExecutionStartTime;
    double ExecutionEndTime;
    struct Nullableint ErrorCode;
    char* ErrorMessage;
};

struct NullableGraphicsCommandBufferStatus
{
    int HasValue;
    struct GraphicsCommandBufferStatus Value;
};

typedef void (*GraphicsService_GetGraphicsAdapterNamePtr)(void* context, char* output);
typedef struct Vector2 (*GraphicsService_GetRenderSizePtr)(void* context);
typedef struct GraphicsAllocationInfos (*GraphicsService_GetTextureAllocationInfosPtr)(void* context, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
typedef int (*GraphicsService_CreateGraphicsHeapPtr)(void* context, unsigned int graphicsHeapId, enum GraphicsServiceHeapType type, unsigned long length);
typedef void (*GraphicsService_SetGraphicsHeapLabelPtr)(void* context, unsigned int graphicsHeapId, char* label);
typedef void (*GraphicsService_DeleteGraphicsHeapPtr)(void* context, unsigned int graphicsHeapId);
typedef int (*GraphicsService_CreateGraphicsBufferPtr)(void* context, unsigned int graphicsBufferId, unsigned int graphicsHeapId, unsigned long heapOffset, int isAliasable, int sizeInBytes);
typedef void (*GraphicsService_SetGraphicsBufferLabelPtr)(void* context, unsigned int graphicsBufferId, char* label);
typedef void (*GraphicsService_DeleteGraphicsBufferPtr)(void* context, unsigned int graphicsBufferId);
typedef void* (*GraphicsService_GetGraphicsBufferCpuPointerPtr)(void* context, unsigned int graphicsBufferId);
typedef int (*GraphicsService_CreateTexturePtr)(void* context, unsigned int textureId, unsigned int graphicsHeapId, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
typedef void (*GraphicsService_SetTextureLabelPtr)(void* context, unsigned int textureId, char* label);
typedef void (*GraphicsService_DeleteTexturePtr)(void* context, unsigned int textureId);
typedef int (*GraphicsService_CreateIndirectCommandBufferPtr)(void* context, unsigned int indirectCommandBufferId, int maxCommandCount);
typedef void (*GraphicsService_SetIndirectCommandBufferLabelPtr)(void* context, unsigned int indirectCommandBufferId, char* label);
typedef void (*GraphicsService_DeleteIndirectCommandBufferPtr)(void* context, unsigned int indirectCommandBufferId);
typedef int (*GraphicsService_CreateShaderPtr)(void* context, unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength);
typedef void (*GraphicsService_SetShaderLabelPtr)(void* context, unsigned int shaderId, char* label);
typedef void (*GraphicsService_DeleteShaderPtr)(void* context, unsigned int shaderId);
typedef int (*GraphicsService_CreatePipelineStatePtr)(void* context, unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor);
typedef void (*GraphicsService_SetPipelineStateLabelPtr)(void* context, unsigned int pipelineStateId, char* label);
typedef void (*GraphicsService_DeletePipelineStatePtr)(void* context, unsigned int pipelineStateId);
typedef int (*GraphicsService_CreateCommandQueuePtr)(void* context, unsigned int commandQueueId, enum GraphicsCommandType commandQueueType);
typedef void (*GraphicsService_SetCommandQueueLabelPtr)(void* context, unsigned int commandQueueId, char* label);
typedef void (*GraphicsService_DeleteCommandQueuePtr)(void* context, unsigned int commandQueueId);
typedef unsigned long (*GraphicsService_GetCommandQueueTimestampFrequencyPtr)(void* context, unsigned int commandQueueId);
typedef unsigned long (*GraphicsService_ExecuteCommandListsPtr)(void* context, unsigned int commandQueueId, unsigned int* commandLists, int commandListsLength, int isAwaitable);
typedef void (*GraphicsService_WaitForCommandQueuePtr)(void* context, unsigned int commandQueueId, unsigned int commandQueueToWaitId, unsigned long fenceValue);
typedef int (*GraphicsService_CreateCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandQueueId, enum GraphicsCommandType commandListType);
typedef void (*GraphicsService_SetCommandListLabelPtr)(void* context, unsigned int commandListId, char* label);
typedef void (*GraphicsService_DeleteCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_ResetCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_CommitCommandListPtr)(void* context, unsigned int commandListId);
typedef int (*GraphicsService_CreateQueryBufferPtr)(void* context, unsigned int queryBufferId, enum GraphicsQueryBufferType queryBufferType, int length);
typedef void (*GraphicsService_SetQueryBufferLabelPtr)(void* context, unsigned int queryBufferId, char* label);
typedef void (*GraphicsService_DeleteQueryBufferPtr)(void* context, unsigned int queryBufferId);
typedef int (*GraphicsService_CreateCommandBufferPtr)(void* context, unsigned int commandBufferId, enum GraphicsCommandBufferType commandBufferType, char* label);
typedef void (*GraphicsService_DeleteCommandBufferPtr)(void* context, unsigned int commandBufferId);
typedef void (*GraphicsService_ResetCommandBufferPtr)(void* context, unsigned int commandBufferId);
typedef void (*GraphicsService_ExecuteCommandBufferPtr)(void* context, unsigned int commandBufferId);
typedef void (*GraphicsService_SetShaderBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index);
typedef void (*GraphicsService_SetShaderBuffersPtr)(void* context, unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index);
typedef void (*GraphicsService_SetShaderTexturesPtr)(void* context, unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListsPtr)(void* context, unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index);
typedef int (*GraphicsService_CreateCopyCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandBufferId, char* label);
typedef void (*GraphicsService_CommitCopyCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_CopyDataToGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int destinationGraphicsBufferId, unsigned int sourceGraphicsBufferId, int length);
typedef void (*GraphicsService_CopyDataToTexturePtr)(void* context, unsigned int commandListId, unsigned int destinationTextureId, unsigned int sourceGraphicsBufferId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
typedef void (*GraphicsService_CopyTexturePtr)(void* context, unsigned int commandListId, unsigned int destinationTextureId, unsigned int sourceTextureId);
typedef void (*GraphicsService_ResetIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);
typedef void (*GraphicsService_OptimizeIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);
typedef int (*GraphicsService_CreateComputeCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandBufferId, char* label);
typedef void (*GraphicsService_CommitComputeCommandListPtr)(void* context, unsigned int commandListId);
typedef struct Vector3 (*GraphicsService_DispatchThreadsPtr)(void* context, unsigned int commandListId, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ);
typedef int (*GraphicsService_CreateRenderCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandBufferId, struct GraphicsRenderPassDescriptor renderDescriptor, char* label);
typedef void (*GraphicsService_CommitRenderCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_SetPipelineStatePtr)(void* context, unsigned int commandListId, unsigned int pipelineStateId);
typedef void (*GraphicsService_SetShaderPtr)(void* context, unsigned int commandListId, unsigned int shaderId);
typedef void (*GraphicsService_ExecuteIndirectCommandBufferPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount);
typedef void (*GraphicsService_SetIndexBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId);
typedef void (*GraphicsService_DrawIndexedPrimitivesPtr)(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
typedef void (*GraphicsService_DrawPrimitivesPtr)(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);
typedef void (*GraphicsService_QueryTimestampPtr)(void* context, unsigned int commandListId, unsigned int queryBufferId, int index);
typedef void (*GraphicsService_ResolveQueryDataPtr)(void* context, unsigned int commandListId, unsigned int queryBufferId, unsigned int destinationBufferId, int startIndex, int endIndex);
typedef void (*GraphicsService_WaitForCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandListToWaitId);
typedef void (*GraphicsService_PresentScreenBufferPtr)(void* context, unsigned int commandBufferId);
typedef void (*GraphicsService_WaitForAvailableScreenBufferPtr)(void* context);

struct GraphicsService
{
    void* Context;
    GraphicsService_GetGraphicsAdapterNamePtr GraphicsService_GetGraphicsAdapterName;
    GraphicsService_GetRenderSizePtr GraphicsService_GetRenderSize;
    GraphicsService_GetTextureAllocationInfosPtr GraphicsService_GetTextureAllocationInfos;
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
    GraphicsService_CreateIndirectCommandBufferPtr GraphicsService_CreateIndirectCommandBuffer;
    GraphicsService_SetIndirectCommandBufferLabelPtr GraphicsService_SetIndirectCommandBufferLabel;
    GraphicsService_DeleteIndirectCommandBufferPtr GraphicsService_DeleteIndirectCommandBuffer;
    GraphicsService_CreateShaderPtr GraphicsService_CreateShader;
    GraphicsService_SetShaderLabelPtr GraphicsService_SetShaderLabel;
    GraphicsService_DeleteShaderPtr GraphicsService_DeleteShader;
    GraphicsService_CreatePipelineStatePtr GraphicsService_CreatePipelineState;
    GraphicsService_SetPipelineStateLabelPtr GraphicsService_SetPipelineStateLabel;
    GraphicsService_DeletePipelineStatePtr GraphicsService_DeletePipelineState;
    GraphicsService_CreateCommandQueuePtr GraphicsService_CreateCommandQueue;
    GraphicsService_SetCommandQueueLabelPtr GraphicsService_SetCommandQueueLabel;
    GraphicsService_DeleteCommandQueuePtr GraphicsService_DeleteCommandQueue;
    GraphicsService_GetCommandQueueTimestampFrequencyPtr GraphicsService_GetCommandQueueTimestampFrequency;
    GraphicsService_ExecuteCommandListsPtr GraphicsService_ExecuteCommandLists;
    GraphicsService_WaitForCommandQueuePtr GraphicsService_WaitForCommandQueue;
    GraphicsService_CreateCommandListPtr GraphicsService_CreateCommandList;
    GraphicsService_SetCommandListLabelPtr GraphicsService_SetCommandListLabel;
    GraphicsService_DeleteCommandListPtr GraphicsService_DeleteCommandList;
    GraphicsService_ResetCommandListPtr GraphicsService_ResetCommandList;
    GraphicsService_CommitCommandListPtr GraphicsService_CommitCommandList;
    GraphicsService_CreateQueryBufferPtr GraphicsService_CreateQueryBuffer;
    GraphicsService_SetQueryBufferLabelPtr GraphicsService_SetQueryBufferLabel;
    GraphicsService_DeleteQueryBufferPtr GraphicsService_DeleteQueryBuffer;
    GraphicsService_CreateCommandBufferPtr GraphicsService_CreateCommandBuffer;
    GraphicsService_DeleteCommandBufferPtr GraphicsService_DeleteCommandBuffer;
    GraphicsService_ResetCommandBufferPtr GraphicsService_ResetCommandBuffer;
    GraphicsService_ExecuteCommandBufferPtr GraphicsService_ExecuteCommandBuffer;
    GraphicsService_SetShaderBufferPtr GraphicsService_SetShaderBuffer;
    GraphicsService_SetShaderBuffersPtr GraphicsService_SetShaderBuffers;
    GraphicsService_SetShaderTexturePtr GraphicsService_SetShaderTexture;
    GraphicsService_SetShaderTexturesPtr GraphicsService_SetShaderTextures;
    GraphicsService_SetShaderIndirectCommandListPtr GraphicsService_SetShaderIndirectCommandList;
    GraphicsService_SetShaderIndirectCommandListsPtr GraphicsService_SetShaderIndirectCommandLists;
    GraphicsService_CreateCopyCommandListPtr GraphicsService_CreateCopyCommandList;
    GraphicsService_CommitCopyCommandListPtr GraphicsService_CommitCopyCommandList;
    GraphicsService_CopyDataToGraphicsBufferPtr GraphicsService_CopyDataToGraphicsBuffer;
    GraphicsService_CopyDataToTexturePtr GraphicsService_CopyDataToTexture;
    GraphicsService_CopyTexturePtr GraphicsService_CopyTexture;
    GraphicsService_ResetIndirectCommandListPtr GraphicsService_ResetIndirectCommandList;
    GraphicsService_OptimizeIndirectCommandListPtr GraphicsService_OptimizeIndirectCommandList;
    GraphicsService_CreateComputeCommandListPtr GraphicsService_CreateComputeCommandList;
    GraphicsService_CommitComputeCommandListPtr GraphicsService_CommitComputeCommandList;
    GraphicsService_DispatchThreadsPtr GraphicsService_DispatchThreads;
    GraphicsService_CreateRenderCommandListPtr GraphicsService_CreateRenderCommandList;
    GraphicsService_CommitRenderCommandListPtr GraphicsService_CommitRenderCommandList;
    GraphicsService_SetPipelineStatePtr GraphicsService_SetPipelineState;
    GraphicsService_SetShaderPtr GraphicsService_SetShader;
    GraphicsService_ExecuteIndirectCommandBufferPtr GraphicsService_ExecuteIndirectCommandBuffer;
    GraphicsService_SetIndexBufferPtr GraphicsService_SetIndexBuffer;
    GraphicsService_DrawIndexedPrimitivesPtr GraphicsService_DrawIndexedPrimitives;
    GraphicsService_DrawPrimitivesPtr GraphicsService_DrawPrimitives;
    GraphicsService_QueryTimestampPtr GraphicsService_QueryTimestamp;
    GraphicsService_ResolveQueryDataPtr GraphicsService_ResolveQueryData;
    GraphicsService_WaitForCommandListPtr GraphicsService_WaitForCommandList;
    GraphicsService_PresentScreenBufferPtr GraphicsService_PresentScreenBuffer;
    GraphicsService_WaitForAvailableScreenBufferPtr GraphicsService_WaitForAvailableScreenBuffer;
};

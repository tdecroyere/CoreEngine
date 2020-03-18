#pragma once
#include "CoreEngine.h"

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

enum GraphicsDepthBufferOperation : int
{
    DepthNone, 
    CompareEqual, 
    CompareLess, 
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

};

typedef int (*GraphicsService_GetGpuErrorPtr)(void* context);
typedef float (*GraphicsService_GetGpuExecutionTimePtr)(void* context, unsigned int commandListId);
typedef struct Vector2 (*GraphicsService_GetRenderSizePtr)(void* context);
typedef char* (*GraphicsService_GetGraphicsAdapterNamePtr)(void* context);
typedef int (*GraphicsService_CreateGraphicsBufferPtr)(void* context, unsigned int graphicsBufferId, int length, int isWriteOnly, char* label);
typedef int (*GraphicsService_CreateTexturePtr)(void* context, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, char* label);
typedef void (*GraphicsService_RemoveTexturePtr)(void* context, unsigned int textureId);
typedef int (*GraphicsService_CreateIndirectCommandBufferPtr)(void* context, unsigned int indirectCommandBufferId, int maxCommandCount, char* label);
typedef int (*GraphicsService_CreateShaderPtr)(void* context, unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, char* label);
typedef void (*GraphicsService_RemoveShaderPtr)(void* context, unsigned int shaderId);
typedef int (*GraphicsService_CreatePipelineStatePtr)(void* context, unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor, char* label);
typedef void (*GraphicsService_RemovePipelineStatePtr)(void* context, unsigned int pipelineStateId);
typedef int (*GraphicsService_CreateCommandBufferPtr)(void* context, unsigned int commandBufferId, char* label);
typedef void (*GraphicsService_ExecuteCommandBufferPtr)(void* context, unsigned int commandBufferId);
typedef void (*GraphicsService_SetShaderBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index);
typedef void (*GraphicsService_SetShaderBuffersPtr)(void* context, unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index);
typedef void (*GraphicsService_SetShaderTexturesPtr)(void* context, unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListsPtr)(void* context, unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index);
typedef int (*GraphicsService_CreateCopyCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandBufferId, char* label);
typedef void (*GraphicsService_CommitCopyCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_UploadDataToGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength);
typedef void (*GraphicsService_CopyGraphicsBufferDataToCpuPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int length);
typedef void (*GraphicsService_ReadGraphicsBufferDataPtr)(void* context, unsigned int graphicsBufferId, void* data, int dataLength);
typedef void (*GraphicsService_UploadDataToTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, void* data, int dataLength);
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
typedef void (*GraphicsService_WaitForCommandListPtr)(void* context, unsigned int commandListId, unsigned int commandListToWaitId);
typedef void (*GraphicsService_PresentScreenBufferPtr)(void* context, unsigned int commandBufferId);
typedef void (*GraphicsService_WaitForAvailableScreenBufferPtr)(void* context);

struct GraphicsService
{
    void* Context;
    GraphicsService_GetGpuErrorPtr GraphicsService_GetGpuError;
    GraphicsService_GetGpuExecutionTimePtr GraphicsService_GetGpuExecutionTime;
    GraphicsService_GetRenderSizePtr GraphicsService_GetRenderSize;
    GraphicsService_GetGraphicsAdapterNamePtr GraphicsService_GetGraphicsAdapterName;
    GraphicsService_CreateGraphicsBufferPtr GraphicsService_CreateGraphicsBuffer;
    GraphicsService_CreateTexturePtr GraphicsService_CreateTexture;
    GraphicsService_RemoveTexturePtr GraphicsService_RemoveTexture;
    GraphicsService_CreateIndirectCommandBufferPtr GraphicsService_CreateIndirectCommandBuffer;
    GraphicsService_CreateShaderPtr GraphicsService_CreateShader;
    GraphicsService_RemoveShaderPtr GraphicsService_RemoveShader;
    GraphicsService_CreatePipelineStatePtr GraphicsService_CreatePipelineState;
    GraphicsService_RemovePipelineStatePtr GraphicsService_RemovePipelineState;
    GraphicsService_CreateCommandBufferPtr GraphicsService_CreateCommandBuffer;
    GraphicsService_ExecuteCommandBufferPtr GraphicsService_ExecuteCommandBuffer;
    GraphicsService_SetShaderBufferPtr GraphicsService_SetShaderBuffer;
    GraphicsService_SetShaderBuffersPtr GraphicsService_SetShaderBuffers;
    GraphicsService_SetShaderTexturePtr GraphicsService_SetShaderTexture;
    GraphicsService_SetShaderTexturesPtr GraphicsService_SetShaderTextures;
    GraphicsService_SetShaderIndirectCommandListPtr GraphicsService_SetShaderIndirectCommandList;
    GraphicsService_SetShaderIndirectCommandListsPtr GraphicsService_SetShaderIndirectCommandLists;
    GraphicsService_CreateCopyCommandListPtr GraphicsService_CreateCopyCommandList;
    GraphicsService_CommitCopyCommandListPtr GraphicsService_CommitCopyCommandList;
    GraphicsService_UploadDataToGraphicsBufferPtr GraphicsService_UploadDataToGraphicsBuffer;
    GraphicsService_CopyGraphicsBufferDataToCpuPtr GraphicsService_CopyGraphicsBufferDataToCpu;
    GraphicsService_ReadGraphicsBufferDataPtr GraphicsService_ReadGraphicsBufferData;
    GraphicsService_UploadDataToTexturePtr GraphicsService_UploadDataToTexture;
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
    GraphicsService_WaitForCommandListPtr GraphicsService_WaitForCommandList;
    GraphicsService_PresentScreenBufferPtr GraphicsService_PresentScreenBuffer;
    GraphicsService_WaitForAvailableScreenBufferPtr GraphicsService_WaitForAvailableScreenBuffer;
};

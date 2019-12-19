#pragma once
#include "CoreEngine.h"

enum GraphicsPrimitiveType : int
{
    Triangle, 
    Line
};

enum GraphicsTextureFormat : int
{
    Rgba8UnormSrgb, 
    Bgra8UnormSrgb, 
    Depth32Float
};

struct GraphicsRenderPassDescriptor
{
    struct Nullableuint ColorTextureId;
    struct NullableVector4 ClearColor;
    struct Nullableuint DepthTextureId;
    int DepthCompare;
    int DepthWrite;
    int BackfaceCulling;

};

typedef struct Vector2 (*GraphicsService_GetRenderSizePtr)(void* context);
typedef int (*GraphicsService_CreateGraphicsBufferPtr)(void* context, unsigned int graphicsBufferId, int length, char* debugName);
typedef int (*GraphicsService_CreateTexturePtr)(void* context, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int isRenderTarget, char* debugName);
typedef void (*GraphicsService_RemoveTexturePtr)(void* context, unsigned int textureId);
typedef int (*GraphicsService_CreateShaderPtr)(void* context, unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, int useDepthBuffer, char* debugName);
typedef void (*GraphicsService_RemoveShaderPtr)(void* context, unsigned int shaderId);
typedef int (*GraphicsService_CreateCopyCommandListPtr)(void* context, unsigned int commandListId, char* debugName, int createNewCommandBuffer);
typedef void (*GraphicsService_ExecuteCopyCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_UploadDataToGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength);
typedef void (*GraphicsService_UploadDataToTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int width, int height, void* data, int dataLength);
typedef void (*GraphicsService_ResetIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);
typedef void (*GraphicsService_OptimizeIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);
typedef int (*GraphicsService_CreateComputeCommandListPtr)(void* context, unsigned int commandListId, char* debugName, int createNewCommandBuffer);
typedef void (*GraphicsService_ExecuteComputeCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_DispatchThreadGroupsPtr)(void* context, unsigned int commandListId, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);
typedef int (*GraphicsService_CreateRenderCommandListPtr)(void* context, unsigned int commandListId, struct GraphicsRenderPassDescriptor renderDescriptor, char* debugName, int createNewCommandBuffer);
typedef void (*GraphicsService_ExecuteRenderCommandListPtr)(void* context, unsigned int commandListId);
typedef int (*GraphicsService_CreateIndirectCommandListPtr)(void* context, unsigned int commandListId, int maxCommandCount, char* debugName);
typedef void (*GraphicsService_SetShaderPtr)(void* context, unsigned int commandListId, unsigned int shaderId);
typedef void (*GraphicsService_SetShaderBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int index);
typedef void (*GraphicsService_SetShaderBuffersPtr)(void* context, unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturesPtr)(void* context, unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index);
typedef void (*GraphicsService_ExecuteIndirectCommandListPtr)(void* context, unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);
typedef void (*GraphicsService_SetIndexBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId);
typedef void (*GraphicsService_DrawIndexedPrimitivesPtr)(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
typedef void (*GraphicsService_PresentScreenBufferPtr)(void* context);

struct GraphicsService
{
    void* Context;
    GraphicsService_GetRenderSizePtr GraphicsService_GetRenderSize;
    GraphicsService_CreateGraphicsBufferPtr GraphicsService_CreateGraphicsBuffer;
    GraphicsService_CreateTexturePtr GraphicsService_CreateTexture;
    GraphicsService_RemoveTexturePtr GraphicsService_RemoveTexture;
    GraphicsService_CreateShaderPtr GraphicsService_CreateShader;
    GraphicsService_RemoveShaderPtr GraphicsService_RemoveShader;
    GraphicsService_CreateCopyCommandListPtr GraphicsService_CreateCopyCommandList;
    GraphicsService_ExecuteCopyCommandListPtr GraphicsService_ExecuteCopyCommandList;
    GraphicsService_UploadDataToGraphicsBufferPtr GraphicsService_UploadDataToGraphicsBuffer;
    GraphicsService_UploadDataToTexturePtr GraphicsService_UploadDataToTexture;
    GraphicsService_ResetIndirectCommandListPtr GraphicsService_ResetIndirectCommandList;
    GraphicsService_OptimizeIndirectCommandListPtr GraphicsService_OptimizeIndirectCommandList;
    GraphicsService_CreateComputeCommandListPtr GraphicsService_CreateComputeCommandList;
    GraphicsService_ExecuteComputeCommandListPtr GraphicsService_ExecuteComputeCommandList;
    GraphicsService_DispatchThreadGroupsPtr GraphicsService_DispatchThreadGroups;
    GraphicsService_CreateRenderCommandListPtr GraphicsService_CreateRenderCommandList;
    GraphicsService_ExecuteRenderCommandListPtr GraphicsService_ExecuteRenderCommandList;
    GraphicsService_CreateIndirectCommandListPtr GraphicsService_CreateIndirectCommandList;
    GraphicsService_SetShaderPtr GraphicsService_SetShader;
    GraphicsService_SetShaderBufferPtr GraphicsService_SetShaderBuffer;
    GraphicsService_SetShaderBuffersPtr GraphicsService_SetShaderBuffers;
    GraphicsService_SetShaderTexturePtr GraphicsService_SetShaderTexture;
    GraphicsService_SetShaderTexturesPtr GraphicsService_SetShaderTextures;
    GraphicsService_SetShaderIndirectCommandListPtr GraphicsService_SetShaderIndirectCommandList;
    GraphicsService_ExecuteIndirectCommandListPtr GraphicsService_ExecuteIndirectCommandList;
    GraphicsService_SetIndexBufferPtr GraphicsService_SetIndexBuffer;
    GraphicsService_DrawIndexedPrimitivesPtr GraphicsService_DrawIndexedPrimitives;
    GraphicsService_PresentScreenBufferPtr GraphicsService_PresentScreenBuffer;
};

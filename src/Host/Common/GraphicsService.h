#pragma once
#include "CoreEngine.h"

enum GraphicsPrimitiveType : int
{
    Triangle, 
    Line
};

typedef struct Vector2 (*GraphicsService_GetRenderSizePtr)(void* context);
typedef int (*GraphicsService_CreateGraphicsBufferPtr)(void* context, unsigned int graphicsBufferId, int length, char* debugName);
typedef int (*GraphicsService_CreateTexturePtr)(void* context, unsigned int textureId, int width, int height, char* debugName);
typedef int (*GraphicsService_CreateShaderPtr)(void* context, unsigned int shaderId, void* shaderByteCode, int shaderByteCodeLength, char* debugName);
typedef void (*GraphicsService_RemoveShaderPtr)(void* context, unsigned int shaderId);
typedef int (*GraphicsService_CreateCopyCommandListPtr)(void* context, unsigned int commandListId, char* debugName, int createNewCommandBuffer);
typedef void (*GraphicsService_ExecuteCopyCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_UploadDataToGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength);
typedef void (*GraphicsService_UploadDataToTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int width, int height, void* data, int dataLength);
typedef int (*GraphicsService_CreateRenderCommandListPtr)(void* context, unsigned int commandListId, char* debugName, int createNewCommandBuffer);
typedef void (*GraphicsService_ExecuteRenderCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*GraphicsService_SetShaderPtr)(void* context, unsigned int commandListId, unsigned int shaderId);
typedef void (*GraphicsService_SetShaderBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, int slot, int index);
typedef void (*GraphicsService_SetShaderBuffersPtr)(void* context, unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int slot, int index);
typedef void (*GraphicsService_SetShaderTexturesPtr)(void* context, unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index);
typedef void (*GraphicsService_SetIndexBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId);
typedef void (*GraphicsService_DrawIndexedPrimitivesPtr)(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
typedef void (*GraphicsService_PresentScreenBufferPtr)(void* context);

struct GraphicsService
{
    void* Context;
    GraphicsService_GetRenderSizePtr GraphicsService_GetRenderSize;
    GraphicsService_CreateGraphicsBufferPtr GraphicsService_CreateGraphicsBuffer;
    GraphicsService_CreateTexturePtr GraphicsService_CreateTexture;
    GraphicsService_CreateShaderPtr GraphicsService_CreateShader;
    GraphicsService_RemoveShaderPtr GraphicsService_RemoveShader;
    GraphicsService_CreateCopyCommandListPtr GraphicsService_CreateCopyCommandList;
    GraphicsService_ExecuteCopyCommandListPtr GraphicsService_ExecuteCopyCommandList;
    GraphicsService_UploadDataToGraphicsBufferPtr GraphicsService_UploadDataToGraphicsBuffer;
    GraphicsService_UploadDataToTexturePtr GraphicsService_UploadDataToTexture;
    GraphicsService_CreateRenderCommandListPtr GraphicsService_CreateRenderCommandList;
    GraphicsService_ExecuteRenderCommandListPtr GraphicsService_ExecuteRenderCommandList;
    GraphicsService_SetShaderPtr GraphicsService_SetShader;
    GraphicsService_SetShaderBufferPtr GraphicsService_SetShaderBuffer;
    GraphicsService_SetShaderBuffersPtr GraphicsService_SetShaderBuffers;
    GraphicsService_SetShaderTexturePtr GraphicsService_SetShaderTexture;
    GraphicsService_SetShaderTexturesPtr GraphicsService_SetShaderTextures;
    GraphicsService_SetIndexBufferPtr GraphicsService_SetIndexBuffer;
    GraphicsService_DrawIndexedPrimitivesPtr GraphicsService_DrawIndexedPrimitives;
    GraphicsService_PresentScreenBufferPtr GraphicsService_PresentScreenBuffer;
};

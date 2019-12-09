#pragma once
#include "CoreEngine.h"

enum GraphicsBindStage : int
{
    Vertex, 
    Pixel
};

enum GraphicsPrimitiveType : int
{
    Triangle, 
    Line
};

enum GraphicsShaderParameterType : int
{
    Buffer, 
    Texture, 
    TextureArray
};

struct GraphicsShaderParameterDescriptor
{
    unsigned int GraphicsResourceId;
    enum GraphicsShaderParameterType ParameterType;
    unsigned int Slot;

};

typedef struct Vector2 (*GetRenderSizePtr)(void* context);
typedef int (*CreateGraphicsBufferPtr)(void* context, unsigned int graphicsResourceId, int length);
typedef int (*CreateTexturePtr)(void* context, unsigned int graphicsResourceId, int width, int height);
typedef unsigned int (*CreatePipelineStatePtr)(void* context, void* shaderByteCode, int shaderByteCodeLength);
typedef void (*RemovePipelineStatePtr)(void* context, unsigned int pipelineStateId);
typedef int (*CreateShaderParametersPtr)(void* context, unsigned int graphicsResourceId, unsigned int pipelineStateId, struct GraphicsShaderParameterDescriptor* parameters, int parametersLength);
typedef unsigned int (*CreateCopyCommandListPtr)(void* context);
typedef void (*ExecuteCopyCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*UploadDataToGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength);
typedef void (*UploadDataToTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, int width, int height, void* data, int dataLength);
typedef unsigned int (*CreateRenderCommandListPtr)(void* context);
typedef void (*ExecuteRenderCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*SetPipelineStatePtr)(void* context, unsigned int commandListId, unsigned int pipelineStateId);
typedef void (*SetGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, enum GraphicsBindStage graphicsBindStage, unsigned int slot);
typedef void (*SetTexturePtr)(void* context, unsigned int commandListId, unsigned int textureId, enum GraphicsBindStage graphicsBindStage, unsigned int slot);
typedef void (*DrawPrimitivesPtr)(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, int instanceCount, int baseInstanceId);
typedef void (*PresentScreenBufferPtr)(void* context);

struct GraphicsService
{
    void* Context;
    GetRenderSizePtr GetRenderSize;
    CreateGraphicsBufferPtr CreateGraphicsBuffer;
    CreateTexturePtr CreateTexture;
    CreatePipelineStatePtr CreatePipelineState;
    RemovePipelineStatePtr RemovePipelineState;
    CreateShaderParametersPtr CreateShaderParameters;
    CreateCopyCommandListPtr CreateCopyCommandList;
    ExecuteCopyCommandListPtr ExecuteCopyCommandList;
    UploadDataToGraphicsBufferPtr UploadDataToGraphicsBuffer;
    UploadDataToTexturePtr UploadDataToTexture;
    CreateRenderCommandListPtr CreateRenderCommandList;
    ExecuteRenderCommandListPtr ExecuteRenderCommandList;
    SetPipelineStatePtr SetPipelineState;
    SetGraphicsBufferPtr SetGraphicsBuffer;
    SetTexturePtr SetTexture;
    DrawPrimitivesPtr DrawPrimitives;
    PresentScreenBufferPtr PresentScreenBuffer;
};

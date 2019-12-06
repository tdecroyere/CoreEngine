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

typedef struct Vector2 (*GetRenderSizePtr)(void* context);
typedef unsigned int (*CreatePipelineStatePtr)(void* context, void* shaderByteCode, int shaderByteCodeLength);
typedef void (*RemovePipelineStatePtr)(void* context, unsigned int pipelineStateId);
typedef unsigned int (*CreateShaderParametersPtr)(void* context, unsigned int pipelineStateId, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3);
typedef unsigned int (*CreateGraphicsBufferPtr)(void* context, int length);
typedef unsigned int (*CreateCopyCommandListPtr)(void* context);
typedef void (*ExecuteCopyCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*UploadDataToGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength);
typedef unsigned int (*CreateRenderCommandListPtr)(void* context);
typedef void (*ExecuteRenderCommandListPtr)(void* context, unsigned int commandListId);
typedef void (*SetPipelineStatePtr)(void* context, unsigned int commandListId, unsigned int pipelineStateId);
typedef void (*SetGraphicsBufferPtr)(void* context, unsigned int commandListId, unsigned int graphicsBufferId, enum GraphicsBindStage graphicsBindStage, unsigned int slot);
typedef void (*DrawPrimitivesPtr)(void* context, unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId);

struct GraphicsService
{
    void* Context;
    GetRenderSizePtr GetRenderSize;
    CreatePipelineStatePtr CreatePipelineState;
    RemovePipelineStatePtr RemovePipelineState;
    CreateShaderParametersPtr CreateShaderParameters;
    CreateGraphicsBufferPtr CreateGraphicsBuffer;
    CreateCopyCommandListPtr CreateCopyCommandList;
    ExecuteCopyCommandListPtr ExecuteCopyCommandList;
    UploadDataToGraphicsBufferPtr UploadDataToGraphicsBuffer;
    CreateRenderCommandListPtr CreateRenderCommandList;
    ExecuteRenderCommandListPtr ExecuteRenderCommandList;
    SetPipelineStatePtr SetPipelineState;
    SetGraphicsBufferPtr SetGraphicsBuffer;
    DrawPrimitivesPtr DrawPrimitives;
};

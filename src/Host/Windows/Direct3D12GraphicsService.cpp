#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"

using namespace std;

struct Vector2 Direct3D12GraphicsService::GetRenderSize()
{
    return Vector2 { 1280, 720 };
}

void Direct3D12GraphicsService::GetGraphicsAdapterName(char* output)
{ 
    // strcpyW(output, "Test");
    string("DirectX12 Not Implemented").copy(output, 5);
}

int Direct3D12GraphicsService::CreateGraphicsBuffer(unsigned int graphicsBufferId, int length, int isWriteOnly, char* label)
{ 
    return 1;
}

int Direct3D12GraphicsService::CreateTexture(unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::DeleteTexture(unsigned int textureId)
{ 

}

int Direct3D12GraphicsService::CreateIndirectCommandBuffer(unsigned int indirectCommandBufferId, int maxCommandCount, char* label)
{ 
    return 1;
}

int Direct3D12GraphicsService::CreateShader(unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::DeleteShader(unsigned int shaderId)
{ 

}

int Direct3D12GraphicsService::CreatePipelineState(unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::DeletePipelineState(unsigned int pipelineStateId)
{ 

}

int Direct3D12GraphicsService::CreateCommandBuffer(unsigned int commandBufferId, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::DeleteCommandBuffer(unsigned int commandBufferId)
{ 

}

void Direct3D12GraphicsService::ResetCommandBuffer(unsigned int commandBufferId)
{ 

}

void Direct3D12GraphicsService::ExecuteCommandBuffer(unsigned int commandBufferId)
{ 

}

NullableGraphicsCommandBufferStatus Direct3D12GraphicsService::GetCommandBufferStatus(unsigned int commandBufferId)
{ 
    auto status = NullableGraphicsCommandBufferStatus {};

    status.HasValue = 1;
    status.Value.State = GraphicsCommandBufferState::Completed;

    return status; 
}

void Direct3D12GraphicsService::SetShaderBuffer(unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index){ }
void Direct3D12GraphicsService::SetShaderBuffers(unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index){ }
void Direct3D12GraphicsService::SetShaderTexture(unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index){ }
void Direct3D12GraphicsService::SetShaderTextures(unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index){ }
void Direct3D12GraphicsService::SetShaderIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index){ }
void Direct3D12GraphicsService::SetShaderIndirectCommandLists(unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index){ }

int Direct3D12GraphicsService::CreateCopyCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::CommitCopyCommandList(unsigned int commandListId){ }
void Direct3D12GraphicsService::UploadDataToGraphicsBuffer(unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength){ }
void Direct3D12GraphicsService::CopyGraphicsBufferDataToCpu(unsigned int commandListId, unsigned int graphicsBufferId, int length){ }
void Direct3D12GraphicsService::ReadGraphicsBufferData(unsigned int graphicsBufferId, void* data, int dataLength){ }
void Direct3D12GraphicsService::UploadDataToTexture(unsigned int commandListId, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, void* data, int dataLength){ }
void Direct3D12GraphicsService::ResetIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount){ }
void Direct3D12GraphicsService::OptimizeIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount){ }

int Direct3D12GraphicsService::CreateComputeCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::CommitComputeCommandList(unsigned int commandListId){ }

struct Vector3 Direct3D12GraphicsService::DispatchThreads(unsigned int commandListId, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ)
{ 
    return Vector3 { 1, 1, 1 };
}

int Direct3D12GraphicsService::CreateRenderCommandList(unsigned int commandListId, unsigned int commandBufferId, struct GraphicsRenderPassDescriptor renderDescriptor, char* label)
{ 
    return 1;
}

void Direct3D12GraphicsService::CommitRenderCommandList(unsigned int commandListId){ }
void Direct3D12GraphicsService::SetPipelineState(unsigned int commandListId, unsigned int pipelineStateId){ }
void Direct3D12GraphicsService::SetShader(unsigned int commandListId, unsigned int shaderId){ }
void Direct3D12GraphicsService::ExecuteIndirectCommandBuffer(unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount){ }
void Direct3D12GraphicsService::SetIndexBuffer(unsigned int commandListId, unsigned int graphicsBufferId){ }
void Direct3D12GraphicsService::DrawIndexedPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId){ }
void Direct3D12GraphicsService::DrawPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount){ }

void Direct3D12GraphicsService::WaitForCommandList(unsigned int commandListId, unsigned int commandListToWaitId){ }
void Direct3D12GraphicsService::PresentScreenBuffer(unsigned int commandBufferId){ }
void Direct3D12GraphicsService::WaitForAvailableScreenBuffer(){ }
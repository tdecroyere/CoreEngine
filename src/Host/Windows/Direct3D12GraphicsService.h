#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

using namespace std;
using namespace Microsoft::WRL;

static const int RenderBuffersCount = 2;

class Direct3D12GraphicsService
{
    public:
        Direct3D12GraphicsService(HWND window, int width, int height);
        ~Direct3D12GraphicsService();

        struct Vector2 GetRenderSize();
        void GetGraphicsAdapterName(char* output);
        
        int CreateGraphicsBuffer(unsigned int graphicsBufferId, int length, int isWriteOnly, char* label);
        int CreateTexture(unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, char* label);
        void DeleteTexture(unsigned int textureId);
        int CreateIndirectCommandBuffer(unsigned int indirectCommandBufferId, int maxCommandCount, char* label);
        int CreateShader(unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, char* label);
        void DeleteShader(unsigned int shaderId);
        int CreatePipelineState(unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor, char* label);
        void DeletePipelineState(unsigned int pipelineStateId);

        int CreateCommandBuffer(unsigned int commandBufferId, enum GraphicsCommandBufferType commandBufferType, char* label);
        void DeleteCommandBuffer(unsigned int commandBufferId);
        void ResetCommandBuffer(unsigned int commandBufferId);
        void ExecuteCommandBuffer(unsigned int commandBufferId);
        NullableGraphicsCommandBufferStatus GetCommandBufferStatus(unsigned int commandBufferId);

        void SetShaderBuffer(unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index);
        void SetShaderBuffers(unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
        void SetShaderTexture(unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index);
        void SetShaderTextures(unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index);
        void SetShaderIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index);
        void SetShaderIndirectCommandLists(unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index);

        int CreateCopyCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label);
        void CommitCopyCommandList(unsigned int commandListId);
        void UploadDataToGraphicsBuffer(unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength);
        void CopyGraphicsBufferDataToCpu(unsigned int commandListId, unsigned int graphicsBufferId, int length);
        void ReadGraphicsBufferData(unsigned int graphicsBufferId, void* data, int dataLength);
        void UploadDataToTexture(unsigned int commandListId, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, void* data, int dataLength);
        void ResetIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);
        void OptimizeIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount);

        int CreateComputeCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label);
        void CommitComputeCommandList(unsigned int commandListId);
        struct Vector3 DispatchThreads(unsigned int commandListId, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ);

        int CreateRenderCommandList(unsigned int commandListId, unsigned int commandBufferId, struct GraphicsRenderPassDescriptor renderDescriptor, char* label);
        void CommitRenderCommandList(unsigned int commandListId);
        void SetPipelineState(unsigned int commandListId, unsigned int pipelineStateId);
        void SetShader(unsigned int commandListId, unsigned int shaderId);
        void ExecuteIndirectCommandBuffer(unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount);
        void SetIndexBuffer(unsigned int commandListId, unsigned int graphicsBufferId);
        void DrawIndexedPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
        void DrawPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);

        void WaitForCommandList(unsigned int commandListId, unsigned int commandListToWaitId);
        void PresentScreenBuffer(unsigned int commandBufferId);
        void WaitForAvailableScreenBuffer();

    private:
        // Device objects
        wstring adapterName;
        ComPtr<ID3D12Device3> graphicsDevice;
        ComPtr<ID3D12CommandQueue> directCommandQueue;
        ComPtr<ID3D12CommandQueue> copyCommandQueue;
        ComPtr<ID3D12CommandQueue> computeCommandQueue;

        // Swap chain objects
        ComPtr<IDXGISwapChain3> swapChain;
        ComPtr<ID3D12Resource> backBufferRenderTargets[RenderBuffersCount];
        ComPtr<ID3D12DescriptorHeap> rtvDescriptorHeap;
        int rtvDescriptorHandleSize;
        int currentBackBufferIndex;

        // Synchronization objects
        ComPtr<ID3D12Fence1> globalFence;
        uint64_t globalFrameFenceValues[RenderBuffersCount] = {};
        uint64_t globalFenceValue;
        HANDLE globalFenceEvent;

        // Command buffer objects
        map<unsigned int, ComPtr<ID3D12CommandAllocator>> commandBuffers;
        map<unsigned int, ComPtr<ID3D12GraphicsCommandList>> commandLists;
        stack<ComPtr<ID3D12GraphicsCommandList>> copyCommandListCache;
        stack<ComPtr<ID3D12GraphicsCommandList>> renderCommandListCache;
        stack<ComPtr<ID3D12GraphicsCommandList>> computeCommandListCache;

        // Heap objects
        ComPtr<ID3D12Heap> uploadHeap;
        uint64_t currentUploadHeapOffset;
        ComPtr<ID3D12Heap> globalHeap;
        uint64_t currentGlobalHeapOffset;

        // Buffers
        map<unsigned int, ComPtr<ID3D12Resource>> cpuBuffers;
        map<unsigned int, ComPtr<ID3D12Resource>> gpuBuffers;

        void EnableDebugLayer();
        ComPtr<IDXGIAdapter4> FindGraphicsAdapter(const ComPtr<IDXGIFactory4> dxgiFactory);
        bool CreateDevice(const ComPtr<IDXGIFactory4> dxgiFactory, const ComPtr<IDXGIAdapter4> graphicsAdapter);
        bool CreateOrResizeSwapChain(const ComPtr<IDXGIFactory4> dxgiFactory, HWND window, int width, int height);
        bool CreateHeaps();

        D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetViewHandle();
        
        ComPtr<ID3D12GraphicsCommandList> CreateOrResetCommandList(unsigned int commandBufferId, char* label, D3D12_COMMAND_LIST_TYPE listType);
        void ReleaseCommandList(ComPtr<ID3D12GraphicsCommandList> commandList, D3D12_COMMAND_LIST_TYPE listType);
};
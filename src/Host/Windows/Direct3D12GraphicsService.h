#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

using namespace std;
using namespace Microsoft::WRL;

static const int RenderBuffersCount = 2;
static const int CommandAllocatorsCount = 2;
static const int QueryHeapMaxSize = 1000;

struct GameState
{
	bool GameRunning;
};

struct Shader
{
    ComPtr<ID3DBlob> VertexShaderMethod;
    ComPtr<ID3DBlob> PixelShaderMethod;
    ComPtr<ID3DBlob> ComputeShaderMethod;
    ComPtr<ID3D12RootSignature> RootSignature;
};

class Direct3D12GraphicsService
{
    public:
        Direct3D12GraphicsService(HWND window, int width, int height, GameState* gameState);
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

        bool CreateOrResizeSwapChain(int width, int height);
        void WaitForGlobalFence(bool waitForAllPendingWork);

    private:
        GameState* gameState;

        // Device objects
        wstring adapterName;
        HWND window;
        ComPtr<IDXGIFactory4> dxgiFactory; 
        ComPtr<ID3D12Device3> graphicsDevice;
        ComPtr<ID3D12CommandQueue> directCommandQueue;
        ComPtr<ID3D12CommandQueue> copyCommandQueue;
        ComPtr<ID3D12CommandQueue> computeCommandQueue;
        bool isPresentBarrier = false;

        // Timing objects
        ComPtr<ID3D12QueryHeap> queryHeap;
        ComPtr<ID3D12QueryHeap> copyQueryHeap;
        uint32_t startQueryIndex = 0;
        uint32_t startCopyQueryIndex = 0;
        uint32_t queryHeapIndex = 0;
        uint32_t copyQueryHeapIndex = 0;
        uint64_t* currentCpuQueryHeap;
        uint64_t* currentCpuCopyQueryHeap;
        uint64_t directQueueFrequency = 0;
        uint64_t computeQueueFrequency = 0;
        uint64_t copyQueueFrequency = 0;

        // Swap chain objects
        ComPtr<IDXGISwapChain3> swapChain;
        ComPtr<ID3D12Resource> backBufferRenderTargets[RenderBuffersCount];
        ComPtr<ID3D12DescriptorHeap> rtvDescriptorHeap;
        int32_t rtvDescriptorHandleSize;
        int32_t currentBackBufferIndex;
        Vector2 currentRenderSize;

        // Synchronization objects
        ComPtr<ID3D12Fence1> directFence;
        ComPtr<ID3D12Fence1> copyFence;
        ComPtr<ID3D12Fence1> computeFence;
        uint64_t directFenceValue = 0;
        uint64_t copyFenceValue = 0;
        uint64_t computeFenceValue = 0;
        HANDLE globalFenceEvent;
        bool isWaitingForGlobalFence;
        uint64_t presentFences[RenderBuffersCount];

        // Command buffer objects
        ComPtr<ID3D12CommandAllocator> directCommandAllocators[CommandAllocatorsCount] = {};
        ComPtr<ID3D12CommandAllocator> copyCommandAllocators[CommandAllocatorsCount] = {};
        ComPtr<ID3D12CommandAllocator> computeCommandAllocators[CommandAllocatorsCount] = {};
        int32_t currentAllocatorIndex = 0;

        // TODO: Merge that into one structure
        map<uint32_t, ComPtr<ID3D12GraphicsCommandList>> commandBuffers;
        map<uint32_t, D3D12_COMMAND_LIST_TYPE> commandBufferTypes;
        map<uint32_t, wstring> commandBufferLabels;
        map<uint32_t, uint32_t> commandListBuffers;
        map<uint32_t, GraphicsRenderPassDescriptor> commandListRenderPassDescriptors;
        map<uint32_t, uint64_t> commandBufferFenceValues;
        map<uint32_t, uint32_t> commandBufferStartQueryIndex;
        map<uint32_t, uint32_t> commandBufferEndQueryIndex;

        // Heap objects
        ComPtr<ID3D12Heap> uploadHeap;
        uint64_t currentUploadHeapOffset = 0;
        ComPtr<ID3D12Heap> readBackHeap;
        uint64_t currentReadBackHeapOffset = 0;
        ComPtr<ID3D12Heap> globalHeap;
        uint64_t currentGlobalHeapOffset;

        ComPtr<ID3D12DescriptorHeap> globalDescriptorHeap;
        uint32_t globalDescriptorHandleSize;
        uint32_t currentGlobalDescriptorOffset;

        ComPtr<ID3D12DescriptorHeap> globalRtvDescriptorHeap;
        uint32_t globalRtvDescriptorHandleSize;
        uint32_t currentGlobalRtvDescriptorOffset;

        // Buffers
        map<uint32_t, ComPtr<ID3D12Resource>> cpuBuffers;
        map<uint32_t, ComPtr<ID3D12Resource>> readBackBuffers;
        map<uint32_t, ComPtr<ID3D12Resource>> gpuBuffers;
        map<uint32_t, ComPtr<ID3D12DescriptorHeap>> bufferDescriptorHeaps;
        map<uint32_t, uint32_t> uavBufferDescriptorOffets;
        map<uint32_t, D3D12_RESOURCE_STATES> bufferResourceStates;
        map<uint32_t, ComPtr<ID3D12CommandSignature>> indirectCommandBufferSignatures;

        // Textures
        map<uint32_t, ComPtr<ID3D12Resource>> cpuTextures;
        map<uint32_t, ComPtr<ID3D12Resource>> gpuTextures;
        map<uint32_t, D3D12_PLACED_SUBRESOURCE_FOOTPRINT> textureFootPrints;
        map<uint32_t, uint32_t> textureDescriptorOffets;
        map<uint32_t, uint32_t> srvtextureDescriptorOffets;
        map<uint32_t, uint32_t> uavTextureDescriptorOffets;
        map<uint32_t, D3D12_RESOURCE_STATES> textureResourceStates;

        // Shaders
        map<uint32_t, Shader> shaders;
        map<uint32_t, ComPtr<ID3D12PipelineState>> pipelineStates;
        bool shaderBound;
        Shader currentShaderIndirectCommand = {}; // TODO: To remove

        map<uint32_t, ComPtr<ID3D12DescriptorHeap>> debugDescriptorHeaps;


        void EnableDebugLayer();
        ComPtr<IDXGIAdapter4> FindGraphicsAdapter(const ComPtr<IDXGIFactory4> dxgiFactory);
        bool CreateDevice(const ComPtr<IDXGIFactory4> dxgiFactory, const ComPtr<IDXGIAdapter4> graphicsAdapter);
        bool CreateHeaps();

        D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetViewHandle();
        void TransitionTextureToState(uint32_t commandListId, uint32_t textureId, D3D12_RESOURCE_STATES destinationState);
        void TransitionBufferToState(uint32_t commandListId, uint32_t bufferId, D3D12_RESOURCE_STATES destinationState);
        DXGI_FORMAT ConvertTextureFormat(GraphicsTextureFormat textureFormat);

        void InitGpuProfiling();
};
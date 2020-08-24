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

struct Direct3D12GraphicsHeap
{
    ComPtr<ID3D12Heap> HeapObject;
    GraphicsServiceHeapType Type;
};

struct Direct3D12GraphicsBuffer
{
    ComPtr<ID3D12Resource> BufferObject;
    GraphicsServiceHeapType Type;
    D3D12_RESOURCE_DESC ResourceDesc;
    D3D12_RESOURCE_STATES ResourceState;
    void* CpuPointer;
};

struct Direct3D12Texture
{
    ComPtr<ID3D12Resource> TextureObject;
    D3D12_RESOURCE_DESC ResourceDesc;
    D3D12_RESOURCE_STATES ResourceState;
    D3D12_PLACED_SUBRESOURCE_FOOTPRINT FootPrint;
    uint32_t TextureDescriptorOffset;
    uint32_t SrvTextureDescriptorOffset;
    uint32_t UavTextureDescriptorOffset;
};

struct Direct3D12IndirectCommandBuffer
{
    ComPtr<ID3D12CommandSignature> CommandSignature;
};

struct Direct3D12QueryBuffer
{
    ComPtr<ID3D12QueryHeap> QueryBufferObject;
    D3D12_QUERY_HEAP_TYPE Type;
};

struct Direct3D12Shader
{
    ComPtr<ID3DBlob> VertexShaderMethod;
    ComPtr<ID3DBlob> PixelShaderMethod;
    ComPtr<ID3DBlob> ComputeShaderMethod;
    ComPtr<ID3D12RootSignature> RootSignature;
};

struct Direct3D12PipelineState
{
    ComPtr<ID3D12PipelineState> PipelineStateObject;
};

struct Direct3D12CommandQueue
{
    ComPtr<ID3D12CommandQueue> CommandQueueObject;
    ComPtr<ID3D12CommandAllocator>* CommandAllocators;
    D3D12_COMMAND_LIST_TYPE Type;
    ComPtr<ID3D12Fence1> Fence;
    uint64_t FenceValue;
};

struct Direct3D12CommandList
{
    ComPtr<ID3D12GraphicsCommandList> CommandListObject;
    D3D12_COMMAND_LIST_TYPE Type;
    Direct3D12CommandQueue* CommandQueue;
    GraphicsRenderPassDescriptor RenderPassDescriptor;
};

class Direct3D12GraphicsService
{
    public:
        Direct3D12GraphicsService(HWND window, int width, int height, GameState* gameState);
        ~Direct3D12GraphicsService();

        struct Vector2 GetRenderSize();
        void GetGraphicsAdapterName(char* output);
        GraphicsAllocationInfos GetTextureAllocationInfos(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
        
        void* CreateGraphicsHeap(enum GraphicsServiceHeapType type, unsigned long sizeInBytes);
        void SetGraphicsHeapLabel(void* graphicsHeapPointer, char* label);
        void DeleteGraphicsHeap(void* graphicsHeapPointer);

        void* CreateGraphicsBuffer(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes);
        void SetGraphicsBufferLabel(void* graphicsBufferPointer, char* label);
        void DeleteGraphicsBuffer(void* graphicsBufferPointer);
        void* GetGraphicsBufferCpuPointer(void* graphicsBufferPointer);

        void* CreateTexture(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
        void SetTextureLabel(void* texturePointer, char* label);
        void DeleteTexture(void* texturePointer);

        void* CreateIndirectCommandBuffer(int maxCommandCount);
        void SetIndirectCommandBufferLabel(void* indirectCommandBufferPointer, char* label);
        void DeleteIndirectCommandBuffer(void* indirectCommandBufferPointer);

        void* CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length);
        void SetQueryBufferLabel(void* queryBufferPointer, char* label);
        void DeleteQueryBuffer(void* queryBufferPointer);

        void* CreateShader(char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength);
        void SetShaderLabel(void* shaderPointer, char* label);
        void DeleteShader(void* shaderPointer);

        void* CreatePipelineState(void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
        void SetPipelineStateLabel(void* pipelineStatePointer, char* label);
        void DeletePipelineState(void* pipelineStatePointer);

        void* CreateCommandQueue(enum GraphicsServiceCommandType commandQueueType);
        void SetCommandQueueLabel(void* commandQueuePointer, char* label);
        void DeleteCommandQueue(void* commandQueuePointer);
        unsigned long GetCommandQueueTimestampFrequency(void* commandQueuePointer);
        unsigned long ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, int isAwaitable);
        void WaitForCommandQueue(void* commandQueuePointer, void* commandQueueToWaitPointer, unsigned long fenceValue);
        void WaitForCommandQueueOnCpu(void* commandQueueToWaitPointer, unsigned long fenceValue);

        void* CreateCommandList(void* commandQueuePointer);
        void SetCommandListLabel(void* commandListPointer, char* label);
        void DeleteCommandList(void* commandListPointer);
        void ResetCommandList(void* commandListPointer);
        void CommitCommandList(void* commandListPointer);

        void SetShaderBuffer(void* commandListPointer, void* graphicsBufferPointer, int slot, int isReadOnly, int index);
        void SetShaderBuffers(void* commandListPointer, void** graphicsBufferPointerList, int graphicsBufferPointerListLength, int slot, int index);
        void SetShaderTexture(void* commandListPointer, void* texturePointer, int slot, int isReadOnly, int index);
        void SetShaderTextures(void* commandListPointer, void** texturePointerList, int texturePointerListLength, int slot, int index);
        void SetShaderIndirectCommandList(void* commandListPointer, void* indirectCommandListPointer, int slot, int index);
        void SetShaderIndirectCommandLists(void* commandListPointer, void** indirectCommandListPointerList, int indirectCommandListPointerListLength, int slot, int index);

        void CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int sizeInBytes);
        void CopyDataToTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
        void CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer);
        void ResetIndirectCommandList(void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount);
        void OptimizeIndirectCommandList(void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount);

        struct Vector3 DispatchThreads(void* commandListPointer, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ);

        void BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
        void EndRenderPass(void* commandListPointer);

        void SetPipelineState(void* commandListPointer, void* pipelineStatePointer);
        void SetShader(void* commandListPointer, void* shaderPointer);
        void ExecuteIndirectCommandBuffer(void* commandListPointer, void* indirectCommandBufferPointer, int maxCommandCount);
        void SetIndexBuffer(void* commandListPointer, void* graphicsBufferPointer);
        void DrawIndexedPrimitives(void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
        void DrawPrimitives(void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);

        void QueryTimestamp(void* commandListPointer, void* queryBufferPointer, int index);
        void ResolveQueryData(void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex);

        void PresentScreenBuffer(void* commandListPointer);
        void WaitForAvailableScreenBuffer();

        bool CreateOrResizeSwapChain(int width, int height);
        void WaitForGlobalFence(bool waitForAllPendingWork);

    private:

        // TODO: To Delete
        GameState* gameState;
        ComPtr<ID3D12CommandQueue> directCommandQueue;
        ComPtr<ID3D12CommandQueue> copyCommandQueue;
        ComPtr<ID3D12CommandQueue> computeCommandQueue;
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
        ComPtr<ID3D12Fence1> directFence;
        ComPtr<ID3D12Fence1> copyFence;
        ComPtr<ID3D12Fence1> computeFence;
        uint64_t directFenceValue = 0;
        uint64_t copyFenceValue = 0;
        uint64_t computeFenceValue = 0;
        ComPtr<ID3D12CommandAllocator> directCommandAllocators[CommandAllocatorsCount] = {};
        ComPtr<ID3D12CommandAllocator> copyCommandAllocators[CommandAllocatorsCount] = {};
        ComPtr<ID3D12CommandAllocator> computeCommandAllocators[CommandAllocatorsCount] = {};
        ComPtr<ID3D12Heap> uploadHeap;
        uint64_t currentUploadHeapOffset = 0;
        ComPtr<ID3D12Heap> readBackHeap;
        uint64_t currentReadBackHeapOffset = 0;
        ComPtr<ID3D12Heap> globalHeap;
        uint64_t currentGlobalHeapOffset;
        map<uint32_t, ComPtr<ID3D12Resource>> cpuBuffers;
        map<uint32_t, ComPtr<ID3D12Resource>> readBackBuffers;
        map<uint32_t, ComPtr<ID3D12Resource>> cpuTextures;
        map<uint32_t, ComPtr<ID3D12GraphicsCommandList>> commandBuffers;
        map<uint32_t, D3D12_COMMAND_LIST_TYPE> commandBufferTypes;
        map<uint32_t, wstring> commandBufferLabels;
        map<uint32_t, uint32_t> commandListBuffers;
        map<uint32_t, uint64_t> commandBufferFenceValues;
        map<uint32_t, uint32_t> commandBufferStartQueryIndex;
        map<uint32_t, uint32_t> commandBufferEndQueryIndex;

        // Device objects
        wstring adapterName;
        HWND window;
        ComPtr<IDXGIFactory4> dxgiFactory; 
        ComPtr<ID3D12Device3> graphicsDevice;
        
        bool isPresentBarrier = false;

        // Command Objects
        int32_t currentAllocatorIndex = 0;

        // Swap chain objects
        ComPtr<IDXGISwapChain3> swapChain;
        ComPtr<ID3D12Resource> backBufferRenderTargets[RenderBuffersCount];
        ComPtr<ID3D12DescriptorHeap> rtvDescriptorHeap;
        int32_t rtvDescriptorHandleSize;
        int32_t currentBackBufferIndex;
        Vector2 currentRenderSize;

        // Synchronization objects
        HANDLE globalFenceEvent;
        bool isWaitingForGlobalFence;
        uint64_t presentFences[RenderBuffersCount];

        // Heap objects
        ComPtr<ID3D12DescriptorHeap> globalDescriptorHeap;
        uint32_t globalDescriptorHandleSize;
        uint32_t currentGlobalDescriptorOffset;

        ComPtr<ID3D12DescriptorHeap> globalRtvDescriptorHeap;
        uint32_t globalRtvDescriptorHandleSize;
        uint32_t currentGlobalRtvDescriptorOffset;

        // Buffers        
        map<uint32_t, ComPtr<ID3D12DescriptorHeap>> bufferDescriptorHeaps;
        map<uint32_t, uint32_t> uavBufferDescriptorOffets;

        // Shaders
        bool shaderBound;
        Direct3D12Shader currentShaderIndirectCommand = {}; // TODO: To remove

        map<uint32_t, ComPtr<ID3D12DescriptorHeap>> debugDescriptorHeaps;

        void EnableDebugLayer();
        ComPtr<IDXGIAdapter4> FindGraphicsAdapter(const ComPtr<IDXGIFactory4> dxgiFactory);
        bool CreateDevice(const ComPtr<IDXGIFactory4> dxgiFactory, const ComPtr<IDXGIAdapter4> graphicsAdapter);
        bool CreateHeaps();

        D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetViewHandle();
        void TransitionTextureToState(Direct3D12CommandList* commandList, Direct3D12Texture* texture, D3D12_RESOURCE_STATES destinationState);
        void TransitionBufferToState(Direct3D12CommandList* commandList, Direct3D12GraphicsBuffer* graphicsBuffer, D3D12_RESOURCE_STATES destinationState);
};
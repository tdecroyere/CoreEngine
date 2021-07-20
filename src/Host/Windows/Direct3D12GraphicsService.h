#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

using namespace std;
using namespace Microsoft::WRL;

extern "C" { _declspec(dllexport) extern const UINT D3D12SDKVersion = 4;}
extern "C" { _declspec(dllexport) extern const char* D3D12SDKPath = u8".\\D3D12\\"; }

static const int RenderBuffersCount = 2;
static const int FramesCount = 2;
static const int CommandAllocatorsCount = 2;
static const int QueryHeapMaxSize = 1000;

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
    ComPtr<ID3D12GraphicsCommandList6> CommandListObject;
    D3D12_COMMAND_LIST_TYPE Type;
    Direct3D12CommandQueue* CommandQueue;
    GraphicsRenderPassDescriptor RenderPassDescriptor;
};

struct Direct3D12GraphicsHeap
{
    ComPtr<ID3D12Heap> HeapObject;
    GraphicsServiceHeapType Type;
};

struct Direct3D12ShaderResourceHeap
{
    ComPtr<ID3D12DescriptorHeap> HeapObject;
    UINT HandleSize;
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
    bool IsPresentTexture;
};

struct Direct3D12QueryBuffer
{
    ComPtr<ID3D12QueryHeap> QueryBufferObject;
    D3D12_QUERY_HEAP_TYPE Type;
};

struct Direct3D12CommandBuffer
{
};

struct Direct3D12Shader
{
    ComPtr<ID3DBlob> AmplificationShaderMethod;
    ComPtr<ID3DBlob> MeshShaderMethod;
    ComPtr<ID3DBlob> PixelShaderMethod;
    ComPtr<ID3DBlob> ComputeShaderMethod;
    ComPtr<ID3D12RootSignature> RootSignature;
    ComPtr<ID3D12CommandSignature> CommandSignature;
};

struct Direct3D12PipelineState
{
    ComPtr<ID3D12PipelineState> PipelineStateObject;
};

struct Direct3D12SwapChain
{
    ComPtr<IDXGISwapChain3> SwapChainObject;
    Direct3D12CommandQueue* CommandQueue;
    void* WaitHandle;
    Direct3D12Texture* BackBufferTextures[RenderBuffersCount];
};

class Direct3D12GraphicsService
{
    public:
        Direct3D12GraphicsService();
        ~Direct3D12GraphicsService();

        void GetGraphicsAdapterName(char* output);
        GraphicsAllocationInfos GetBufferAllocationInfos(int sizeInBytes);
        GraphicsAllocationInfos GetTextureAllocationInfos(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);

        void* CreateCommandQueue(enum GraphicsServiceCommandType commandQueueType);
        void SetCommandQueueLabel(void* commandQueuePointer, char* label);
        void DeleteCommandQueue(void* commandQueuePointer);
        void ResetCommandQueue(void* commandQueuePointer);
        unsigned long GetCommandQueueTimestampFrequency(void* commandQueuePointer);
        unsigned long ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength);
        void WaitForCommandQueueOnCpu(struct GraphicsFence fenceToWait);

        void* CreateCommandList(void* commandQueuePointer);
        void SetCommandListLabel(void* commandListPointer, char* label);
        void DeleteCommandList(void* commandListPointer);
        void ResetCommandList(void* commandListPointer);
        void CommitCommandList(void* commandListPointer);

        void* CreateGraphicsHeap(enum GraphicsServiceHeapType type, unsigned long sizeInBytes);
        void SetGraphicsHeapLabel(void* graphicsHeapPointer, char* label);
        void DeleteGraphicsHeap(void* graphicsHeapPointer);

        void* CreateShaderResourceHeap(unsigned long length);
        void SetShaderResourceHeapLabel(void* shaderResourceHeapPointer, char* label);
        void DeleteShaderResourceHeap(void* shaderResourceHeapPointer);
        void CreateShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index, void* texturePointer, int isWriteable, unsigned int mipLevel);
        void DeleteShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index);
        void CreateShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer, int isWriteable);
        void DeleteShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index);

        void* CreateGraphicsBuffer(void* graphicsHeapPointer, unsigned long heapOffset, GraphicsBufferUsage graphicsBufferUsage, int sizeInBytes);
        void SetGraphicsBufferLabel(void* graphicsBufferPointer, char* label);
        void DeleteGraphicsBuffer(void* graphicsBufferPointer);
        void* GetGraphicsBufferCpuPointer(void* graphicsBufferPointer);
        void ReleaseGraphicsBufferCpuPointer(void* graphicsBufferPointer);

        void* CreateTexture(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
        void SetTextureLabel(void* texturePointer, char* label);
        void DeleteTexture(void* texturePointer);

        void* CreateSwapChain(void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat);
        void DeleteSwapChain(void* swapChainPointer);
        void ResizeSwapChain(void* swapChainPointer, int width, int height);
        void* GetSwapChainBackBufferTexture(void* swapChainPointer);
        unsigned long PresentSwapChain(void* swapChainPointer);
        void WaitForSwapChainOnCpu(void* swapChainPointer);

        void* CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length);
        void ResetQueryBuffer(void* queryBufferPointer);
        void SetQueryBufferLabel(void* queryBufferPointer, char* label);
        void DeleteQueryBuffer(void* queryBufferPointer);

        void* CreateShader(char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength);
        void SetShaderLabel(void* shaderPointer, char* label);
        void DeleteShader(void* shaderPointer);

        void* CreateComputePipelineState(void* shaderPointer);
        void* CreatePipelineState(void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
        void SetPipelineStateLabel(void* pipelineStatePointer, char* label);
        void DeletePipelineState(void* pipelineStatePointer);

        void CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, unsigned int sizeInBytes, unsigned int destinationOffsetInBytes, unsigned int sourceOffsetInBytes);
        void CopyDataToTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
        void CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer);

        void TransitionGraphicsBufferToState(void* commandListPointer, void* graphicsBufferPointer, enum GraphicsResourceState resourceState);

        void DispatchThreads(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);

        void BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
        void EndRenderPass(void* commandListPointer);

        void SetPipelineState(void* commandListPointer, void* pipelineStatePointer);
        void SetShaderResourceHeap(void* commandListPointer, void* shaderResourceHeapPointer);
        void SetShader(void* commandListPointer, void* shaderPointer);
        void SetShaderParameterValues(void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength);

        void SetTextureBarrier(void* commandListPointer, void* texturePointer);
        void SetGraphicsBufferBarrier(void* commandListPointer, void* graphicsBufferPointer);

        void DispatchMesh(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);
        void ExecuteIndirect(void* commandListPointer, unsigned int maxCommandCount, void* commandGraphicsBufferPointer, unsigned int commandBufferOffset);

        void BeginQuery(void* commandListPointer, void* queryBufferPointer, int index);
        void EndQuery(void* commandListPointer, void* queryBufferPointer, int index);
        void ResolveQueryData(void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex);

    private:
        // Device objects
        wstring adapterName;
        ComPtr<IDXGIFactory4> dxgiFactory; 
        ComPtr<ID3D12Device9> graphicsDevice;
        ComPtr<ID3D12Debug5> debugController;
        ComPtr<ID3D12InfoQueue1> debugInfoQueue;
        ComPtr<IDXGIDebug> dxgiDebug;
        
        // Command Objects
        int32_t currentAllocatorIndex = 0;

        // Synchronization objects
        HANDLE globalFenceEvent;
        bool isWaitingForGlobalFence;

        // Heap objects
        ComPtr<ID3D12DescriptorHeap> globalRtvDescriptorHeap;
        uint32_t globalRtvDescriptorHandleSize;
        uint32_t currentGlobalRtvDescriptorOffset;

        ComPtr<ID3D12DescriptorHeap> globalDsvDescriptorHeap;
        uint32_t globalDsvDescriptorHandleSize;
        uint32_t currentGlobalDsvDescriptorOffset;

        // Shaders
        Direct3D12Shader* shaderBound;

        void EnableDebugLayer();
        ComPtr<IDXGIAdapter4> FindGraphicsAdapter(const ComPtr<IDXGIFactory4> dxgiFactory);
        bool CreateDevice(const ComPtr<IDXGIFactory4> dxgiFactory, const ComPtr<IDXGIAdapter4> graphicsAdapter);
        bool CreateHeaps();

        void TransitionTextureToState(Direct3D12CommandList* commandList, Direct3D12Texture* texture, D3D12_RESOURCE_STATES destinationState);
        void TransitionBufferToState(Direct3D12CommandList* commandList, Direct3D12GraphicsBuffer* graphicsBuffer, D3D12_RESOURCE_STATES destinationState);
};
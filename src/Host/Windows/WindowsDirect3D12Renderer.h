#pragma once

#include <winrt/base.h>
#include <winrt/Windows.UI.Core.h>
#include <d3d12.h>

#if defined(NTDDI_WIN10_RS2)
#include <dxgi1_6.h>
#else
#include <dxgi1_5.h>
#endif

#include "../Common/CoreEngine.h"

#define ArrayCount(value) ((sizeof(value) / sizeof(value[0])))

static const int RenderBuffersCountConst = 2;

namespace impl
{
    using namespace winrt;
    using namespace Windows::Foundation::Collections;
    using namespace Windows::UI::Core;

    struct WindowsDirect3D12Buffer
    {
        com_ptr<ID3D12Resource>* CpuGraphicsBuffers;
        com_ptr<ID3D12Resource>* GpuGraphicsBuffers;
        uint32_t BuffersCount;
        bool IsInCopyState;
    };

    class Direct3D12Texture
    {
    public:
        int Width;
        int Height;
        int Pitch;
        DXGI_FORMAT Format;
        com_ptr<ID3D12Resource> Resource;
        com_ptr<ID3D12Resource> UploadHeap;
        void* UploadHeapData;
        D3D12_RESOURCE_BARRIER PixelShaderToCopyDestBarrier;
        D3D12_RESOURCE_BARRIER CopyDestToPixelShaderBarrier;
        D3D12_PLACED_SUBRESOURCE_FOOTPRINT SubResourceFootPrint;
    };

    class WindowsDirect3D12Renderer
    {
    public:
        WindowsDirect3D12Renderer(const CoreWindow& window, int width, int height, int refreshRate);
        ~WindowsDirect3D12Renderer();

        Vector2 GetRenderSize();
        unsigned int CreateShader(HostMemoryBuffer shaderByteCode);
        unsigned int CreateShaderParameters(unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3);
        unsigned int CreateStaticGraphicsBuffer(HostMemoryBuffer data);
        HostMemoryBuffer CreateDynamicGraphicsBuffer(unsigned int length);
        void UploadDataToGraphicsBuffer(unsigned int graphicsBufferId, HostMemoryBuffer data);
        void BeginCopyGpuData();
        void EndCopyGpuData();
        void BeginRender();
        void EndRender();
        void DrawPrimitives(unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId);

        void PresentScreenBuffer();
        bool SwitchScreenMode();

    private:
        bool IsInitialized;
        bool IsFullscreen;
        int Width;
        int Height;
        int BytesPerPixel;
        int Pitch;
        int RefreshRate;
        bool VSync;
        int RenderBuffersCount;

        com_ptr<ID3D12Device3> Device;
        com_ptr<IDXGISwapChain3> SwapChain;

        com_ptr<ID3D12CommandQueue> CommandQueue;
        com_ptr<ID3D12CommandAllocator> CommandAllocator[RenderBuffersCountConst] = {};
        com_ptr<ID3D12GraphicsCommandList> CommandList;

        com_ptr<ID3D12CommandQueue> copyCommandQueue;
        com_ptr<ID3D12CommandAllocator> copyCommandAllocator[RenderBuffersCountConst] = {};
        com_ptr<ID3D12GraphicsCommandList> copyCommandList;

        com_ptr<ID3D12DescriptorHeap> RtvDescriptorHeap;
        com_ptr<ID3D12DescriptorHeap> SrvDescriptorHeap;
        int RtvDescriptorHandleSize;
        com_ptr<ID3D12Resource> RenderTargets[RenderBuffersCountConst];
        D3D12_RESOURCE_BARRIER PresentToRenderTargetBarriers[RenderBuffersCountConst];
        D3D12_RESOURCE_BARRIER RenderTargetToPresentBarriers[RenderBuffersCountConst];

        com_ptr<ID3D12Heap> uploadHeap;
        uint64_t currentUploadHeapOffset;
        com_ptr<ID3D12Heap> globalHeap;
        uint64_t currentGlobalHeapOffset;

        std::map<uint32_t, WindowsDirect3D12Buffer> graphicsBuffers;
        // std::map<uint32_t, com_ptr<ID3D12Resource>> cpuGraphicsBuffers;
        // std::map<uint32_t, com_ptr<ID3D12Resource>> graphicsBuffers;
        IVector<uint32_t> graphicsBuffersToCopy;
        uint32_t currentGraphicsBufferId;

        com_ptr<ID3D12PipelineState> pipelineState;
        com_ptr<ID3D12RootSignature> rootSignature;

        com_ptr<ID3D12PipelineState> SpritePSO;
        com_ptr<ID3D12RootSignature> SpriteRootSignature;

        Direct3D12Texture Texture;

        D3D12_VIEWPORT Viewport;
        D3D12_RECT ScissorRect;

        int CurrentBackBufferIndex;

        // Synchronization objects
        com_ptr<ID3D12Fence1> Fence;
        uint64_t FrameFenceValues[RenderBuffersCountConst] = {};
        uint64_t FenceValue;
        HANDLE FenceEvent;

        com_ptr<IDXGIAdapter4> FindGraphicsAdapter(const com_ptr<IDXGIFactory4> dxgiFactory);
        Direct3D12Texture Direct3D12CreateTexture(ID3D12Device* device, int width, int height, DXGI_FORMAT format);
        void UploadTextureData(ID3D12GraphicsCommandList* commandList, const Direct3D12Texture& texture);
        D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetViewHandle();

        void Direct32D2EnableDebugLayer();
        void Direct32D2WaitForPreviousFrame();
        bool Direct3D12CreateDevice(const com_ptr<IDXGIFactory4> dxgiFactory, const com_ptr<IDXGIAdapter4> graphicsAdapter, const CoreWindow& window, int width, int height);
        bool Direct3D12InitSizeDependentResources();

        bool Direct3D12CreateSpriteRootSignature();
        com_ptr<ID3D12PipelineState> Direct3D12CreatePipelineState(ID3D12RootSignature* rootSignature, char* shaderCode);
        bool Direct3D12CreateSpritePSO();
        bool Direct3D12CreateResources();
    };
};

using ::impl::WindowsDirect3D12Buffer;
using ::impl::Direct3D12Texture;
using ::impl::WindowsDirect3D12Renderer;
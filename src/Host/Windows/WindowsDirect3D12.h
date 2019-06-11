#pragma once

#include <winrt/base.h>
#include <winrt/Windows.UI.Core.h>
#include <d3d12.h>

#if defined(NTDDI_WIN10_RS2)
#include <dxgi1_6.h>
#else
#include <dxgi1_5.h>
#endif

#define ReturnIfFailed(expression) if (FAILED(expression)) { OutputDebugStringA("ERROR: DirectX12 Init error!\n"); return false; };
#define ArrayCount(value) ((sizeof(value) / sizeof(value[0])))

static const int RenderBuffersCountConst = 2;

namespace impl
{
    using namespace winrt;
    using namespace Windows::UI::Core;

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

    class Direct3D12
    {
    public:
        Direct3D12() {}
        D3D12_RESOURCE_BARRIER CreateTransitionResourceBarrier(ID3D12Resource* resource, D3D12_RESOURCE_STATES stateBefore, D3D12_RESOURCE_STATES stateAfter);
        Direct3D12Texture Direct3D12CreateTexture(ID3D12Device* device, int width, int height, DXGI_FORMAT format);
        void UploadTextureData(ID3D12GraphicsCommandList* commandList, const Direct3D12Texture& texture);
        D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetViewHandle();

        void Direct32D2EnableDebugLayer();
        void Direct32D2WaitForPreviousFrame();
        bool Direct3D12CreateDevice(const CoreWindow& window, int width, int height);
        bool Direct3D12InitSizeDependentResources();

        bool Direct3D12CreateSpriteRootSignature();
        bool Direct3D12CreateCheckBoardRootSignature();
        com_ptr<ID3D12PipelineState> Direct3D12CreatePipelineState(ID3D12RootSignature* rootSignature, char* shaderCode);
        bool Direct3D12CreateSpritePSO();
        bool Direct3D12CreateCheckBoardPSO();
        bool Direct3D12CreateResources();

        void Direct3D12Init(const CoreWindow& window, int width, int height, int refreshRate);
        void Direct3D12Destroy();

        void Direct3D12BeginFrame();
        void Direct3D12EndFrame();
        void Direct3D12PresentScreenBuffer();
        bool Direct3D12SwitchScreenMode();

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

        com_ptr<ID3D12Device> Device;
        com_ptr<ID3D12CommandQueue> CommandQueue;
        com_ptr<ID3D12CommandAllocator> CommandAllocator;
        com_ptr<ID3D12GraphicsCommandList> CommandList;
        com_ptr<IDXGISwapChain3> SwapChain;

        com_ptr<ID3D12DescriptorHeap> RtvDescriptorHeap;
        com_ptr<ID3D12DescriptorHeap> SrvDescriptorHeap;
        int RtvDescriptorHandleSize;
        com_ptr<ID3D12Resource> RenderTargets[RenderBuffersCountConst];
        D3D12_RESOURCE_BARRIER PresentToRenderTargetBarriers[RenderBuffersCountConst];
        D3D12_RESOURCE_BARRIER RenderTargetToPresentBarriers[RenderBuffersCountConst];

        com_ptr<ID3D12PipelineState> SpritePSO;
        com_ptr<ID3D12RootSignature> SpriteRootSignature;
        com_ptr<ID3D12PipelineState> CheckBoardPSO;
        com_ptr<ID3D12RootSignature> CheckBoardRootSignature;

        Direct3D12Texture Texture;

        D3D12_VIEWPORT Viewport;
        D3D12_RECT ScissorRect;

        // Synchronization objects
        int CurrentBackBufferIndex;
        HANDLE FenceEvent;
        com_ptr<ID3D12Fence> Fence;
        UINT64 FenceValue;
    };
};

using ::impl::Direct3D12Texture;
using ::impl::Direct3D12;
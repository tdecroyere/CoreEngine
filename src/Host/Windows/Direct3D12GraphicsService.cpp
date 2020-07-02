#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"
#include "Direct3D12GraphicsServiceUtils.h"

using namespace std;
using namespace Microsoft::WRL;

#define AssertIfFailed(result) assert(!FAILED(result))
#define GetAlignedValue(value, alignement) (value + (alignement - (value % alignement)) % alignement)

Direct3D12GraphicsService::Direct3D12GraphicsService(HWND window, int width, int height)
{
    UINT createFactoryFlags = 0;

#ifdef DEBUG
	EnableDebugLayer();
    createFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
#endif

    ComPtr<IDXGIFactory4> dxgiFactory;
	AssertIfFailed(CreateDXGIFactory2(createFactoryFlags, IID_PPV_ARGS(dxgiFactory.ReleaseAndGetAddressOf())));

	auto graphicsAdapter = FindGraphicsAdapter(dxgiFactory);
	AssertIfFailed(CreateDevice(dxgiFactory, graphicsAdapter));
	AssertIfFailed(CreateOrResizeSwapChain(dxgiFactory, window, width, height));
	AssertIfFailed(CreateHeaps());
}

Direct3D12GraphicsService::~Direct3D12GraphicsService()
{
	// Ensure that the GPU is no longer referencing resources that are about to be
	// cleaned up by the destructor.
	this->WaitForAvailableScreenBuffer();

	// Fullscreen state should always be false before exiting the app.
	this->swapChain->SetFullscreenState(false, nullptr);

	CloseHandle(this->globalFenceEvent);
}

struct Vector2 Direct3D12GraphicsService::GetRenderSize()
{
    return Vector2 { 1280, 720 };
}

void Direct3D12GraphicsService::GetGraphicsAdapterName(char* output)
{ 
    // strcpyW(output, "Test");
    this->adapterName.copy((wchar_t*)output, this->adapterName.length());
}

int Direct3D12GraphicsService::CreateGraphicsBuffer(unsigned int graphicsBufferId, int length, int isWriteOnly, char* label)
{ 
	// For the moment, all data is aligned in a 64 KB alignement
	uint64_t alignement = 64 * 1024;

	D3D12_RESOURCE_DESC resourceDesc = {};
	resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
	resourceDesc.Alignment = 0;
	resourceDesc.Width = GetAlignedValue(length, alignement);
	resourceDesc.Height = 1;
	resourceDesc.DepthOrArraySize = 1;
	resourceDesc.MipLevels = 1;
	resourceDesc.Format = DXGI_FORMAT_UNKNOWN;
	resourceDesc.SampleDesc.Count = 1;
	resourceDesc.SampleDesc.Quality = 0;
	resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
	resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

	// Create a Direct3D12 buffer on the CPU
	ComPtr<ID3D12Resource> cpuBuffer;
	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(this->uploadHeap.Get(), this->currentUploadHeapOffset, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, nullptr, IID_PPV_ARGS(cpuBuffer.ReleaseAndGetAddressOf())));
	cpuBuffer->SetName((wstring(L"CpuBuffer_") + wstring(label, label + strlen(label))).c_str());
	this->currentUploadHeapOffset += GetAlignedValue(length, alignement);
	this->cpuBuffers[graphicsBufferId] = cpuBuffer;

	// Create a Direct3D12 buffer on the GPU
	ComPtr<ID3D12Resource> gpuBuffer;
	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(this->globalHeap.Get(), this->currentGlobalHeapOffset, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(gpuBuffer.ReleaseAndGetAddressOf())));
	cpuBuffer->SetName((wstring(L"GpuBuffer_") + wstring(label, label + strlen(label))).c_str());
	this->currentGlobalHeapOffset += GetAlignedValue(length, alignement);
	this->gpuBuffers[graphicsBufferId] = gpuBuffer;

	void* pointer = nullptr;
	D3D12_RANGE range = { 0, 0 };
	cpuBuffer->Map(0, &range, &pointer);

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

int Direct3D12GraphicsService::CreateCommandBuffer(unsigned int commandBufferId, enum GraphicsCommandBufferType commandBufferType, char* label)
{ 
	D3D12_COMMAND_LIST_TYPE listType = D3D12_COMMAND_LIST_TYPE_DIRECT;

	if (commandBufferType == GraphicsCommandBufferType::Copy)
	{
		listType = D3D12_COMMAND_LIST_TYPE_COPY;
	}

	else if (commandBufferType == GraphicsCommandBufferType::Compute)
	{
		listType = D3D12_COMMAND_LIST_TYPE_COMPUTE;
	}

	ComPtr<ID3D12CommandAllocator> commandBuffer;
	AssertIfFailed(this->graphicsDevice->CreateCommandAllocator(listType, IID_PPV_ARGS(commandBuffer.ReleaseAndGetAddressOf())));

	commandBuffer->SetName(wstring(label, label + strlen(label)).c_str());
	this->commandBuffers[commandBufferId] = commandBuffer;
    return 1;
}

void Direct3D12GraphicsService::DeleteCommandBuffer(unsigned int commandBufferId)
{ 
	this->commandBuffers.erase(commandBufferId);
}

void Direct3D12GraphicsService::ResetCommandBuffer(unsigned int commandBufferId)
{ 
	if (this->commandBuffers.count(commandBufferId))
	{
		auto commandBuffer = this->commandBuffers[commandBufferId];
		commandBuffer->Reset();
	}
}

void Direct3D12GraphicsService::ExecuteCommandBuffer(unsigned int commandBufferId)
{ 
	// TODO: Update Status
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
	if (!this->commandBuffers.count(commandBufferId))
	{
		return 0;
	}
	
	auto commandList = CreateOrResetCommandList(commandBufferId, label, D3D12_COMMAND_LIST_TYPE_COPY);
	this->commandLists[commandListId] = commandList;

    return 1;
}

void Direct3D12GraphicsService::CommitCopyCommandList(unsigned int commandListId)
{ 
	if (this->commandLists.count(commandListId))
	{
		auto commandList = this->commandLists[commandListId];
		commandList->Close();

		ID3D12CommandList* commandLists[] = { commandList.Get() };
		this->copyCommandQueue->ExecuteCommandLists(1, commandLists);

		this->commandLists.erase(commandListId);
		ReleaseCommandList(commandList, D3D12_COMMAND_LIST_TYPE_COPY);
	}
}

void Direct3D12GraphicsService::UploadDataToGraphicsBuffer(unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength)
{ 
	if (!this->commandLists.count(commandListId) && !this->cpuBuffers.count(graphicsBufferId) && !this->gpuBuffers.count(graphicsBufferId))
	{
		return;
	}

	auto commandList = this->commandLists[commandListId];
	auto gpuBuffer = this->gpuBuffers[graphicsBufferId];
	auto cpuBuffer = this->cpuBuffers[graphicsBufferId];

	commandList->CopyResource(gpuBuffer.Get(), cpuBuffer.Get());
}

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
	if (!this->commandBuffers.count(commandBufferId))
	{
		return 0;
	}
	
	auto commandList = CreateOrResetCommandList(commandBufferId, label, D3D12_COMMAND_LIST_TYPE_DIRECT);
	this->commandLists[commandListId] = commandList;

	// D3D12_VIEWPORT viewport = {};
	// viewport.Width = (float)width;
	// viewport.Height = (float)height;
	// viewport.MaxDepth = 1.0f;
	// commandList->RSSetViewports(1, &viewport);

	// D3D12_RECT scissorRect = {};
	// scissorRect.right = (long)width;
	// scissorRect.bottom = (long)height;
	// commandList->RSSetScissorRects(1, &scissorRect);

	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = GetCurrentRenderTargetViewHandle();

	if (!renderDescriptor.RenderTarget1TextureId.HasValue && !renderDescriptor.DepthTextureId.HasValue)
	{
		commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(this->backBufferRenderTargets[this->currentBackBufferIndex].Get(), D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET));
		commandList->OMSetRenderTargets(1, &renderTargetViewHandle, false, nullptr);

		float clearColor[4] = { 0.0f, 0.215f, 1.0f, 0.0f };
		commandList->ClearRenderTargetView(renderTargetViewHandle, clearColor, 0, nullptr);
	}
	
    return 1;
}

void Direct3D12GraphicsService::CommitRenderCommandList(unsigned int commandListId)
{ 
	if (this->commandLists.count(commandListId))
	{
		auto commandList = this->commandLists[commandListId];
		commandList->Close();

		ID3D12CommandList* commandLists[] = { commandList.Get() };
		this->directCommandQueue->ExecuteCommandLists(1, commandLists);

		this->commandLists.erase(commandListId);
		ReleaseCommandList(commandList, D3D12_COMMAND_LIST_TYPE_DIRECT);
	}
}

void Direct3D12GraphicsService::SetPipelineState(unsigned int commandListId, unsigned int pipelineStateId){ }
void Direct3D12GraphicsService::SetShader(unsigned int commandListId, unsigned int shaderId){ }
void Direct3D12GraphicsService::ExecuteIndirectCommandBuffer(unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount){ }
void Direct3D12GraphicsService::SetIndexBuffer(unsigned int commandListId, unsigned int graphicsBufferId){ }
void Direct3D12GraphicsService::DrawIndexedPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId){ }

void Direct3D12GraphicsService::DrawPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
{ 
	if (this->commandLists.count(commandListId))
	{
		auto commandList = this->commandLists[commandListId];

		if (primitiveType == GraphicsPrimitiveType::TriangleStrip)
		{
			commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
		}

		else if (primitiveType == GraphicsPrimitiveType::Line)
		{
			commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_LINELIST);
		}

		else
		{
			commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
		}

		commandList->DrawInstanced(vertexCount, 1, startVertex, 0);
	}
}

void Direct3D12GraphicsService::WaitForCommandList(unsigned int commandListId, unsigned int commandListToWaitId){ }

void Direct3D12GraphicsService::PresentScreenBuffer(unsigned int commandBufferId)
{ 
	if (!this->commandBuffers.count(commandBufferId))
	{
		return;
	}
	
	auto commandList = CreateOrResetCommandList(commandBufferId, "PresentCommandList", D3D12_COMMAND_LIST_TYPE_DIRECT);
	commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(this->backBufferRenderTargets[this->currentBackBufferIndex].Get(), D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT));

	commandList->Close();

	ID3D12CommandList* commandLists[] = { commandList.Get() };
	this->directCommandQueue->ExecuteCommandLists(1, commandLists);

	ReleaseCommandList(commandList, D3D12_COMMAND_LIST_TYPE_DIRECT);

	AssertIfFailed(this->swapChain->Present(1, 0));
}

void Direct3D12GraphicsService::WaitForAvailableScreenBuffer()
{ 
	// TODO:
	// WAITING FOR THE FRAME TO COMPLETE BEFORE CONTINUING IS NOT BEST PRACTICE.
	// This is code implemented as such for simplicity. More advanced samples 
	// illustrate how to use fences for efficient resource usage.

	if (this->globalFence)
	{
		// Signal and increment the fence value
		const UINT64 fence = this->globalFenceValue;
		this->directCommandQueue->Signal(this->globalFence.Get(), fence);
		this->globalFenceValue++;

		// Wait until the previous frame is finished
		if (this->globalFence->GetCompletedValue() < fence)
		{
			this->globalFence->SetEventOnCompletion(fence, this->globalFenceEvent);
			WaitForSingleObject(this->globalFenceEvent, INFINITE);
		}

		this->currentBackBufferIndex = this->swapChain->GetCurrentBackBufferIndex();
	}
}

void Direct3D12GraphicsService::EnableDebugLayer()
{
	// If the project is in a debug build, enable debugging via SDK Layers.
	ComPtr<ID3D12Debug> debugController;
	
	D3D12GetDebugInterface(IID_PPV_ARGS(debugController.GetAddressOf()));

	if (debugController)
	{
		debugController->EnableDebugLayer();
	}
}

ComPtr<IDXGIAdapter4> Direct3D12GraphicsService::FindGraphicsAdapter(const ComPtr<IDXGIFactory4> dxgiFactory)
{	
    ComPtr<IDXGIAdapter1> dxgiAdapter1;
	ComPtr<IDXGIAdapter4> dxgiAdapter4;

	SIZE_T maxDedicatedVideoMemory = 0;

	for (int i = 0; dxgiFactory->EnumAdapters1(i, dxgiAdapter1.ReleaseAndGetAddressOf()) != DXGI_ERROR_NOT_FOUND; i++)
	{
		DXGI_ADAPTER_DESC1 dxgiAdapterDesc1;
		dxgiAdapter1->GetDesc1(&dxgiAdapterDesc1);

		if ((dxgiAdapterDesc1.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) == 0)
		{
			ComPtr<ID3D12Device> tempDevice;
			D3D12CreateDevice(dxgiAdapter1.Get(), D3D_FEATURE_LEVEL_12_1, IID_PPV_ARGS(tempDevice.ReleaseAndGetAddressOf()));

			// D3D12_FEATURE_DATA_D3D12_OPTIONS deviceOptions = {};
			// AssertIfFailed(tempDevice->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &deviceOptions, sizeof(deviceOptions)));

			if (/*deviceOptions.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER_2 && */dxgiAdapterDesc1.DedicatedVideoMemory > maxDedicatedVideoMemory)
			{
				this->adapterName = wstring(dxgiAdapterDesc1.Description);
				maxDedicatedVideoMemory = dxgiAdapterDesc1.DedicatedVideoMemory;
				dxgiAdapter1.As(&dxgiAdapter4);
			}
		}
	}

	return dxgiAdapter4;
}

bool Direct3D12GraphicsService::CreateDevice(const ComPtr<IDXGIFactory4> dxgiFactory, const ComPtr<IDXGIAdapter4> graphicsAdapter)
{
	// Created Direct3D Device
	HRESULT result = D3D12CreateDevice(graphicsAdapter.Get(), D3D_FEATURE_LEVEL_12_0, IID_PPV_ARGS(this->graphicsDevice.ReleaseAndGetAddressOf()));

	if (FAILED(result))
	{
		// If hardware initialization fail, fall back to the WARP driver
		OutputDebugStringA("Direct3D hardware device initialization failed. Falling back to WARP driver.\n");

		ComPtr<IDXGIAdapter> warpAdapter;
		dxgiFactory->EnumWarpAdapter(IID_PPV_ARGS(warpAdapter.ReleaseAndGetAddressOf()));

		AssertIfFailed(D3D12CreateDevice(warpAdapter.Get(), D3D_FEATURE_LEVEL_12_0, IID_PPV_ARGS(this->graphicsDevice.ReleaseAndGetAddressOf())));
	}

	// Create the direct command queue
	D3D12_COMMAND_QUEUE_DESC directCommandQueueDesc = {};
	directCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	directCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&directCommandQueueDesc, IID_PPV_ARGS(this->directCommandQueue.ReleaseAndGetAddressOf())));

	// Create the copy command queue
	D3D12_COMMAND_QUEUE_DESC copyCommandQueueDesc = {};
	copyCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	copyCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COPY;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&copyCommandQueueDesc, IID_PPV_ARGS(this->copyCommandQueue.ReleaseAndGetAddressOf())));

	// Create the compute command queue
	D3D12_COMMAND_QUEUE_DESC computeCommandQueueDesc = {};
	computeCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	computeCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COMPUTE;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&computeCommandQueueDesc, IID_PPV_ARGS(this->computeCommandQueue.ReleaseAndGetAddressOf())));

	// Describe and create a render target view (RTV) descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
	rtvHeapDesc.NumDescriptors = RenderBuffersCount;
	rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	
	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS(this->rtvDescriptorHeap.ReleaseAndGetAddressOf())));
	this->rtvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

	return true;
}

bool Direct3D12GraphicsService::CreateOrResizeSwapChain(const ComPtr<IDXGIFactory4> dxgiFactory, HWND window, int width, int height)
{
	// Create the swap chain
	// TODO: Check for supported formats
	// TODO: Add support for HDR displays
	// TODO: Add support for resizing

	if (this->swapChain)
	{
		// Wait until all previous GPU work is complete.
		WaitForAvailableScreenBuffer();

		// Release resources that are tied to the swap chain and update fence values.
		for (int i = 0; i < RenderBuffersCount; i++)
		{
			this->backBufferRenderTargets[i].Reset();
			this->globalFrameFenceValues[i] = this->globalFrameFenceValues[this->currentBackBufferIndex];
		}
	}

	// TODO: Select the right format for HDR or SDR
	auto format = DXGI_FORMAT_B8G8R8A8_UNORM;
	auto formatSrgb = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;

	if (!this->swapChain)
	{
		DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
		swapChainDesc.BufferCount = RenderBuffersCount;
		swapChainDesc.Width = width;
		swapChainDesc.Height = height;
		swapChainDesc.Format = format;
		swapChainDesc.Scaling = DXGI_SCALING_STRETCH;
		swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
		swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_IGNORE;
		swapChainDesc.SampleDesc = { 1, 0 };

		DXGI_SWAP_CHAIN_FULLSCREEN_DESC swapChainFullScreenDesc = {};
		swapChainFullScreenDesc.Windowed = true;
		
		AssertIfFailed(dxgiFactory->CreateSwapChainForHwnd(this->directCommandQueue.Get(), window, &swapChainDesc, &swapChainFullScreenDesc, nullptr, (IDXGISwapChain1**)this->swapChain.ReleaseAndGetAddressOf()));

		// Create a fence object used to synchronize the CPU with the GPU
		AssertIfFailed(this->graphicsDevice->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(this->globalFence.ReleaseAndGetAddressOf())));
		this->globalFenceValue = 1;

		// Create an event handle to use for frame synchronization
		this->globalFenceEvent = CreateEventA(nullptr, false, false, nullptr);
	}

	else
	{
		// If the swap chain already exists, resize it.
        auto result = this->swapChain->ResizeBuffers(RenderBuffersCount, width, height, format, 0);

        if (result == DXGI_ERROR_DEVICE_REMOVED || result == DXGI_ERROR_DEVICE_RESET)
        {
            char buff[64] = {};
            sprintf_s(buff, "Device Lost on ResizeBuffers: Reason code 0x%08X\n", (result == DXGI_ERROR_DEVICE_REMOVED) ? this->graphicsDevice->GetDeviceRemovedReason() : result);
            OutputDebugStringA(buff);

            // If the device was removed for any reason, a new device and swap chain will need to be created.
            //HandleDeviceLost();

            // Everything is set up now. Do not continue execution of this method. HandleDeviceLost will reenter this method
            // and correctly set up the new device.
            return false;
        }

        else
        {
            AssertIfFailed(result);
        }
	}

	// Create a RTV for each back buffers
	D3D12_CPU_DESCRIPTOR_HANDLE rtvDecriptorHandle = this->rtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();

	D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
	rtvDesc.Format = formatSrgb;
	rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		AssertIfFailed(this->swapChain->GetBuffer(i, IID_PPV_ARGS(this->backBufferRenderTargets[i].ReleaseAndGetAddressOf())));

		wchar_t buff[64] = {};
  		swprintf(buff, L"BackBufferRenderTarget%d", i);
		this->backBufferRenderTargets[i]->SetName(buff);

		this->graphicsDevice->CreateRenderTargetView(this->backBufferRenderTargets[i].Get(), &rtvDesc, rtvDecriptorHandle);
		rtvDecriptorHandle.ptr += this->rtvDescriptorHandleSize;
	}

    // Reset the index to the current back buffer
    this->currentBackBufferIndex = this->swapChain->GetCurrentBackBufferIndex();

	return true;
}

bool Direct3D12GraphicsService::CreateHeaps()
{
	// Create cpu heap
	D3D12_HEAP_DESC heapDescriptor = {};
	heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_UPLOAD;
	heapDescriptor.Properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
	heapDescriptor.SizeInBytes = 1024 * 1024 * 100; // Allocate 100 MB for now
	heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

	AssertIfFailed(this->graphicsDevice->CreateHeap(&heapDescriptor, IID_PPV_ARGS(this->uploadHeap.ReleaseAndGetAddressOf())));

	// Create global GPU heap
	heapDescriptor = {};
	heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_DEFAULT;
	heapDescriptor.SizeInBytes = 1024 * 1024 * 1024; // Allocate 1GB for now
	heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES;

	AssertIfFailed(this->graphicsDevice->CreateHeap(&heapDescriptor, IID_PPV_ARGS(this->globalHeap.ReleaseAndGetAddressOf())));

	this->currentUploadHeapOffset = 0;
	this->currentGlobalHeapOffset = 0;

	return true;
}

D3D12_CPU_DESCRIPTOR_HANDLE Direct3D12GraphicsService::GetCurrentRenderTargetViewHandle()
{
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = {};
	renderTargetViewHandle.ptr = this->rtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + this->currentBackBufferIndex * this->rtvDescriptorHandleSize;

	return renderTargetViewHandle;
}

ComPtr<ID3D12GraphicsCommandList> Direct3D12GraphicsService::CreateOrResetCommandList(unsigned int commandBufferId, char* label, D3D12_COMMAND_LIST_TYPE listType)
{
	if (!this->commandBuffers.count(commandBufferId))
	{
		return nullptr;
	}
	
	auto commandBuffer = this->commandBuffers[commandBufferId];

	auto commandListCache = this->renderCommandListCache;

	if (listType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandListCache = this->copyCommandListCache;
	}

	else if (listType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandListCache = this->computeCommandListCache;
	}

	if (commandListCache.empty())
	{
		ComPtr<ID3D12GraphicsCommandList> commandList;
		AssertIfFailed(this->graphicsDevice->CreateCommandList(0, listType, commandBuffer.Get(), nullptr, IID_PPV_ARGS(commandList.ReleaseAndGetAddressOf())));
		commandList->SetName(wstring(label, label + strlen(label)).c_str());

		return commandList;
	}

	else
	{
		auto commandList = commandListCache.top();
		commandListCache.pop();
		
		commandList->Reset(commandBuffer.Get(), nullptr);
		commandList->SetName(wstring(label, label + strlen(label)).c_str());

		return commandList;
	}
}

void Direct3D12GraphicsService::ReleaseCommandList(ComPtr<ID3D12GraphicsCommandList> commandList, D3D12_COMMAND_LIST_TYPE listType)
{
	auto commandListCache = this->renderCommandListCache;

	if (listType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandListCache = this->copyCommandListCache;
	}

	else if (listType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandListCache = this->computeCommandListCache;
	}

	commandListCache.push(commandList);
}
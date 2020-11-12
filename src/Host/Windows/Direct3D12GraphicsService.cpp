#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"
#include "Direct3D12GraphicsServiceUtils.h"

using namespace std;
using namespace Microsoft::WRL;

#define GetAlignedValue(value, alignement) (value + (alignement - (value % alignement)) % alignement)

Direct3D12GraphicsService::Direct3D12GraphicsService()
{
	this->isWaitingForGlobalFence = false;
    UINT createFactoryFlags = 0;

#ifdef DEBUG
	EnableDebugLayer();
    createFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
#endif

	// this->window = window;
	AssertIfFailed(CreateDXGIFactory2(createFactoryFlags, IID_PPV_ARGS(this->dxgiFactory.ReleaseAndGetAddressOf())));

	auto graphicsAdapter = FindGraphicsAdapter(dxgiFactory);
	AssertIfFailed(CreateDevice(dxgiFactory, graphicsAdapter));
	//AssertIfFailed(CreateOrResizeSwapChain(width, height));
	AssertIfFailed(CreateHeaps());
}

Direct3D12GraphicsService::~Direct3D12GraphicsService()
{
	// Ensure that the GPU is no longer referencing resources that are about to be
	// cleaned up by the destructor.
	CloseHandle(this->globalFenceEvent);
}

void Direct3D12GraphicsService::GetGraphicsAdapterName(char* output)
{ 
    this->adapterName.copy((wchar_t*)output, this->adapterName.length());
}

GraphicsAllocationInfos Direct3D12GraphicsService::GetTextureAllocationInfos(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
	auto textureDesc = CreateTextureResourceDescription(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
	auto allocationInfos = this->graphicsDevice->GetResourceAllocationInfo(0, 1, &textureDesc);

	GraphicsAllocationInfos result = {};
	result.SizeInBytes = allocationInfos.SizeInBytes;
	result.Alignment = allocationInfos.Alignment;

	return result;
}

void* Direct3D12GraphicsService::CreateCommandQueue(enum GraphicsServiceCommandType commandQueueType)
{
	D3D12_COMMAND_QUEUE_DESC commandQueueDesc = {};
	commandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	commandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	if (commandQueueType == GraphicsServiceCommandType::Compute)
	{
		commandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COMPUTE;
	}

	else if (commandQueueType == GraphicsServiceCommandType::Copy)
	{
		commandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COPY;
	}

	ComPtr<ID3D12CommandQueue> commandQueue;
	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&commandQueueDesc, IID_PPV_ARGS(commandQueue.ReleaseAndGetAddressOf())));

	ComPtr<ID3D12Fence1> commandQueueFence;
	AssertIfFailed(this->graphicsDevice->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(commandQueueFence.ReleaseAndGetAddressOf())));

	auto commandAllocators = new ComPtr<ID3D12CommandAllocator>[CommandAllocatorsCount];

	// Init command allocators for each frame in flight
	// TODO: For multi threading support we need to allocate on allocator per frame per thread
	for (int i = 0; i < CommandAllocatorsCount; i++)
	{
		ComPtr<ID3D12CommandAllocator> commandAllocator;
		AssertIfFailed(this->graphicsDevice->CreateCommandAllocator(commandQueueDesc.Type, IID_PPV_ARGS(commandAllocator.ReleaseAndGetAddressOf())));
		commandAllocators[i] = commandAllocator;
	}

	Direct3D12CommandQueue* commandQueueStruct = new Direct3D12CommandQueue();
	commandQueueStruct->CommandQueueObject = commandQueue;
	commandQueueStruct->CommandAllocators = commandAllocators;
	commandQueueStruct->Type = commandQueueDesc.Type;
	commandQueueStruct->Fence = commandQueueFence;
	commandQueueStruct->FenceValue = 0;

	return commandQueueStruct;
}

void Direct3D12GraphicsService::SetCommandQueueLabel(void* commandQueuePointer, char* label)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	commandQueue->CommandQueueObject->SetName(wstring(label, label + strlen(label)).c_str());
	commandQueue->Fence->SetName((wstring(label, label + strlen(label)) + L"Fence").c_str());

	for (int i = 0; i < CommandAllocatorsCount; i++)
	{
		wchar_t buffer[64] = {};
  		swprintf(buffer, (wstring(label, label + strlen(label)) + L"Allocator%d").c_str(), i);
		commandQueue->CommandAllocators[i]->SetName(buffer);
	}
}

void Direct3D12GraphicsService::DeleteCommandQueue(void* commandQueuePointer)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;
	delete commandQueue;
}

void Direct3D12GraphicsService::ResetCommandQueue(void* commandQueuePointer)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;
	auto commandAllocator = commandQueue->CommandAllocators[this->currentAllocatorIndex];

	AssertIfFailed(commandAllocator->Reset());
}

unsigned long Direct3D12GraphicsService::GetCommandQueueTimestampFrequency(void* commandQueuePointer)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	uint64_t timestampFrequency;
	AssertIfFailed(commandQueue->CommandQueueObject->GetTimestampFrequency(&timestampFrequency));

	return timestampFrequency;
}

unsigned long Direct3D12GraphicsService::ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, int isAwaitable)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	// TODO: We need to free that memory somehow
	ID3D12CommandList** commandListsToExecute = new ID3D12CommandList*[commandListsLength];

	for (int i = 0; i < commandListsLength; i++)
	{
		commandListsToExecute[i] = ((Direct3D12CommandList*)commandLists[i])->CommandListObject.Get();
	}

	commandQueue->CommandQueueObject->ExecuteCommandLists(commandListsLength, commandListsToExecute);
	
	if (isAwaitable)
	{
		// TODO: Switch to an atomic increment here for multi threading
		auto fenceValue = commandQueue->FenceValue;
		commandQueue->CommandQueueObject->Signal(commandQueue->Fence.Get(), fenceValue);
		commandQueue->FenceValue = fenceValue + 1;

		return fenceValue;
	}

	return 0;
}

void Direct3D12GraphicsService::WaitForCommandQueue(void* commandQueuePointer, void* commandQueueToWaitPointer, unsigned long fenceValue)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;
	Direct3D12CommandQueue* commandQueueToWait = (Direct3D12CommandQueue*)commandQueueToWaitPointer;

	AssertIfFailed(commandQueue->CommandQueueObject->Wait(commandQueueToWait->Fence.Get(), fenceValue));
}

void Direct3D12GraphicsService::WaitForCommandQueueOnCpu(void* commandQueueToWaitPointer, unsigned long fenceValue)
{
	Direct3D12CommandQueue* commandQueueToWait = (Direct3D12CommandQueue*)commandQueueToWaitPointer;

	if (commandQueueToWait->Fence->GetCompletedValue() < fenceValue)
	{
		commandQueueToWait->Fence->SetEventOnCompletion(fenceValue, this->globalFenceEvent);
		WaitForSingleObject(this->globalFenceEvent, INFINITE);
	}
}

void* Direct3D12GraphicsService::CreateCommandList(void* commandQueuePointer)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	auto commandAllocator = commandQueue->CommandAllocators[this->currentAllocatorIndex];
	auto listType = commandQueue->Type;

	ComPtr<ID3D12GraphicsCommandList> commandList;
	AssertIfFailed(this->graphicsDevice->CreateCommandList(0, listType, commandAllocator.Get(), nullptr, IID_PPV_ARGS(commandList.ReleaseAndGetAddressOf())));

	if (listType != D3D12_COMMAND_LIST_TYPE_COPY)
	{
		ID3D12DescriptorHeap* descriptorHeaps[] = { this->globalDescriptorHeap.Get() };
		commandList->SetDescriptorHeaps(1, descriptorHeaps);
	}

	Direct3D12CommandList* commandListStruct = new Direct3D12CommandList();
	commandListStruct->CommandListObject = commandList;
	commandListStruct->Type = listType;
	commandListStruct->CommandQueue = commandQueue;

	return commandListStruct;
}

void Direct3D12GraphicsService::SetCommandListLabel(void* commandListPointer, char* label)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	commandList->CommandListObject->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteCommandList(void* commandListPointer)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	delete commandList;
}

void Direct3D12GraphicsService::ResetCommandList(void* commandListPointer)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	auto commandAllocator = commandList->CommandQueue->CommandAllocators[this->currentAllocatorIndex];

	commandList->CommandListObject->Reset(commandAllocator.Get(), nullptr);

	if (commandList->Type != D3D12_COMMAND_LIST_TYPE_COPY)
	{
		ID3D12DescriptorHeap* descriptorHeaps[] = { this->globalDescriptorHeap.Get() };
		commandList->CommandListObject->SetDescriptorHeaps(1, descriptorHeaps);
	}
}

void Direct3D12GraphicsService::CommitCommandList(void* commandListPointer)
{
	this->shaderBound = false;

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	AssertIfFailed(commandList->CommandListObject->Close());
}

void Direct3D12GraphicsService::SetShaderBuffer(void* commandListPointer, void* graphicsBufferPointer, int slot, int isReadOnly, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)graphicsBufferPointer;

	if (commandList->Type == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		commandList->CommandListObject->SetGraphicsRootShaderResourceView(slot, graphicsBuffer->BufferObject->GetGPUVirtualAddress());
	}

	else if (commandList->Type == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		// TODO: Remove that hack
		if (slot == 1)
		{
			auto gpuAddress = graphicsBuffer->BufferObject->GetGPUVirtualAddress();
			commandList->CommandListObject->SetComputeRoot32BitConstants(0, 2, &gpuAddress, 0);
			commandList->CommandListObject->SetComputeRootShaderResourceView(slot, graphicsBuffer->BufferObject->GetGPUVirtualAddress());
		}
	}
}

void* Direct3D12GraphicsService::CreateGraphicsHeap(enum GraphicsServiceHeapType type, unsigned long sizeInBytes)
{
	// Create cpu heap
	D3D12_HEAP_DESC heapDescriptor = {};

	if (type == GraphicsServiceHeapType::Upload)
	{
		heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_UPLOAD;
		heapDescriptor.Properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		heapDescriptor.SizeInBytes = sizeInBytes;
		heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
	}

	else if (type == GraphicsServiceHeapType::ReadBack)
	{
		heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_READBACK;
		heapDescriptor.Properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		heapDescriptor.SizeInBytes = sizeInBytes;
		heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;
	}

	else
	{
		heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_DEFAULT;
		heapDescriptor.SizeInBytes = sizeInBytes;
		heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES;
	}

	ComPtr<ID3D12Heap> graphicsHeap;
	AssertIfFailed(this->graphicsDevice->CreateHeap(&heapDescriptor, IID_PPV_ARGS(graphicsHeap.ReleaseAndGetAddressOf())));

	Direct3D12GraphicsHeap* graphicsHeapStruct = new Direct3D12GraphicsHeap();
	graphicsHeapStruct->HeapObject = graphicsHeap;
	graphicsHeapStruct->Type = type;

	return graphicsHeapStruct;
}

void Direct3D12GraphicsService::SetGraphicsHeapLabel(void* graphicsHeapPointer, char* label)
{
	Direct3D12GraphicsHeap* graphicsHeap = (Direct3D12GraphicsHeap*)graphicsHeapPointer;
	graphicsHeap->HeapObject->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteGraphicsHeap(void* graphicsHeapPointer)
{
	Direct3D12GraphicsHeap* graphicsHeap = (Direct3D12GraphicsHeap*)graphicsHeapPointer;
	delete graphicsHeap;
}

void* Direct3D12GraphicsService::CreateGraphicsBuffer(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes)
{ 
	Direct3D12GraphicsHeap* graphicsHeap = (Direct3D12GraphicsHeap*)graphicsHeapPointer;

	D3D12_RESOURCE_DESC resourceDesc = {};
	resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
	resourceDesc.Alignment = 0;
	resourceDesc.Width = sizeInBytes;
	resourceDesc.Height = 1;
	resourceDesc.DepthOrArraySize = 1;
	resourceDesc.MipLevels = 1;
	resourceDesc.Format = DXGI_FORMAT_UNKNOWN;
	resourceDesc.SampleDesc.Count = 1;
	resourceDesc.SampleDesc.Quality = 0;
	resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;

	auto resourceState = D3D12_RESOURCE_STATE_COPY_DEST;

	if (graphicsHeap->Type == GraphicsServiceHeapType::Upload)
	{
		resourceState = D3D12_RESOURCE_STATE_GENERIC_READ;
	}

	ComPtr<ID3D12Resource> graphicsBuffer;
	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(graphicsHeap->HeapObject.Get(), heapOffset, &resourceDesc, resourceState, nullptr, IID_PPV_ARGS(graphicsBuffer.ReleaseAndGetAddressOf())));
	
	// TODO: Resource state tracking should be moved to the engine

	Direct3D12GraphicsBuffer* graphicsBufferStruct = new Direct3D12GraphicsBuffer();
	graphicsBufferStruct->BufferObject = graphicsBuffer;
	graphicsBufferStruct->Type = graphicsHeap->Type;
	graphicsBufferStruct->ResourceDesc = resourceDesc;
	graphicsBufferStruct->ResourceState = resourceState;
	graphicsBufferStruct->CpuPointer = nullptr;

    return graphicsBufferStruct;
}

void Direct3D12GraphicsService::SetGraphicsBufferLabel(void* graphicsBufferPointer, char* label)
{
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)graphicsBufferPointer;
	graphicsBuffer->BufferObject->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteGraphicsBuffer(void* graphicsBufferPointer)
{
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)graphicsBufferPointer;
	delete graphicsBuffer;
}

void* Direct3D12GraphicsService::GetGraphicsBufferCpuPointer(void* graphicsBufferPointer)
{
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)graphicsBufferPointer;

	if (graphicsBuffer->CpuPointer != nullptr)
	{
		return graphicsBuffer->CpuPointer;
	}

	void* pointer = nullptr;
	D3D12_RANGE range = { 0, 0 };
	graphicsBuffer->BufferObject->Map(0, &range, &pointer);
	graphicsBuffer->CpuPointer = pointer;

	return pointer;
}

void* Direct3D12GraphicsService::CreateTexture(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
	Direct3D12Texture* textureStruct = new Direct3D12Texture();
	Direct3D12GraphicsHeap* graphicsHeap = (Direct3D12GraphicsHeap*)graphicsHeapPointer;

	// TODO: Support mip levels
	auto textureDesc = CreateTextureResourceDescription(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
	textureStruct->ResourceDesc = textureDesc;

	D3D12_CLEAR_VALUE* clearValue = nullptr;

	D3D12_RESOURCE_STATES initialState = usage == GraphicsTextureUsage::ShaderRead ? D3D12_RESOURCE_STATE_COPY_DEST : D3D12_RESOURCE_STATE_GENERIC_READ;

	if (usage == GraphicsTextureUsage::RenderTarget)
	{
		if (textureFormat != GraphicsTextureFormat::Depth32Float)
		{
			D3D12_CLEAR_VALUE rawClearValue = {};
			rawClearValue.Format = ConvertTextureFormat(textureFormat);
			clearValue = &rawClearValue;
		}
	}

	ComPtr<ID3D12Resource> gpuTexture;
	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(graphicsHeap->HeapObject.Get(), heapOffset, &textureDesc, initialState, clearValue, IID_PPV_ARGS(gpuTexture.ReleaseAndGetAddressOf())));
	textureStruct->TextureObject = gpuTexture;
	textureStruct->ResourceState = initialState;

	UINT64 uploadBufferSize;
	D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint;

	this->graphicsDevice->GetCopyableFootprints(&textureDesc, 0, 1, 0, &footPrint, nullptr, nullptr, &uploadBufferSize);
	textureStruct->FootPrint = footPrint;

	// TODO: Move descriptors creation from here
	if (textureFormat != GraphicsTextureFormat::Depth32Float)
	{
		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Format = ConvertTextureFormat(textureFormat);
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Texture2D.MipLevels = 1;//mipLevels;

		auto globalDescriptorHeapHandle = this->globalDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
		globalDescriptorHeapHandle.ptr += this->currentGlobalDescriptorOffset;

		this->graphicsDevice->CreateShaderResourceView(gpuTexture.Get(), &srvDesc, globalDescriptorHeapHandle);
		textureStruct->SrvTextureDescriptorOffset = this->currentGlobalDescriptorOffset;
		this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;

		if (usage == GraphicsTextureUsage::RenderTarget)
		{
			D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
			rtvDesc.Format = ConvertTextureFormat(textureFormat);
			rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

			auto globalRtvDescriptorHeapHandle = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
			globalRtvDescriptorHeapHandle.ptr += this->currentGlobalRtvDescriptorOffset;

			this->graphicsDevice->CreateRenderTargetView(gpuTexture.Get(), &rtvDesc, globalRtvDescriptorHeapHandle);
			textureStruct->TextureDescriptorOffset = this->currentGlobalRtvDescriptorOffset;
			this->currentGlobalRtvDescriptorOffset += this->globalRtvDescriptorHandleSize;
		}

		else if (usage == GraphicsTextureUsage::ShaderWrite)
		{
			// UAV View
			D3D12_UNORDERED_ACCESS_VIEW_DESC  uavDesc = {};
			uavDesc.Format = ConvertTextureFormat(textureFormat);
			uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
			uavDesc.Texture2D.MipSlice = 0;

			globalDescriptorHeapHandle = this->globalDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
			globalDescriptorHeapHandle.ptr += this->currentGlobalDescriptorOffset;

			this->graphicsDevice->CreateUnorderedAccessView(gpuTexture.Get(), nullptr, &uavDesc, globalDescriptorHeapHandle);
			textureStruct->UavTextureDescriptorOffset = this->currentGlobalDescriptorOffset;
			this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;
		}
	}

	return textureStruct;
}

void Direct3D12GraphicsService::SetTextureLabel(void* texturePointer, char* label)
{
	Direct3D12Texture* texture = (Direct3D12Texture*)texturePointer;
	texture->TextureObject->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteTexture(void* texturePointer)
{ 
	Direct3D12Texture* texture = (Direct3D12Texture*)texturePointer;

	if (!texture->IsPresentTexture)
	{
		delete texture;
	}
}

void* Direct3D12GraphicsService::CreateSwapChain(void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
	swapChainDesc.BufferCount = RenderBuffersCount;
	swapChainDesc.Width = width;
	swapChainDesc.Height = height;
	swapChainDesc.Format = ConvertTextureFormat(textureFormat, true);
	swapChainDesc.Scaling = DXGI_SCALING_STRETCH;
	swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
	swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_IGNORE;
	swapChainDesc.SampleDesc = { 1, 0 };

	DXGI_SWAP_CHAIN_FULLSCREEN_DESC swapChainFullScreenDesc = {};
	swapChainFullScreenDesc.Windowed = true;
	
	ComPtr<IDXGISwapChain3> swapChain;
	AssertIfFailed(dxgiFactory->CreateSwapChainForHwnd(commandQueue->CommandQueueObject.Get(), (HWND)windowPointer, &swapChainDesc, &swapChainFullScreenDesc, nullptr, (IDXGISwapChain1**)swapChain.ReleaseAndGetAddressOf()));

	Direct3D12SwapChain* swapChainStructure = new Direct3D12SwapChain();
	swapChainStructure->SwapChainObject = swapChain;
	swapChainStructure->CommandQueue = commandQueue;

	D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
	rtvDesc.Format = ConvertTextureFormat(textureFormat);
	rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		ComPtr<ID3D12Resource> backBuffer;
		AssertIfFailed(swapChain->GetBuffer(i, IID_PPV_ARGS(backBuffer.ReleaseAndGetAddressOf())));

		wchar_t buff[64] = {};
  		swprintf(buff, L"BackBufferRenderTarget%d", i);
		backBuffer->SetName(buff);

		Direct3D12Texture* backBufferTexture = new Direct3D12Texture();
		backBufferTexture->TextureObject = backBuffer;
		backBufferTexture->ResourceState = D3D12_RESOURCE_STATE_PRESENT;
		backBufferTexture->IsPresentTexture = true;
		backBufferTexture->ResourceDesc = CreateTextureResourceDescription(textureFormat, GraphicsTextureUsage::RenderTarget, width, height, 1, 1, 1);

		auto globalRtvDescriptorHeapHandle = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
		globalRtvDescriptorHeapHandle.ptr += this->currentGlobalRtvDescriptorOffset;

		this->graphicsDevice->CreateRenderTargetView(backBuffer.Get(), &rtvDesc, globalRtvDescriptorHeapHandle);
		backBufferTexture->TextureDescriptorOffset = this->currentGlobalRtvDescriptorOffset;
		this->currentGlobalRtvDescriptorOffset += this->globalRtvDescriptorHandleSize;

		swapChainStructure->BackBufferTextures[i] = backBufferTexture;
	}

	return swapChainStructure;
}

void Direct3D12GraphicsService::ResizeSwapChain(void* swapChainPointer, int width, int height)
{
	Direct3D12SwapChain* swapChain = (Direct3D12SwapChain*)swapChainPointer;

	this->WaitForCommandQueueOnCpu(swapChain->CommandQueue, swapChain->CommandQueue->FenceValue - 1);

	D3D12_RESOURCE_DESC backBufferDesc;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		backBufferDesc = swapChain->BackBufferTextures[i]->ResourceDesc;
		delete swapChain->BackBufferTextures[i];
	}

	backBufferDesc.Width = width;
	backBufferDesc.Height = height;

	AssertIfFailed(swapChain->SwapChainObject->ResizeBuffers(RenderBuffersCount, width, height, DXGI_FORMAT_UNKNOWN, 0));

	D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
	rtvDesc.Format = backBufferDesc.Format;
	rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		ComPtr<ID3D12Resource> backBuffer;
		AssertIfFailed(swapChain->SwapChainObject->GetBuffer(i, IID_PPV_ARGS(backBuffer.ReleaseAndGetAddressOf())));

		wchar_t buff[64] = {};
  		swprintf(buff, L"BackBufferRenderTarget%d", i);
		backBuffer->SetName(buff);

		Direct3D12Texture* backBufferTexture = new Direct3D12Texture();
		backBufferTexture->TextureObject = backBuffer;
		backBufferTexture->ResourceState = D3D12_RESOURCE_STATE_PRESENT;
		backBufferTexture->IsPresentTexture = true;
		backBufferTexture->ResourceDesc = backBufferDesc;

		auto globalRtvDescriptorHeapHandle = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
		globalRtvDescriptorHeapHandle.ptr += this->currentGlobalRtvDescriptorOffset;

		this->graphicsDevice->CreateRenderTargetView(backBuffer.Get(), &rtvDesc, globalRtvDescriptorHeapHandle);
		backBufferTexture->TextureDescriptorOffset = this->currentGlobalRtvDescriptorOffset;
		this->currentGlobalRtvDescriptorOffset += this->globalRtvDescriptorHandleSize;

		swapChain->BackBufferTextures[i] = backBufferTexture;
	}
}

void* Direct3D12GraphicsService::GetSwapChainBackBufferTexture(void* swapChainPointer)
{
	Direct3D12SwapChain* swapChain = (Direct3D12SwapChain*)swapChainPointer;
	return swapChain->BackBufferTextures[swapChain->SwapChainObject->GetCurrentBackBufferIndex()];
}

unsigned long Direct3D12GraphicsService::PresentSwapChain(void* swapChainPointer)
{
	Direct3D12SwapChain* swapChain = (Direct3D12SwapChain*)swapChainPointer;
	AssertIfFailed(swapChain->SwapChainObject->Present(1, 0));

	// TODO: Switch to an atomic increment here for multi threading
	auto fenceValue = swapChain->CommandQueue->FenceValue;
	swapChain->CommandQueue->CommandQueueObject->Signal(swapChain->CommandQueue->Fence.Get(), fenceValue);
	swapChain->CommandQueue->FenceValue = fenceValue + 1;

	// TODO: Do something better here
	this->currentAllocatorIndex = (this->currentAllocatorIndex + 1) % RenderBuffersCount;

	return fenceValue;
}

// TODO: To remove
struct IndirectCommand
{
	D3D12_GPU_VIRTUAL_ADDRESS cbv;
	D3D12_DRAW_ARGUMENTS drawArguments;
};

void* Direct3D12GraphicsService::CreateIndirectCommandBuffer(int maxCommandCount)
{ 
	if (this->currentShaderIndirectCommand.RootSignature == nullptr)
	{
		// TODO: This is a hack
		return nullptr;
	}

	// TODO: Remove that hack, we need to pass the shader definition to the create method
	auto indirectCommandShader = this->currentShaderIndirectCommand;
	
	// Each command consists of a CBV update and a DrawInstanced call.
	D3D12_INDIRECT_ARGUMENT_DESC argumentDescs[2] = {};
	argumentDescs[0].Type = D3D12_INDIRECT_ARGUMENT_TYPE_SHADER_RESOURCE_VIEW;
	argumentDescs[0].ShaderResourceView.RootParameterIndex = 0;
	argumentDescs[1].Type = D3D12_INDIRECT_ARGUMENT_TYPE_DRAW;

	D3D12_COMMAND_SIGNATURE_DESC commandSignatureDesc = {};
	commandSignatureDesc.pArgumentDescs = argumentDescs;
	commandSignatureDesc.NumArgumentDescs = 2;
	commandSignatureDesc.ByteStride = sizeof(IndirectCommand);

	ComPtr<ID3D12CommandSignature> commandSignature;
	AssertIfFailed(this->graphicsDevice->CreateCommandSignature(&commandSignatureDesc, indirectCommandShader.RootSignature.Get(), IID_PPV_ARGS(&commandSignature)));
	// AssertIfFailed(this->graphicsDevice->CreateCommandSignature(&commandSignatureDesc, nullptr, IID_PPV_ARGS(&commandSignature)));

	// int Direct3D12GraphicsService::CreateGraphicsBufferOld(unsigned int graphicsBufferId, int sizeInBytes, int isWriteOnly, char* label)
	// return CreateGraphicsBufferOld(indirectCommandBufferId, maxCommandCount * sizeof(IndirectCommand), false, "");

	Direct3D12IndirectCommandBuffer* indirectCommandBufferStruct = new Direct3D12IndirectCommandBuffer();
	indirectCommandBufferStruct->CommandSignature = commandSignature;

	return indirectCommandBufferStruct;
}

void Direct3D12GraphicsService::SetIndirectCommandBufferLabel(void* indirectCommandBufferPointer, char* label)
{
	// if (!this->graphicsBuffers.count(indirectCommandBufferId))
	// {
	// 	return;
	// }

	// this->graphicsBuffers[indirectCommandBufferId]->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteIndirectCommandBuffer(void* indirectCommandBufferPointer)
{

}

void* Direct3D12GraphicsService::CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length)
{
	auto type = D3D12_QUERY_HEAP_TYPE_TIMESTAMP;

	if (queryBufferType == GraphicsQueryBufferType::CopyTimestamp)
	{
		type = D3D12_QUERY_HEAP_TYPE_COPY_QUEUE_TIMESTAMP;
	}

	D3D12_QUERY_HEAP_DESC heapDesc = {};
	heapDesc.Count = QueryHeapMaxSize;
	heapDesc.NodeMask = 0;
	heapDesc.Type = type;

	ComPtr<ID3D12QueryHeap> queryBuffer;
	AssertIfFailed(this->graphicsDevice->CreateQueryHeap(&heapDesc, IID_PPV_ARGS(queryBuffer.ReleaseAndGetAddressOf())));

	Direct3D12QueryBuffer* queryBufferStruct = new Direct3D12QueryBuffer();
	queryBufferStruct->QueryBufferObject = queryBuffer;
	queryBufferStruct->Type = type;

	return queryBufferStruct;
}

void Direct3D12GraphicsService::SetQueryBufferLabel(void* queryBufferPointer, char* label)
{
	auto internalLabel = wstring(label, label + strlen(label));

	Direct3D12QueryBuffer* queryBuffer = (Direct3D12QueryBuffer*)queryBufferPointer;
	queryBuffer->QueryBufferObject->SetName(internalLabel.c_str());
}
        
void Direct3D12GraphicsService::DeleteQueryBuffer(void* queryBufferPointer)
{
	Direct3D12QueryBuffer* queryBuffer = (Direct3D12QueryBuffer*)queryBufferPointer;
	delete queryBuffer;
}

void* Direct3D12GraphicsService::CreateShader(char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{ 
	Direct3D12Shader* shader = new Direct3D12Shader();

	auto currentDataPtr = (unsigned char*)shaderByteCode;

	auto rootSignatureByteCodeLength = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);
	auto rootSignatureBlob = CreateShaderBlob(currentDataPtr, rootSignatureByteCodeLength);
	currentDataPtr += rootSignatureByteCodeLength;

	ComPtr<ID3D12RootSignature> rootSignature;
	AssertIfFailed(this->graphicsDevice->CreateRootSignature(0, rootSignatureBlob->GetBufferPointer(), rootSignatureBlob->GetBufferSize(), IID_PPV_ARGS(rootSignature.ReleaseAndGetAddressOf())));

	shader->RootSignature = rootSignature;
	
	auto shaderTableCount = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);

	for (int i = 0; i < shaderTableCount; i++)
	{
		auto entryPointNameLength = (*(int*)currentDataPtr);
		currentDataPtr += sizeof(int);

		auto entryPointNameTemp = new char[entryPointNameLength + 1];
		entryPointNameTemp[entryPointNameLength] = '\0';

		memcpy(entryPointNameTemp, (char*)currentDataPtr, entryPointNameLength);
		auto entryPointName = string(entryPointNameTemp);
		currentDataPtr += entryPointNameLength;

		auto shaderByteCodeLength = (*(int*)currentDataPtr);
		currentDataPtr += sizeof(int);

		auto shaderBlob = CreateShaderBlob(currentDataPtr, shaderByteCodeLength);
		currentDataPtr += shaderByteCodeLength;

		if (entryPointName == "VertexMain")
		{
			shader->VertexShaderMethod = shaderBlob;
		}

		else if (entryPointName == "PixelMain")
		{
			shader->PixelShaderMethod = shaderBlob;
		}

		else if (entryPointName == string(computeShaderFunction))
		{
			shader->ComputeShaderMethod = shaderBlob;
		}
	}

    return shader;
}

void Direct3D12GraphicsService::SetShaderLabel(void* shaderPointer, char* label)
{
	Direct3D12Shader* shader = (Direct3D12Shader*)shaderPointer;

	// TODO: Remove that hack
	auto rootSignatureName = wstring(label, label + strlen(label));

	if (rootSignatureName.compare(L"RenderMeshInstanceShader") == 0)
	{
		this->currentShaderIndirectCommand = *shader;
	}

	shader->RootSignature->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteShader(void* shaderPointer)
{ 
	Direct3D12Shader* shader = (Direct3D12Shader*)shaderPointer;
	delete shader;
}

void* Direct3D12GraphicsService::CreatePipelineState(void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{ 
	if (shaderPointer == nullptr)
	{
		return nullptr;
	}

	Direct3D12Shader* shader = (Direct3D12Shader*)shaderPointer;

	ComPtr<ID3D12PipelineState> pipelineState;

	if (shader->ComputeShaderMethod == nullptr)
	{
		auto primitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;

		if (renderPassDescriptor.PrimitiveType == GraphicsPrimitiveType::Line)
		{
			primitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE;
		}

		D3D12_GRAPHICS_PIPELINE_STATE_DESC psoDesc = {};
		psoDesc.pRootSignature = shader->RootSignature.Get();

		psoDesc.VS = { shader->VertexShaderMethod->GetBufferPointer(), shader->VertexShaderMethod->GetBufferSize() };
		psoDesc.PS = { shader->PixelShaderMethod->GetBufferPointer(), shader->PixelShaderMethod->GetBufferSize() };

		psoDesc.SampleMask = 0xFFFFFF;
		psoDesc.PrimitiveTopologyType = primitiveTopologyType;

		if (!renderPassDescriptor.RenderTarget1TexturePointer.HasValue && !renderPassDescriptor.DepthTexturePointer.HasValue)
		{
			psoDesc.NumRenderTargets = 1;
			psoDesc.RTVFormats[0] = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB; // TODO: Fill Correct Back Buffer Format
		}

		else
		{
			psoDesc.NumRenderTargets = 1;
			psoDesc.RTVFormats[0] = ConvertTextureFormat(renderPassDescriptor.RenderTarget1TextureFormat.Value);
		}

		psoDesc.SampleDesc.Count = 1;
		psoDesc.RasterizerState.FillMode = D3D12_FILL_MODE_SOLID;
		psoDesc.RasterizerState.CullMode = D3D12_CULL_MODE_BACK;
		psoDesc.RasterizerState.FrontCounterClockwise = false;
		psoDesc.RasterizerState.DepthBias = D3D12_DEFAULT_DEPTH_BIAS;
		psoDesc.RasterizerState.DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP;
		psoDesc.RasterizerState.SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
		psoDesc.RasterizerState.DepthClipEnable = true;
		psoDesc.RasterizerState.MultisampleEnable = false;
		psoDesc.RasterizerState.AntialiasedLineEnable = false;
		psoDesc.RasterizerState.ForcedSampleCount = 0;
		psoDesc.RasterizerState.ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;
		psoDesc.DepthStencilState.DepthEnable = false;
		psoDesc.DepthStencilState.StencilEnable = false;
		psoDesc.BlendState.AlphaToCoverageEnable = false;
		psoDesc.BlendState.IndependentBlendEnable = false;

		if (renderPassDescriptor.RenderTarget1BlendOperation.HasValue)
		{
			auto blendOperation = renderPassDescriptor.RenderTarget1BlendOperation.Value;
			psoDesc.BlendState.RenderTarget[0] = InitBlendState(blendOperation);
		}

		else
		{
			psoDesc.BlendState.RenderTarget[0] = InitBlendState(GraphicsBlendOperation::None);
		}

		AssertIfFailed(this->graphicsDevice->CreateGraphicsPipelineState(&psoDesc, IID_PPV_ARGS(pipelineState.ReleaseAndGetAddressOf())));
	}

	else
	{
		D3D12_COMPUTE_PIPELINE_STATE_DESC psoDesc = {};
		psoDesc.pRootSignature = shader->RootSignature.Get();
		psoDesc.CS = { shader->ComputeShaderMethod->GetBufferPointer(), shader->ComputeShaderMethod->GetBufferSize() };

		AssertIfFailed(this->graphicsDevice->CreateComputePipelineState(&psoDesc, IID_PPV_ARGS(pipelineState.ReleaseAndGetAddressOf())));
	}

	Direct3D12PipelineState* pipelineStateStruct = new Direct3D12PipelineState();
	pipelineStateStruct->PipelineStateObject = pipelineState;

    return pipelineStateStruct;
}

void Direct3D12GraphicsService::SetPipelineStateLabel(void* pipelineStatePointer, char* label)
{
	Direct3D12PipelineState* pipelineState = (Direct3D12PipelineState*)pipelineStatePointer;
	pipelineState->PipelineStateObject->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeletePipelineState(void* pipelineStatePointer)
{ 
	Direct3D12PipelineState* pipelineState = (Direct3D12PipelineState*)pipelineStatePointer;
	delete pipelineState;
}

void Direct3D12GraphicsService::SetShaderBuffers(void* commandListPointer, void** graphicsBufferPointerList, int graphicsBufferPointerListLength, int slot, int index)
{ 
	
}

void Direct3D12GraphicsService::SetShaderTexture(void* commandListPointer, void* texturePointer, int slot, int isReadOnly, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}
	
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12Texture* texture = (Direct3D12Texture*)texturePointer;

	if (commandList->Type == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_GENERIC_READ);

		auto descriptorHeapdOffset = texture->SrvTextureDescriptorOffset;

		D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
		descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

		commandList->CommandListObject->SetGraphicsRootDescriptorTable(slot, descriptorHeapHandle);
	}

	else if (commandList->Type == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		if (isReadOnly)
		{
			TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_GENERIC_READ);
			
			auto descriptorHeapdOffset = texture->SrvTextureDescriptorOffset;

			D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
			descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

			commandList->CommandListObject->SetComputeRootDescriptorTable(slot, descriptorHeapHandle);
		}

		else
		{
			TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_GENERIC_READ);
			
			auto descriptorHeapdOffset = texture->UavTextureDescriptorOffset;

			D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
			descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

			commandList->CommandListObject->SetComputeRootDescriptorTable(slot, descriptorHeapHandle);
		}
	}
}

int currentDebugDescriptorIndex = 0;

void Direct3D12GraphicsService::SetShaderTextures(void* commandListPointer, void** texturePointerList, int texturePointerListLength, int slot, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;

	// TODO: Remove that hack

	if (commandList->Type != D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		return;
	}

	ComPtr<ID3D12DescriptorHeap> srvDescriptorHeap;

	// Create Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
	descriptorHeapDesc.NumDescriptors = texturePointerListLength;
	descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(srvDescriptorHeap.ReleaseAndGetAddressOf())));

	this->debugDescriptorHeaps[currentDebugDescriptorIndex++] = srvDescriptorHeap;

	int srvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	auto heapPtr = srvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
	
	for (int i = 0; i < texturePointerListLength; i++)
	{
		Direct3D12Texture* texture = (Direct3D12Texture*)texturePointerList[i];

		if (texture->ResourceDesc.Format == DXGI_FORMAT_D32_FLOAT)
		{
			return;
		}

		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Format = texture->ResourceDesc.Format;
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Texture2D.MipLevels = texture->ResourceDesc.MipLevels;
		this->graphicsDevice->CreateShaderResourceView(texture->TextureObject.Get(), &srvDesc, heapPtr);

		heapPtr.ptr += srvDescriptorHandleSize;

		TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_GENERIC_READ);
	}

	// TODO: Change that because if the next shader invoke needs the global heap it will not work

	ID3D12DescriptorHeap* descriptorHeaps[] = { srvDescriptorHeap.Get() };
	commandList->CommandListObject->SetDescriptorHeaps(1, descriptorHeaps);
	commandList->CommandListObject->SetGraphicsRootDescriptorTable(slot, srvDescriptorHeap->GetGPUDescriptorHandleForHeapStart());
}

void Direct3D12GraphicsService::SetShaderIndirectCommandList(void* commandListPointer, void* indirectCommandListPointer, int slot, int index){ }
void Direct3D12GraphicsService::SetShaderIndirectCommandLists(void* commandListPointer, void** indirectCommandListPointerList, int indirectCommandListPointerListLength, int slot, int index)
{ 
	return;
	/*
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;

	// TODO: Remove that hack
	if (slot != 30006)
	{
		return;
	}

	auto bufferId = indirectCommandListIdList[3];
	auto gpuBuffer = this->graphicsBuffers[bufferId];

	TransitionBufferToState(commandList, bufferId, D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

	auto descriptorHeapdOffset = this->uavBufferDescriptorOffets[bufferId];

	D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
	descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

	commandList->CommandListObject->SetComputeRootDescriptorTable(2, descriptorHeapHandle);

	*/
}

void Direct3D12GraphicsService::CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int sizeInBytes)
{ 
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12GraphicsBuffer* destinationGraphicsBuffer = (Direct3D12GraphicsBuffer*)destinationGraphicsBufferPointer;
	Direct3D12GraphicsBuffer* sourceGraphicsBuffer = (Direct3D12GraphicsBuffer*)sourceGraphicsBufferPointer;

	if (destinationGraphicsBuffer->Type == GraphicsServiceHeapType::ReadBack && destinationGraphicsBuffer->CpuPointer != nullptr)
	{
		D3D12_RANGE range = { 0, 0 };
		destinationGraphicsBuffer->BufferObject->Unmap(0, &range);
		destinationGraphicsBuffer->CpuPointer = nullptr;
	}

	commandList->CommandListObject->CopyBufferRegion(destinationGraphicsBuffer->BufferObject.Get(), 0, sourceGraphicsBuffer->BufferObject.Get(), 0, sizeInBytes);
}

void Direct3D12GraphicsService::CopyDataToTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{
	// TODO: For the moment it only takes into account the mip level
	if (mipLevel > 0)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12Texture* destinationTexture = (Direct3D12Texture*)destinationTexturePointer;
	Direct3D12GraphicsBuffer* sourceGraphicsBuffer = (Direct3D12GraphicsBuffer*)sourceGraphicsBufferPointer;

	TransitionTextureToState(commandList, destinationTexture, D3D12_RESOURCE_STATE_COPY_DEST);

	D3D12_TEXTURE_COPY_LOCATION destinationLocation = {};
	destinationLocation.pResource = destinationTexture->TextureObject.Get();
	destinationLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
	destinationLocation.SubresourceIndex = mipLevel;

	D3D12_TEXTURE_COPY_LOCATION sourceLocation = {};
	sourceLocation.pResource = sourceGraphicsBuffer->BufferObject.Get();
	sourceLocation.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
	sourceLocation.PlacedFootprint = destinationTexture->FootPrint;

	commandList->CommandListObject->CopyTextureRegion(&destinationLocation, 0, 0, 0, &sourceLocation, nullptr);
}

void Direct3D12GraphicsService::CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12Texture* destinationTexture = (Direct3D12Texture*)destinationTexturePointer;
	Direct3D12Texture* sourceTexture = (Direct3D12Texture*)sourceTexturePointer;

	commandList->CommandListObject->CopyResource(destinationTexture->TextureObject.Get(), sourceTexture->TextureObject.Get());
}

void Direct3D12GraphicsService::ResetIndirectCommandList(void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount){ }
void Direct3D12GraphicsService::OptimizeIndirectCommandList(void* commandListPointer, void* indirectCommandListPointer, int maxCommandCount){ }

struct Vector3 Direct3D12GraphicsService::DispatchThreads(void* commandListPointer, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ)
{ 
	if (!this->shaderBound)
	{
		return Vector3 { 32, 32, 1 };
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;

	// TODO: Change that
	commandList->CommandListObject->Dispatch(ceil(threadCountX / 32.0f), ceil(threadCountY / 32.0f), 1);

    return Vector3 { 32, 32, 1 };
}

void Direct3D12GraphicsService::BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
	// TODO: Switch to DX12 render passes

	auto renderDescriptor = renderPassDescriptor;

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	commandList->RenderPassDescriptor = renderDescriptor;

	if (renderDescriptor.RenderTarget1TexturePointer.HasValue)
	{
		Direct3D12Texture* texture = (Direct3D12Texture*)renderDescriptor.RenderTarget1TexturePointer.Value;

		D3D12_CPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
		descriptorHeapHandle.ptr = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + texture->TextureDescriptorOffset;

		TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_RENDER_TARGET);
		commandList->CommandListObject->OMSetRenderTargets(1, &descriptorHeapHandle, false, nullptr);

		if (renderDescriptor.RenderTarget1ClearColor.HasValue)
		{
			// float clearColor[4] = { renderDescriptor.RenderTarget1ClearColor.Value.X, renderDescriptor.RenderTarget1ClearColor.Value.Y, renderDescriptor.RenderTarget1ClearColor.Value.Z, renderDescriptor.RenderTarget1ClearColor.Value.W };
			float clearColor[4] = {};
			commandList->CommandListObject->ClearRenderTargetView(descriptorHeapHandle, clearColor, 0, nullptr);
		}

		D3D12_VIEWPORT viewport = {};
		viewport.Width = (float)texture->ResourceDesc.Width;
		viewport.Height = (float)texture->ResourceDesc.Height;
		viewport.MaxDepth = 1.0f;
		commandList->CommandListObject->RSSetViewports(1, &viewport);

		D3D12_RECT scissorRect = {};
		scissorRect.right = (long)texture->ResourceDesc.Width;
		scissorRect.bottom = (long)texture->ResourceDesc.Height;
		commandList->CommandListObject->RSSetScissorRects(1, &scissorRect);
	}
}

void Direct3D12GraphicsService::EndRenderPass(void* commandListPointer)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	auto renderDescriptor = commandList->RenderPassDescriptor;

	if (renderDescriptor.RenderTarget1TexturePointer.HasValue)
	{
		Direct3D12Texture* texture = (Direct3D12Texture*)renderDescriptor.RenderTarget1TexturePointer.Value;

		if (!texture->IsPresentTexture)
		{
			TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_GENERIC_READ);
		}

		else
		{
			TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_PRESENT);
		}
	}
}

void Direct3D12GraphicsService::SetPipelineState(void* commandListPointer, void* pipelineStatePointer)
{ 
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12PipelineState* pipelineState = (Direct3D12PipelineState*)pipelineStatePointer;

	if (pipelineState == nullptr)
	{
		return;
	}

	commandList->CommandListObject->SetPipelineState(pipelineState->PipelineStateObject.Get());
}

void Direct3D12GraphicsService::SetShader(void* commandListPointer, void* shaderPointer)
{ 
	if (shaderPointer == nullptr)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12Shader* shader = (Direct3D12Shader*)shaderPointer;

	if (commandList->Type == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		commandList->CommandListObject->SetGraphicsRootSignature(shader->RootSignature.Get());
	}

	else if (commandList->Type == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandList->CommandListObject->SetComputeRootSignature(shader->RootSignature.Get());
	}

	this->shaderBound = true;
}

void Direct3D12GraphicsService::ExecuteIndirectCommandBuffer(void* commandListPointer, void* indirectCommandBufferPointer, int maxCommandCount)
{ 
	return;

	// if (!this->shaderBound)
	// {
	// 	return;
	// }

	// Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	// auto indirectCommandBuffer = this->graphicsBuffers[indirectCommandBufferId];
	// TransitionBufferToState(commandList, indirectCommandBufferId, D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT);

	// auto signature = this->indirectCommandBufferSignatures[indirectCommandBufferId];

	// commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

	// // TODO: Compute the count in the shader?
	// commandList->CommandListObject->ExecuteIndirect(signature.Get(), 1, indirectCommandBuffer.Get(), 0, nullptr, 0);
}

void Direct3D12GraphicsService::SetIndexBuffer(void* commandListPointer, void* graphicsBufferPointer)
{ 
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)graphicsBufferPointer;

	D3D12_INDEX_BUFFER_VIEW indexBufferView = {};
	indexBufferView.BufferLocation = graphicsBuffer->BufferObject->GetGPUVirtualAddress();
	indexBufferView.SizeInBytes = graphicsBuffer->ResourceDesc.Width;
	indexBufferView.Format = DXGI_FORMAT_R32_UINT;

	commandList->CommandListObject->IASetIndexBuffer(&indexBufferView);
}

void Direct3D12GraphicsService::DrawIndexedPrimitives(void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;

	if (primitiveType == GraphicsPrimitiveType::TriangleStrip)
	{
		commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
	}

	else if (primitiveType == GraphicsPrimitiveType::Line)
	{
		commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_LINELIST);
	}

	else
	{
		commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
	}

	commandList->CommandListObject->DrawIndexedInstanced(indexCount, instanceCount, startIndex, 0, baseInstanceId);
}

void Direct3D12GraphicsService::DrawPrimitives(void* commandListPointer, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;

	if (primitiveType == GraphicsPrimitiveType::TriangleStrip)
	{
		commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
	}

	else if (primitiveType == GraphicsPrimitiveType::Line)
	{
		commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_LINELIST);
	}

	else
	{
		commandList->CommandListObject->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
	}

	commandList->CommandListObject->DrawInstanced(vertexCount, 1, startVertex, 0);
}

void Direct3D12GraphicsService::QueryTimestamp(void* commandListPointer, void* queryBufferPointer, int index)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12QueryBuffer* queryBuffer = (Direct3D12QueryBuffer*)queryBufferPointer;

	// TODO: Copy queries are crashing
	if (commandList->Type != D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandList->CommandListObject->EndQuery(queryBuffer->QueryBufferObject.Get(), D3D12_QUERY_TYPE_TIMESTAMP, index);
	}
}

void Direct3D12GraphicsService::ResolveQueryData(void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12QueryBuffer* queryBuffer = (Direct3D12QueryBuffer*)queryBufferPointer;
	Direct3D12GraphicsBuffer* destinationBuffer = (Direct3D12GraphicsBuffer*)destinationBufferPointer;

	if (destinationBuffer->CpuPointer != nullptr)
	{
		D3D12_RANGE range = { 0, 0 };
		destinationBuffer->BufferObject->Unmap(0, &range);
		destinationBuffer->CpuPointer = nullptr;
	}

	// TODO: Copy queries are crashing
	if (commandList->Type != D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandList->CommandListObject->ResolveQueryData(queryBuffer->QueryBufferObject.Get(), D3D12_QUERY_TYPE_TIMESTAMP, startIndex, endIndex, destinationBuffer->BufferObject.Get(), 0);
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
				this->adapterName = wstring(dxgiAdapterDesc1.Description) + L" (DirectX 12)";
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

	this->globalFenceEvent = CreateEventA(nullptr, false, false, nullptr);

	return true;
}

bool Direct3D12GraphicsService::CreateHeaps()
{
	// Create global Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
	descriptorHeapDesc.NumDescriptors = 100000; //TODO: Change that
	descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(this->globalDescriptorHeap.ReleaseAndGetAddressOf())));
	this->globalDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	this->currentGlobalDescriptorOffset = 0;

	// Create global RTV Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvDescriptorHeapDesc = {};
	rtvDescriptorHeapDesc.NumDescriptors = 100000; //TODO: Change that
	rtvDescriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvDescriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&rtvDescriptorHeapDesc, IID_PPV_ARGS(this->globalRtvDescriptorHeap.ReleaseAndGetAddressOf())));
	this->globalRtvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
	this->currentGlobalRtvDescriptorOffset = 0;

	return true;
}

// TODO: Make it generic to all resource types
void Direct3D12GraphicsService::TransitionTextureToState(Direct3D12CommandList* commandList, Direct3D12Texture* texture, D3D12_RESOURCE_STATES destinationState)
{
	if (texture->ResourceState != destinationState)
	{
		commandList->CommandListObject->ResourceBarrier(1, &CreateTransitionResourceBarrier(texture->TextureObject.Get(), texture->ResourceState, destinationState));
		texture->ResourceState = destinationState;
	}
}

// TODO: Make it generic to all resource types
void Direct3D12GraphicsService::TransitionBufferToState(Direct3D12CommandList* commandList, Direct3D12GraphicsBuffer* graphicsBuffer, D3D12_RESOURCE_STATES destinationState)
{
	if (graphicsBuffer->ResourceState != destinationState)
	{
		commandList->CommandListObject->ResourceBarrier(1, &CreateTransitionResourceBarrier(graphicsBuffer->BufferObject.Get(), graphicsBuffer->ResourceState, destinationState));
		graphicsBuffer->ResourceState = destinationState;
	}
}
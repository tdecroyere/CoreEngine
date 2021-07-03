#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"
#include "Direct3D12GraphicsServiceUtils.h"
#include "WindowsNativeUIServiceUtils.h"

using namespace std;
using namespace Microsoft::WRL;

#define GetAlignedValue(value, alignement) (value + (alignement - (value % alignement)) % alignement)

Direct3D12GraphicsService::Direct3D12GraphicsService()
{
	isDirect3d = true;
	
	this->isWaitingForGlobalFence = false;
    UINT createFactoryFlags = 0;

#ifdef DEBUG
	// If the project is in a debug build, enable debugging via SDK Layers.
	AssertIfFailed(D3D12GetDebugInterface(IID_PPV_ARGS(this->debugController.GetAddressOf())));

	if (this->debugController)
	{
		this->debugController->EnableDebugLayer();
	}

    createFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;

	// UUID experimentalFeatures[] = { D3D12ExperimentalShaderModels };
	// AssertIfFailed(D3D12EnableExperimentalFeatures(1, experimentalFeatures, NULL, NULL));
#endif

	// this->window = window;
	AssertIfFailed(CreateDXGIFactory2(createFactoryFlags, IID_PPV_ARGS(this->dxgiFactory.ReleaseAndGetAddressOf())));

	auto graphicsAdapter = FindGraphicsAdapter(dxgiFactory);
	AssertIfFailed(CreateDevice(dxgiFactory, graphicsAdapter));
	//AssertIfFailed(CreateOrResizeSwapChain(width, height));
	AssertIfFailed(CreateHeaps());

#ifdef DEBUG
	EnableDebugLayer();
#endif
}

Direct3D12GraphicsService::~Direct3D12GraphicsService()
{
	// Ensure that the GPU is no longer referencing resources that are about to be
	// cleaned up by the destructor.
	CloseHandle(this->globalFenceEvent);

#ifdef DEBUG
	this->dxgiDebug->ReportLiveObjects(DXGI_DEBUG_ALL, DXGI_DEBUG_RLO_SUMMARY);
#endif
}

void Direct3D12GraphicsService::GetGraphicsAdapterName(char* output)
{ 
    this->adapterName.copy((wchar_t*)output, this->adapterName.length());
}

GraphicsAllocationInfos Direct3D12GraphicsService::GetBufferAllocationInfos(int sizeInBytes)
{
	GraphicsAllocationInfos result = {};
	result.SizeInBytes = sizeInBytes;
	result.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

	return result;
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

	if (commandQueue != nullptr && commandQueue->CommandAllocators != nullptr)
	{
		for (int i = 0; i < CommandAllocatorsCount; i++)
		{
			commandQueue->CommandAllocators[i]->Release();
		}
	}

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

unsigned long Direct3D12GraphicsService::ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	for (int i = 0; i < fencesToWaitLength; i++)
	{
		auto fenceToWait = fencesToWait[i];
		Direct3D12CommandQueue* commandQueueToWait = (Direct3D12CommandQueue*)fenceToWait.CommandQueuePointer;

		AssertIfFailed(commandQueue->CommandQueueObject->Wait(commandQueueToWait->Fence.Get(), fenceToWait.Value));
	}

	// TODO: We need to free that memory somehow
	ID3D12CommandList** commandListsToExecute = new ID3D12CommandList*[commandListsLength];

	for (int i = 0; i < commandListsLength; i++)
	{
		commandListsToExecute[i] = ((Direct3D12CommandList*)commandLists[i])->CommandListObject.Get();
	}

	commandQueue->CommandQueueObject->ExecuteCommandLists(commandListsLength, commandListsToExecute);

	delete commandListsToExecute;

	// TODO: Switch to an atomic increment here for multi threading
	auto fenceValue = commandQueue->FenceValue;
	commandQueue->CommandQueueObject->Signal(commandQueue->Fence.Get(), fenceValue);
	commandQueue->FenceValue = fenceValue + 1;

	return fenceValue;
}

void Direct3D12GraphicsService::WaitForCommandQueueOnCpu(struct GraphicsFence fenceToWait)
{
	Direct3D12CommandQueue* commandQueueToWait = (Direct3D12CommandQueue*)fenceToWait.CommandQueuePointer;

	if (commandQueueToWait->Fence->GetCompletedValue() < fenceToWait.Value)
	{
		commandQueueToWait->Fence->SetEventOnCompletion(fenceToWait.Value, this->globalFenceEvent);
		WaitForSingleObject(this->globalFenceEvent, INFINITE);
	}
}

void* Direct3D12GraphicsService::CreateCommandList(void* commandQueuePointer)
{
	Direct3D12CommandQueue* commandQueue = (Direct3D12CommandQueue*)commandQueuePointer;

	auto commandAllocator = commandQueue->CommandAllocators[this->currentAllocatorIndex];
	auto listType = commandQueue->Type;

	ComPtr<ID3D12GraphicsCommandList6> commandList;
	AssertIfFailed(this->graphicsDevice->CreateCommandList(0, listType, commandAllocator.Get(), nullptr, IID_PPV_ARGS(commandList.ReleaseAndGetAddressOf())));

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
}

void Direct3D12GraphicsService::CommitCommandList(void* commandListPointer)
{
	this->shaderBound = false;

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	AssertIfFailed(commandList->CommandListObject->Close());
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
		heapDescriptor.Properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
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

void* Direct3D12GraphicsService::CreateShaderResourceHeap(unsigned long length)
{
	D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
	descriptorHeapDesc.NumDescriptors = length;
	descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	ComPtr<ID3D12DescriptorHeap> descriptorHeap;
	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(descriptorHeap.ReleaseAndGetAddressOf())));
	auto handleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

	Direct3D12ShaderResourceHeap* descriptorHeapStruct = new Direct3D12ShaderResourceHeap();
	descriptorHeapStruct->HeapObject = descriptorHeap;
	descriptorHeapStruct->HandleSize = handleSize;

	return descriptorHeapStruct;
}

void Direct3D12GraphicsService::SetShaderResourceHeapLabel(void* shaderResourceHeapPointer, char* label)
{
	Direct3D12ShaderResourceHeap* descriptorHeap = (Direct3D12ShaderResourceHeap*)shaderResourceHeapPointer;
	descriptorHeap->HeapObject->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteShaderResourceHeap(void* shaderResourceHeapPointer)
{
	Direct3D12ShaderResourceHeap* descriptorHeap = (Direct3D12ShaderResourceHeap*)shaderResourceHeapPointer;
	delete descriptorHeap;
}

void Direct3D12GraphicsService::CreateShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index, void* texturePointer)
{
	// TODO: Create also RTV and DSV when appropriate so that we can remove the other global heaps

	// TODO: To remove when SM6.6 is stable
	int textureOffset = 500;

	Direct3D12ShaderResourceHeap* descriptorHeap = (Direct3D12ShaderResourceHeap*)shaderResourceHeapPointer;
	Direct3D12Texture* texture = (Direct3D12Texture*)texturePointer;

	D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
	srvDesc.Format = ConvertSRVTextureFormat(texture->ResourceDesc.Format);
	srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
	srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	srvDesc.Texture2D.MipLevels = 1;//mipLevels;

	auto descriptorHandle = descriptorHeap->HeapObject->GetCPUDescriptorHandleForHeapStart();
	descriptorHandle.ptr += (textureOffset + index) * descriptorHeap->HandleSize;

	this->graphicsDevice->CreateShaderResourceView(texture->TextureObject.Get(), &srvDesc, descriptorHandle);
}

void Direct3D12GraphicsService::DeleteShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index)
{
	// TODO
}

void Direct3D12GraphicsService::CreateShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer)
{
	Direct3D12ShaderResourceHeap* descriptorHeap = (Direct3D12ShaderResourceHeap*)shaderResourceHeapPointer;
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)bufferPointer;

	D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
	srvDesc.ViewDimension = D3D12_SRV_DIMENSION_BUFFER;
	srvDesc.Format = DXGI_FORMAT_R32_TYPELESS;
	srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	srvDesc.Buffer.NumElements = (UINT)graphicsBuffer->ResourceDesc.Width / 4;
    srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_RAW;

	auto descriptorHandle = descriptorHeap->HeapObject->GetCPUDescriptorHandleForHeapStart();
	descriptorHandle.ptr += index * descriptorHeap->HandleSize;

	this->graphicsDevice->CreateShaderResourceView(graphicsBuffer->BufferObject.Get(), &srvDesc, descriptorHandle);
}

void Direct3D12GraphicsService::DeleteShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index)
{
	// TODO
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
	D3D12_RANGE readRange = { 0, 0 };
	graphicsBuffer->BufferObject->Map(0, &readRange, &pointer);
	graphicsBuffer->CpuPointer = pointer;

	return pointer;
}

void Direct3D12GraphicsService::ReleaseGraphicsBufferCpuPointer(void* graphicsBufferPointer)
{
	// Do nothing here because Direct3D12 support permanent map to cpu pointers
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

	if (usage == GraphicsTextureUsage::RenderTarget && textureFormat == GraphicsTextureFormat::Depth32Float)
	{
		initialState = D3D12_RESOURCE_STATE_DEPTH_WRITE;	

		D3D12_CLEAR_VALUE rawClearValue = {};
		rawClearValue.Format = ConvertTextureFormat(textureFormat);
		clearValue = &rawClearValue;
	}

	else if (usage == GraphicsTextureUsage::RenderTarget)
	{
		D3D12_CLEAR_VALUE rawClearValue = {};
		rawClearValue.Format = ConvertTextureFormat(textureFormat);
		clearValue = &rawClearValue;

		initialState = D3D12_RESOURCE_STATE_RENDER_TARGET;
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
	// TODO: Handle in DirectX the create of the RTV and DSV if needed with a slot available list
	if (usage == GraphicsTextureUsage::RenderTarget && textureFormat == GraphicsTextureFormat::Depth32Float)
	{
		D3D12_DEPTH_STENCIL_VIEW_DESC dsvDesc = {};
		dsvDesc.Format = ConvertTextureFormat(textureFormat);
		dsvDesc.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D;

		auto globalDsvDescriptorHeapHandle = this->globalDsvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
		globalDsvDescriptorHeapHandle.ptr += this->currentGlobalDsvDescriptorOffset;

		this->graphicsDevice->CreateDepthStencilView(gpuTexture.Get(), &dsvDesc, globalDsvDescriptorHeapHandle);
		textureStruct->TextureDescriptorOffset = this->currentGlobalDsvDescriptorOffset;
		this->currentGlobalDsvDescriptorOffset += this->globalDsvDescriptorHandleSize;
	}

	else if (usage == GraphicsTextureUsage::RenderTarget)
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

	// else if (usage == GraphicsTextureUsage::ShaderWrite)
	// {
	// 	// UAV View
	// 	D3D12_UNORDERED_ACCESS_VIEW_DESC  uavDesc = {};
	// 	uavDesc.Format = ConvertTextureFormat(textureFormat);
	// 	uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
	// 	uavDesc.Texture2D.MipSlice = 0;

	// 	globalDescriptorHeapHandle = this->globalDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
	// 	globalDescriptorHeapHandle.ptr += this->currentGlobalDescriptorOffset;

	// 	this->graphicsDevice->CreateUnorderedAccessView(gpuTexture.Get(), nullptr, &uavDesc, globalDescriptorHeapHandle);
	// 	textureStruct->UavTextureDescriptorOffset = this->currentGlobalDescriptorOffset;
	// 	this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;
	// }

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
	swapChainDesc.Flags = DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT;

	DXGI_SWAP_CHAIN_FULLSCREEN_DESC swapChainFullScreenDesc = {};
	swapChainFullScreenDesc.Windowed = true;
	
	ComPtr<IDXGISwapChain3> swapChain;
	AssertIfFailed(dxgiFactory->CreateSwapChainForHwnd(commandQueue->CommandQueueObject.Get(), (HWND)windowPointer, &swapChainDesc, &swapChainFullScreenDesc, nullptr, (IDXGISwapChain1**)swapChain.ReleaseAndGetAddressOf()));
	swapChain->SetMaximumFrameLatency(1);

	Direct3D12SwapChain* swapChainStructure = new Direct3D12SwapChain();
	swapChainStructure->SwapChainObject = swapChain;
	swapChainStructure->CommandQueue = commandQueue;
	swapChainStructure->WaitHandle = swapChain->GetFrameLatencyWaitableObject();

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

void Direct3D12GraphicsService::DeleteSwapChain(void* swapChainPointer)
{
	Direct3D12SwapChain* swapChain = (Direct3D12SwapChain*)swapChainPointer;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		DeleteTexture(swapChain->BackBufferTextures[i]);
	}

	delete swapChain;
}

void Direct3D12GraphicsService::ResizeSwapChain(void* swapChainPointer, int width, int height)
{
	Direct3D12SwapChain* swapChain = (Direct3D12SwapChain*)swapChainPointer;

	GraphicsFence fenceToWait = {};
	fenceToWait.CommandQueuePointer = swapChain->CommandQueue;
	fenceToWait.Value = swapChain->CommandQueue->FenceValue - 1;

	this->WaitForCommandQueueOnCpu(fenceToWait);
	
	D3D12_RESOURCE_DESC backBufferDesc;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		backBufferDesc = swapChain->BackBufferTextures[i]->ResourceDesc;
		delete swapChain->BackBufferTextures[i];
	}

	backBufferDesc.Width = width;
	backBufferDesc.Height = height;

	AssertIfFailed(swapChain->SwapChainObject->ResizeBuffers(RenderBuffersCount, width, height, DXGI_FORMAT_UNKNOWN, DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT));

	D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
	rtvDesc.Format = backBufferDesc.Format;
	rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		DeleteTexture(swapChain->BackBufferTextures[i]);

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
	this->currentAllocatorIndex = (this->currentAllocatorIndex + 1) % FramesCount;

	return fenceValue;
}

void Direct3D12GraphicsService::WaitForSwapChainOnCpu(void* swapChainPointer)
{
	Direct3D12SwapChain* swapChain = (Direct3D12SwapChain*)swapChainPointer;

	if (WaitForSingleObjectEx(swapChain->WaitHandle, 1000, true) == WAIT_TIMEOUT)
	{
		assert("Wait for SwapChain timeout");
	}
}

void* Direct3D12GraphicsService::CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length)
{
	auto type = D3D12_QUERY_HEAP_TYPE_TIMESTAMP;

	if (queryBufferType == GraphicsQueryBufferType::CopyTimestamp)
	{
		type = D3D12_QUERY_HEAP_TYPE_COPY_QUEUE_TIMESTAMP;
	}

	else if (queryBufferType == GraphicsQueryBufferType::GraphicsPipelineStats)
	{
		type = D3D12_QUERY_HEAP_TYPE_PIPELINE_STATISTICS1;
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

void Direct3D12GraphicsService::ResetQueryBuffer(void* queryBufferPointer)
{
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

	// Skip SPIR-V offset
	currentDataPtr += sizeof(int);

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

		if (entryPointName == "AmplificationMain")
		{
			shader->AmplificationShaderMethod = shaderBlob;
		}

		else if (entryPointName == "MeshMain")
		{
			shader->MeshShaderMethod = shaderBlob;
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
		D3D12_RT_FORMAT_ARRAY renderTargets = {};
		
		if (!renderPassDescriptor.RenderTarget1TexturePointer.HasValue && !renderPassDescriptor.DepthTexturePointer.HasValue)
		{
			renderTargets.NumRenderTargets = 1;
			renderTargets.RTFormats[0] = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB; // TODO: Fill Correct Back Buffer Format
		}

		else
		{
			renderTargets.NumRenderTargets = 1;
			renderTargets.RTFormats[0] = ConvertTextureFormat(renderPassDescriptor.RenderTarget1TextureFormat.Value);
		}

		DXGI_FORMAT depthFormat = DXGI_FORMAT_UNKNOWN;

		if (renderPassDescriptor.DepthTexturePointer.HasValue)
		{
			// TODO: Change that
			depthFormat = DXGI_FORMAT_D32_FLOAT;
		}

		DXGI_SAMPLE_DESC sampleDesc = {};
		sampleDesc.Count = 1;
		sampleDesc.Quality = 0;

		D3D12_RASTERIZER_DESC rasterizerState = {};
		rasterizerState.FillMode = D3D12_FILL_MODE_SOLID;
		rasterizerState.CullMode = D3D12_CULL_MODE_BACK;
		rasterizerState.FrontCounterClockwise = false;
		rasterizerState.DepthBias = D3D12_DEFAULT_DEPTH_BIAS;
		rasterizerState.DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP;
		rasterizerState.SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
		rasterizerState.DepthClipEnable = true;
		rasterizerState.MultisampleEnable = false;
		rasterizerState.AntialiasedLineEnable = false;
		rasterizerState.ForcedSampleCount = 0;
		rasterizerState.ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;

		D3D12_DEPTH_STENCIL_DESC depthStencilState = {};

		if (renderPassDescriptor.DepthBufferOperation != GraphicsDepthBufferOperation::DepthNone)
		{
			depthStencilState.DepthEnable = true;
			depthStencilState.StencilEnable = false;

			if (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::ClearWrite ||
				renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::Write)
			{
				depthStencilState.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;
			}

			if (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::CompareEqual)
			{
				depthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_EQUAL;
			}

			else if (renderPassDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::CompareGreater)
			{
				depthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_GREATER;
			}

			else
			{
				depthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_GREATER_EQUAL;
			}
		}

		D3D12_BLEND_DESC blendState = {};
		blendState.AlphaToCoverageEnable = false;
		blendState.IndependentBlendEnable = false;

		if (renderPassDescriptor.RenderTarget1BlendOperation.HasValue)
		{
			auto blendOperation = renderPassDescriptor.RenderTarget1BlendOperation.Value;
			blendState.RenderTarget[0] = InitBlendState(blendOperation);
		}

		else
		{
			blendState.RenderTarget[0] = InitBlendState(GraphicsBlendOperation::None);
		}

		D3D12_PRIMITIVE_TOPOLOGY_TYPE topologyType = renderPassDescriptor.PrimitiveType == GraphicsPrimitiveType::Triangle ? D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE : D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE;

		D3D12_PIPELINE_STATE_STREAM_DESC psoStream = {};
		GraphicsPso psoDesc = {};
		
		psoDesc.RootSignature = shader->RootSignature.Get();

		if (shader->AmplificationShaderMethod != nullptr)
		{
			psoDesc.AS = { shader->AmplificationShaderMethod->GetBufferPointer(), shader->AmplificationShaderMethod->GetBufferSize() };
		}

		psoDesc.MS = { shader->MeshShaderMethod->GetBufferPointer(), shader->MeshShaderMethod->GetBufferSize() };
		psoDesc.PS = { shader->PixelShaderMethod->GetBufferPointer(), shader->PixelShaderMethod->GetBufferSize() };
		psoDesc.RenderTargets = renderTargets;
		psoDesc.SampleDesc = sampleDesc;
		psoDesc.RasterizerState = rasterizerState;
		psoDesc.DepthStencilFormat = depthFormat;
		psoDesc.DepthStencilState = depthStencilState;
		psoDesc.BlendState = blendState;
		psoDesc.PrimitiveTopologyType = topologyType;

		psoStream.SizeInBytes = sizeof(GraphicsPso);
		psoStream.pPipelineStateSubobjectStream = &psoDesc;

		AssertIfFailed(this->graphicsDevice->CreatePipelineState(&psoStream, IID_PPV_ARGS(pipelineState.ReleaseAndGetAddressOf())));
	}

	else
	{
		// TODO: Switch to CreatePipelineState
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

void Direct3D12GraphicsService::CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int sizeInBytes)
{ 
	// TODO: Transition buffer to copy dest first?
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12GraphicsBuffer* destinationGraphicsBuffer = (Direct3D12GraphicsBuffer*)destinationGraphicsBufferPointer;
	Direct3D12GraphicsBuffer* sourceGraphicsBuffer = (Direct3D12GraphicsBuffer*)sourceGraphicsBufferPointer;

	if (destinationGraphicsBuffer->Type == GraphicsServiceHeapType::ReadBack && destinationGraphicsBuffer->CpuPointer != nullptr)
	{
		D3D12_RANGE range = { 0, 0 };
		destinationGraphicsBuffer->BufferObject->Unmap(0, &range);
		destinationGraphicsBuffer->CpuPointer = nullptr;
	}

	// if (destinationGraphicsBuffer->Type == GraphicsServiceHeapType::Gpu)
	// {
	// 	TransitionBufferToState(commandList, destinationGraphicsBuffer, D3D12_RESOURCE_STATE_COPY_DEST);
	// }

	commandList->CommandListObject->CopyBufferRegion(destinationGraphicsBuffer->BufferObject.Get(), 0, sourceGraphicsBuffer->BufferObject.Get(), 0, sizeInBytes);

	// if (destinationGraphicsBuffer->Type == GraphicsServiceHeapType::Gpu)
	// {
	// 	TransitionBufferToState(commandList, destinationGraphicsBuffer, D3D12_RESOURCE_STATE_COMMON);
	// }

	// TODO: Group transitions together
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

	// TODO: When to Transition texture to generic read?
}

void Direct3D12GraphicsService::CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12Texture* destinationTexture = (Direct3D12Texture*)destinationTexturePointer;
	Direct3D12Texture* sourceTexture = (Direct3D12Texture*)sourceTexturePointer;

	commandList->CommandListObject->CopyResource(destinationTexture->TextureObject.Get(), sourceTexture->TextureObject.Get());
}

void Direct3D12GraphicsService::TransitionGraphicsBufferToState(void* commandListPointer, void* graphicsBufferPointer, enum GraphicsResourceState resourceState)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12GraphicsBuffer* graphicsBuffer = (Direct3D12GraphicsBuffer*)graphicsBufferPointer;

	D3D12_RESOURCE_STATES destinationState = D3D12_RESOURCE_STATE_COMMON;

	if (resourceState == GraphicsResourceState::StateDestinationCopy)
	{
		destinationState = D3D12_RESOURCE_STATE_COPY_DEST;
	}

	else if (resourceState == GraphicsResourceState::StateShaderRead)
	{
		destinationState = D3D12_RESOURCE_STATE_GENERIC_READ;
	}

	TransitionBufferToState(commandList, graphicsBuffer, destinationState);
}

void Direct3D12GraphicsService::DispatchThreads(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	commandList->CommandListObject->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
}

void Direct3D12GraphicsService::BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{
	auto renderDescriptor = renderPassDescriptor;

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	commandList->RenderPassDescriptor = renderDescriptor;

	if (renderDescriptor.RenderTarget1TexturePointer.HasValue)
	{
		Direct3D12Texture* texture = (Direct3D12Texture*)renderDescriptor.RenderTarget1TexturePointer.Value;

		// TODO: Refactor that
		D3D12_CPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
		descriptorHeapHandle.ptr = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + texture->TextureDescriptorOffset;

		TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_RENDER_TARGET);

		D3D12_RENDER_PASS_RENDER_TARGET_DESC renderTargetDesc = {};
		renderTargetDesc.cpuDescriptor = descriptorHeapHandle;
		renderTargetDesc.BeginningAccess.Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE;

		if (renderDescriptor.RenderTarget1ClearColor.HasValue)
		{
			float clearColor[4] = { renderDescriptor.RenderTarget1ClearColor.Value.X, renderDescriptor.RenderTarget1ClearColor.Value.Y, renderDescriptor.RenderTarget1ClearColor.Value.Z, renderDescriptor.RenderTarget1ClearColor.Value.W };

			renderTargetDesc.BeginningAccess.Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR;
			renderTargetDesc.BeginningAccess.Clear.ClearValue.Format = texture->ResourceDesc.Format;
			// renderTargetDesc.BeginningAccess.Clear.ClearValue.Color = clearColor;
		}

		renderTargetDesc.EndingAccess.Type = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE;

		D3D12_RENDER_PASS_DEPTH_STENCIL_DESC* depthStencilDesc = nullptr;
		D3D12_RENDER_PASS_DEPTH_STENCIL_DESC tmpDepthDesc = {};

		if (renderDescriptor.DepthTexturePointer.HasValue)
		{
			Direct3D12Texture* depthTexture = (Direct3D12Texture*)renderDescriptor.DepthTexturePointer.Value;

			D3D12_CPU_DESCRIPTOR_HANDLE depthDescriptorHeapHandle = {};
			depthDescriptorHeapHandle.ptr = this->globalDsvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + depthTexture->TextureDescriptorOffset;

			tmpDepthDesc.cpuDescriptor = depthDescriptorHeapHandle;
			tmpDepthDesc.DepthBeginningAccess.Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE;

			if (renderDescriptor.DepthBufferOperation == GraphicsDepthBufferOperation::ClearWrite)
			{
				D3D12_DEPTH_STENCIL_VALUE clearValue = {};
				clearValue.Depth = 0.0f;

				tmpDepthDesc.DepthBeginningAccess.Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR;
				tmpDepthDesc.DepthBeginningAccess.Clear.ClearValue.Format = texture->ResourceDesc.Format;
				tmpDepthDesc.DepthBeginningAccess.Clear.ClearValue.DepthStencil = clearValue;
			}

			tmpDepthDesc.DepthEndingAccess.Type = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE;

			depthStencilDesc = &tmpDepthDesc;
		}

		commandList->CommandListObject->BeginRenderPass(1, &renderTargetDesc, depthStencilDesc, D3D12_RENDER_PASS_FLAG_NONE);

		D3D12_VIEWPORT viewport = {};
		viewport.Width = (float)texture->ResourceDesc.Width;
		viewport.Height = (float)texture->ResourceDesc.Height;
		viewport.MinDepth = 0.0f;
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
	commandList->CommandListObject->EndRenderPass();
	
	auto renderDescriptor = commandList->RenderPassDescriptor;

	if (renderDescriptor.RenderTarget1TexturePointer.HasValue)
	{
		Direct3D12Texture* texture = (Direct3D12Texture*)renderDescriptor.RenderTarget1TexturePointer.Value;

		if (!texture->IsPresentTexture)
		{
			// TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_GENERIC_READ);
			// TransitionTextureToState(commandList, texture, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
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

// TODO: To remove when sm6.6 is stable
Direct3D12ShaderResourceHeap* currentDescriptorHeap;

void Direct3D12GraphicsService::SetShaderResourceHeap(void* commandListPointer, void* shaderResourceHeapPointer)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12ShaderResourceHeap* descriptorHeap = (Direct3D12ShaderResourceHeap*)shaderResourceHeapPointer;

	ID3D12DescriptorHeap* descriptorHeaps[] = { descriptorHeap->HeapObject.Get() };
	commandList->CommandListObject->SetDescriptorHeaps(1, descriptorHeaps);

	// TODO: To remove when sm6.6 is stable
	currentDescriptorHeap = descriptorHeap;
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

	// TODO: To remove when sm6.6 is stable
	if (currentDescriptorHeap != nullptr)
	{
		commandList->CommandListObject->SetGraphicsRootDescriptorTable(1, currentDescriptorHeap->HeapObject->GetGPUDescriptorHandleForHeapStart());

		D3D12_GPU_DESCRIPTOR_HANDLE texturesHandle = currentDescriptorHeap->HeapObject->GetGPUDescriptorHandleForHeapStart();
		texturesHandle.ptr += (500 * currentDescriptorHeap->HandleSize);

		commandList->CommandListObject->SetGraphicsRootDescriptorTable(2, texturesHandle);
		currentDescriptorHeap = nullptr;
	}

	this->shaderBound = true;
}

void Direct3D12GraphicsService::SetShaderParameterValues(void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;

	if (commandList->Type == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		commandList->CommandListObject->SetGraphicsRoot32BitConstants(slot, valuesLength, values, 0);
	}

	else if (commandList->Type == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandList->CommandListObject->SetComputeRoot32BitConstants(slot, valuesLength, values, 0);
	}
}

void Direct3D12GraphicsService::DispatchMesh(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ)
{
	if (!this->shaderBound)
	{
		return;
	}

	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	commandList->CommandListObject->DispatchMesh(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
}

void Direct3D12GraphicsService::BeginQuery(void* commandListPointer, void* queryBufferPointer, int index)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12QueryBuffer* queryBuffer = (Direct3D12QueryBuffer*)queryBufferPointer;

	D3D12_QUERY_TYPE queryType = D3D12_QUERY_TYPE_PIPELINE_STATISTICS1;

	if (queryBuffer->Type == D3D12_QUERY_HEAP_TYPE_TIMESTAMP)
	{
		return;
	}

	commandList->CommandListObject->BeginQuery(queryBuffer->QueryBufferObject.Get(), queryType, index);
}

void Direct3D12GraphicsService::EndQuery(void* commandListPointer, void* queryBufferPointer, int index)
{
	Direct3D12CommandList* commandList = (Direct3D12CommandList*)commandListPointer;
	Direct3D12QueryBuffer* queryBuffer = (Direct3D12QueryBuffer*)queryBufferPointer;

	D3D12_QUERY_TYPE queryType = D3D12_QUERY_TYPE_TIMESTAMP;

	if (queryBuffer->Type == D3D12_QUERY_HEAP_TYPE_PIPELINE_STATISTICS1)
	{
		queryType = D3D12_QUERY_TYPE_PIPELINE_STATISTICS1;
	}

	commandList->CommandListObject->EndQuery(queryBuffer->QueryBufferObject.Get(), queryType, index);
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

	D3D12_QUERY_TYPE queryType = D3D12_QUERY_TYPE_TIMESTAMP;

	if (queryBuffer->Type == D3D12_QUERY_HEAP_TYPE_PIPELINE_STATISTICS1)
	{
		queryType = D3D12_QUERY_TYPE_PIPELINE_STATISTICS1;
	}

	commandList->CommandListObject->ResolveQueryData(queryBuffer->QueryBufferObject.Get(), queryType, startIndex, endIndex, destinationBuffer->BufferObject.Get(), 0);
}

static void DebugReportCallback(D3D12_MESSAGE_CATEGORY Category, D3D12_MESSAGE_SEVERITY Severity, D3D12_MESSAGE_ID ID, LPCSTR pDescription, void* pContext)
{

}

void Direct3D12GraphicsService::EnableDebugLayer()
{
	this->graphicsDevice->QueryInterface(IID_PPV_ARGS(this->debugInfoQueue.GetAddressOf()));

	if (debugInfoQueue)
	{
		DWORD callBackCookie = 0;
		AssertIfFailed(debugInfoQueue->RegisterMessageCallback(DebugReportCallback, D3D12_MESSAGE_CALLBACK_IGNORE_FILTERS, nullptr, &callBackCookie));
	}

	AssertIfFailed(DXGIGetDebugInterface1(0, IID_PPV_ARGS(this->dxgiDebug.ReleaseAndGetAddressOf())));
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

			if (tempDevice != nullptr)
			{
				D3D12_FEATURE_DATA_D3D12_OPTIONS deviceOptions = {};
				AssertIfFailed(tempDevice->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &deviceOptions, sizeof(deviceOptions)));

				D3D12_FEATURE_DATA_D3D12_OPTIONS7 deviceOptions7 = {};
				AssertIfFailed(tempDevice->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS7, &deviceOptions7, sizeof(deviceOptions7)));

				D3D12_FEATURE_DATA_SHADER_MODEL shaderModel = {};
				shaderModel.HighestShaderModel = D3D_SHADER_MODEL_6_6;

				// if (dxgiAdapterDesc1.VendorId != 32902)
				// {
				// 	continue;
				// }

				AssertIfFailed(tempDevice->CheckFeatureSupport(D3D12_FEATURE_SHADER_MODEL, &shaderModel, sizeof(shaderModel)));

				if (deviceOptions.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER_2 && 
					deviceOptions.ResourceBindingTier == D3D12_RESOURCE_BINDING_TIER_3 && 
					deviceOptions7.MeshShaderTier == D3D12_MESH_SHADER_TIER_1 &&
					shaderModel.HighestShaderModel == D3D_SHADER_MODEL_6_6 &&
					dxgiAdapterDesc1.DedicatedVideoMemory > maxDedicatedVideoMemory)
				{
					this->adapterName = wstring(dxgiAdapterDesc1.Description) + wstring(L" (DirectX 12.1.") + to_wstring(D3D12_SDK_VERSION) + L")";
					maxDedicatedVideoMemory = dxgiAdapterDesc1.DedicatedVideoMemory;
					dxgiAdapter1.As(&dxgiAdapter4);
				}
			}
		}
	}

	return dxgiAdapter4;
}

bool Direct3D12GraphicsService::CreateDevice(const ComPtr<IDXGIFactory4> dxgiFactory, const ComPtr<IDXGIAdapter4> graphicsAdapter)
{
	// Created Direct3D Device
	HRESULT result = D3D12CreateDevice(graphicsAdapter.Get(), D3D_FEATURE_LEVEL_12_1, IID_PPV_ARGS(this->graphicsDevice.ReleaseAndGetAddressOf()));

	if (FAILED(result))
	{
		// If hardware initialization fail, fall back to the WARP driver
		OutputDebugStringA("Direct3D hardware device initialization failed. Falling back to WARP driver.\n");

		ComPtr<IDXGIAdapter> warpAdapter;
		dxgiFactory->EnumWarpAdapter(IID_PPV_ARGS(warpAdapter.ReleaseAndGetAddressOf()));

		AssertIfFailed(D3D12CreateDevice(warpAdapter.Get(), D3D_FEATURE_LEVEL_12_0, IID_PPV_ARGS(this->graphicsDevice.ReleaseAndGetAddressOf())));
	}

	this->globalFenceEvent = CreateEventA(nullptr, false, false, nullptr);

	// TODO: Remove that, that method will work only on DEV mode in Windows 10
	// It will prevent the driver to use boost mode so that's really bad
	// but when working with a mobile GPU it is the only way to have stable
	// GPU measurements
	AssertIfFailed(this->graphicsDevice->SetStablePowerState(true));

	return true;
}

bool Direct3D12GraphicsService::CreateHeaps()
{
	// Create global RTV Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvDescriptorHeapDesc = {};
	rtvDescriptorHeapDesc.NumDescriptors = 100000; //TODO: Change that
	rtvDescriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvDescriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&rtvDescriptorHeapDesc, IID_PPV_ARGS(this->globalRtvDescriptorHeap.ReleaseAndGetAddressOf())));
	this->globalRtvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
	this->currentGlobalRtvDescriptorOffset = 0;

	// Create global DSV Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC dsvDescriptorHeapDesc = {};
	rtvDescriptorHeapDesc.NumDescriptors = 100000; //TODO: Change that
	rtvDescriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV;
	rtvDescriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&rtvDescriptorHeapDesc, IID_PPV_ARGS(this->globalDsvDescriptorHeap.ReleaseAndGetAddressOf())));
	this->globalDsvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);
	this->currentGlobalDsvDescriptorOffset = 0;

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
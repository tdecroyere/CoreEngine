#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"
#include "Direct3D12GraphicsServiceUtils.h"

using namespace std;
using namespace Microsoft::WRL;

#define GetAlignedValue(value, alignement) (value + (alignement - (value % alignement)) % alignement)

bool enableTiming = true;

Direct3D12GraphicsService::Direct3D12GraphicsService(HWND window, int width, int height, GameState* gameState)
{
	this->gameState = gameState;
	this->isWaitingForGlobalFence = false;
    UINT createFactoryFlags = 0;

#ifdef DEBUG
	EnableDebugLayer();
    createFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
#endif

	this->window = window;
	AssertIfFailed(CreateDXGIFactory2(createFactoryFlags, IID_PPV_ARGS(this->dxgiFactory.ReleaseAndGetAddressOf())));

	auto graphicsAdapter = FindGraphicsAdapter(dxgiFactory);
	AssertIfFailed(CreateDevice(dxgiFactory, graphicsAdapter));
	AssertIfFailed(CreateOrResizeSwapChain(width, height));
	AssertIfFailed(CreateHeaps());
	InitGpuProfiling();
}

Direct3D12GraphicsService::~Direct3D12GraphicsService()
{
	// Ensure that the GPU is no longer referencing resources that are about to be
	// cleaned up by the destructor.

	// TODO: Wait until all command queues are finished on the GPU
	this->WaitForAvailableScreenBuffer();

	// Fullscreen state should always be false before exiting the app.
	this->swapChain->SetFullscreenState(false, nullptr);

	CloseHandle(this->globalFenceEvent);
}

struct Vector2 Direct3D12GraphicsService::GetRenderSize()
{
    return this->currentRenderSize;
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

int Direct3D12GraphicsService::CreateGraphicsHeap(unsigned int graphicsHeapId, enum GraphicsServiceHeapType type, unsigned long sizeInBytes)
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

	this->graphicsHeaps[graphicsHeapId] = graphicsHeap;
	this->graphicsHeapTypes[graphicsHeapId] = type;

	return 1;
}

void Direct3D12GraphicsService::SetGraphicsHeapLabel(unsigned int graphicsHeapId, char* label)
{
	if (!this->graphicsHeaps.count(graphicsHeapId))
	{
		return;
	}

	this->graphicsHeaps[graphicsHeapId]->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteGraphicsHeap(unsigned int graphicsHeapId)
{
	this->graphicsHeaps.erase(graphicsHeapId);
}

int Direct3D12GraphicsService::CreateGraphicsBuffer(unsigned int graphicsBufferId, unsigned int graphicsHeapId, unsigned long heapOffset, int isAliasable, int sizeInBytes)
{ 
	if (!this->graphicsHeaps.count(graphicsHeapId))
	{
		return false;
	}

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

	if (this->graphicsHeapTypes[graphicsHeapId] == GraphicsServiceHeapType::Upload)
	{
		resourceState = D3D12_RESOURCE_STATE_GENERIC_READ;
	}

	ComPtr<ID3D12Resource> graphicsBuffer;
	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(this->graphicsHeaps[graphicsHeapId].Get(), heapOffset, &resourceDesc, resourceState, nullptr, IID_PPV_ARGS(graphicsBuffer.ReleaseAndGetAddressOf())));
	
	this->graphicsBuffers[graphicsBufferId] = graphicsBuffer;

	// TODO: Resource state tracking should be moved to the engine
	this->bufferResourceStates[graphicsBufferId] = resourceState;
	
    return 1;
}

void Direct3D12GraphicsService::SetGraphicsBufferLabel(unsigned int graphicsBufferId, char* label)
{
	if (!this->graphicsBuffers.count(graphicsBufferId))
	{
		return;
	}

	this->graphicsBuffers[graphicsBufferId]->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteGraphicsBuffer(unsigned int graphicsBufferId)
{
	// TODO: Wait for next frame for releasing resources

	//this->gpuBuffers.erase(graphicsBufferId);
}

void* Direct3D12GraphicsService::GetGraphicsBufferCpuPointer(unsigned int graphicsBufferId)
{
	if (!this->graphicsBuffers.count(graphicsBufferId))
	{
		return nullptr;
	}

	if (this->graphicsBufferPointers.count(graphicsBufferId))
	{
		return this->graphicsBufferPointers[graphicsBufferId];
	}

	auto graphicsBuffer = this->graphicsBuffers[graphicsBufferId];

	void* pointer = nullptr;
	D3D12_RANGE range = { 0, 0 };
	graphicsBuffer->Map(0, &range, &pointer);

	this->graphicsBufferPointers[graphicsBufferId] = pointer;

	return pointer;
}

// TODO: Te remove
int Direct3D12GraphicsService::CreateGraphicsBufferOld(unsigned int graphicsBufferId, int sizeInBytes, int isWriteOnly, char* label)
{ 
	// For the moment, all data is aligned in a 64 KB alignement
	uint64_t alignement = 64 * 1024;

	D3D12_RESOURCE_DESC resourceDesc = {};
	resourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
	resourceDesc.Alignment = 0;
	resourceDesc.Width = isWriteOnly ? GetAlignedValue(sizeInBytes, alignement) : sizeInBytes;
	resourceDesc.Height = 1;
	resourceDesc.DepthOrArraySize = 1;
	resourceDesc.MipLevels = 1;
	resourceDesc.Format = DXGI_FORMAT_UNKNOWN;
	resourceDesc.SampleDesc.Count = 1;
	resourceDesc.SampleDesc.Quality = 0;
	resourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
	resourceDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;

	// Create a Direct3D12 buffer on the GPU
	ComPtr<ID3D12Resource> gpuBuffer;

	// if (isWriteOnly)
	// {
	// 	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(this->globalHeap.Get(), this->currentGlobalHeapOffset, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(gpuBuffer.ReleaseAndGetAddressOf())));
	// 	gpuBuffer->SetName((wstring(L"GpuBuffer_") + wstring(label, label + strlen(label))).c_str());
	// 	this->currentGlobalHeapOffset += GetAlignedValue(length, alignement);
	// }

	// else
	// {
		D3D12_HEAP_PROPERTIES defaultHeapProperties = {};
		defaultHeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
		defaultHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		defaultHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
		defaultHeapProperties.CreationNodeMask = 0;
		defaultHeapProperties.VisibleNodeMask = 0;

		AssertIfFailed(this->graphicsDevice->CreateCommittedResource(&defaultHeapProperties, D3D12_HEAP_FLAG_NONE, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(gpuBuffer.ReleaseAndGetAddressOf())));
	//}

	this->graphicsBuffers[graphicsBufferId] = gpuBuffer;
	this->bufferResourceStates[graphicsBufferId] = D3D12_RESOURCE_STATE_COPY_DEST;


	resourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

	// Create a Direct3D12 buffer on the CPU
	defaultHeapProperties = {};
	defaultHeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
	defaultHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
	defaultHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
	defaultHeapProperties.CreationNodeMask = 1;
	defaultHeapProperties.VisibleNodeMask = 1;

	ComPtr<ID3D12Resource> cpuBuffer;
	AssertIfFailed(this->graphicsDevice->CreateCommittedResource(&defaultHeapProperties, D3D12_HEAP_FLAG_NONE, &resourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, nullptr, IID_PPV_ARGS(cpuBuffer.ReleaseAndGetAddressOf())));
	cpuBuffer->SetName((wstring(L"CpuBuffer_") + wstring(label, label + strlen(label))).c_str());
	this->currentUploadHeapOffset += GetAlignedValue(sizeInBytes, alignement);
	this->cpuBuffers[graphicsBufferId] = cpuBuffer;

	if (!isWriteOnly)
	{
		defaultHeapProperties = {};
		defaultHeapProperties.Type = D3D12_HEAP_TYPE_READBACK;
		defaultHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		defaultHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
		defaultHeapProperties.CreationNodeMask = 1;
		defaultHeapProperties.VisibleNodeMask = 1;

		ComPtr<ID3D12Resource> readBackBuffer;
		AssertIfFailed(this->graphicsDevice->CreateCommittedResource(&defaultHeapProperties, D3D12_HEAP_FLAG_NONE, &resourceDesc, D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(readBackBuffer.ReleaseAndGetAddressOf())));
		readBackBuffer->SetName((wstring(L"ReadBackBuffer_") + wstring(label, label + strlen(label))).c_str());
		this->currentReadBackHeapOffset += GetAlignedValue(sizeInBytes, alignement);
		this->readBackBuffers[graphicsBufferId] = readBackBuffer;
	}

	if (!isWriteOnly)
	{
		// UAV View
		D3D12_UNORDERED_ACCESS_VIEW_DESC  uavDesc = {};
		uavDesc.Format = DXGI_FORMAT_UNKNOWN;
		uavDesc.ViewDimension = D3D12_UAV_DIMENSION_BUFFER;
		uavDesc.Buffer.NumElements = sizeInBytes / 24;
		uavDesc.Buffer.StructureByteStride = 24; // TODO: Change that

		auto globalDescriptorHeapHandle = this->globalDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
		globalDescriptorHeapHandle.ptr += this->currentGlobalDescriptorOffset;

		this->graphicsDevice->CreateUnorderedAccessView(gpuBuffer.Get(), nullptr, &uavDesc, globalDescriptorHeapHandle);
		this->uavBufferDescriptorOffets[graphicsBufferId] = this->currentGlobalDescriptorOffset;
		this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;
	}

	// Create Descriptor heap
	// D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
	// descriptorHeapDesc.NumDescriptors = 1;
	// descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	// descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	// ComPtr<ID3D12DescriptorHeap> descriptorHeap;
	// AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(descriptorHeap.ReleaseAndGetAddressOf())));

	// D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
	// srvDesc.Format = DXGI_FORMAT_R32_TYPELESS;
	// srvDesc.ViewDimension = D3D12_SRV_DIMENSION_BUFFER;
	// srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	// srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_RAW;
   	// srvDesc.Buffer.StructureByteStride = 0;
	// srvDesc.Buffer.NumElements = (UINT)length / 4;

	// this->graphicsDevice->CreateShaderResourceView(gpuBuffer.Get(), &srvDesc, descriptorHeap->GetCPUDescriptorHandleForHeapStart());
	// this->bufferDescriptorHeaps[graphicsBufferId] = descriptorHeap;

    return 1;
}

int Direct3D12GraphicsService::CreateTexture(unsigned int textureId, unsigned int graphicsHeapId, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
	if (!this->graphicsHeaps.count(graphicsHeapId))
	{
		return false;
	}

	// TODO: Support mip levels
	auto textureDesc = CreateTextureResourceDescription(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);

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
	AssertIfFailed(this->graphicsDevice->CreatePlacedResource(this->graphicsHeaps[graphicsHeapId].Get(), heapOffset, &textureDesc, initialState, clearValue, IID_PPV_ARGS(gpuTexture.ReleaseAndGetAddressOf())));
	this->gpuTextures[textureId] = gpuTexture;
	this->textureResourceStates[textureId] = initialState;

	UINT64 uploadBufferSize;
	D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint;

	this->graphicsDevice->GetCopyableFootprints(&textureDesc, 0, 1, 0, &footPrint, nullptr, nullptr, &uploadBufferSize);
	this->textureFootPrints[textureId] = footPrint;

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
		this->srvtextureDescriptorOffets[textureId] = this->currentGlobalDescriptorOffset;
		this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;

		if (usage == GraphicsTextureUsage::RenderTarget)
		{
			D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
			rtvDesc.Format = ConvertTextureFormat(textureFormat);
			rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

			auto globalRtvDescriptorHeapHandle = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
			globalRtvDescriptorHeapHandle.ptr += this->currentGlobalRtvDescriptorOffset;

			this->graphicsDevice->CreateRenderTargetView(gpuTexture.Get(), &rtvDesc, globalRtvDescriptorHeapHandle);
			this->textureDescriptorOffets[textureId] = this->currentGlobalRtvDescriptorOffset;
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
			this->uavTextureDescriptorOffets[textureId] = this->currentGlobalDescriptorOffset;
			this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;
		}
	}

	return 1;
}

void Direct3D12GraphicsService::SetTextureLabel(unsigned int textureId, char* label)
{
	if (!this->gpuTextures.count(textureId))
	{
		return;
	}

	this->gpuTextures[textureId]->SetName(wstring(label, label + strlen(label)).c_str());
}

/*
int Direct3D12GraphicsService::CreateTextureOld(unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, char* label)
{ 
	auto textureName = wstring(label, label + strlen(label));

	// TODO: Support mip levels
	// TODO: Switch to placed resources
	auto textureDesc = CreateTextureResourceDescription(textureFormat, width, height, faceCount, mipLevels, multisampleCount);

	D3D12_CLEAR_VALUE* clearValue = nullptr;

	D3D12_RESOURCE_STATES initialState = D3D12_RESOURCE_STATE_COPY_DEST;

	if (isRenderTarget) 
	{
		if (textureFormat == GraphicsTextureFormat::Depth32Float)
		{
			textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
		}

		else
		{
			// TODO: Change this, mixing UAV and RenderTarget is not recommanded
			textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;

			// TODO: To remove?
			D3D12_CLEAR_VALUE rawClearValue = {};
			rawClearValue.Format = ConvertTextureFormat(textureFormat);
			clearValue = &rawClearValue;
			initialState = D3D12_RESOURCE_STATE_GENERIC_READ;
		}
	} 
	
	else 
	{
		textureDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
	}

	D3D12_HEAP_PROPERTIES defaultHeapProperties = {};
	defaultHeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
	defaultHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
	defaultHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
	defaultHeapProperties.CreationNodeMask = 1;
	defaultHeapProperties.VisibleNodeMask = 1;

	ComPtr<ID3D12Resource> gpuTexture;
	AssertIfFailed(this->graphicsDevice->CreateCommittedResource(&defaultHeapProperties, D3D12_HEAP_FLAG_NONE, &textureDesc, initialState, clearValue, IID_PPV_ARGS(gpuTexture.ReleaseAndGetAddressOf())));
	gpuTexture->SetName(textureName.c_str());
	this->gpuTextures[textureId] = gpuTexture;
	this->textureResourceStates[textureId] = initialState;

	if (!isRenderTarget)
	{
		UINT64 uploadBufferSize;
		D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint;

		this->graphicsDevice->GetCopyableFootprints(&textureDesc, 0, 1, 0, &footPrint, nullptr, nullptr, &uploadBufferSize);
		this->textureFootPrints[textureId] = footPrint;

		D3D12_RESOURCE_DESC textureResourceDesc = {};
		textureResourceDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
		textureResourceDesc.Alignment = 0;
		textureResourceDesc.Width = uploadBufferSize;
		textureResourceDesc.Height = 1;
		textureResourceDesc.DepthOrArraySize = 1;
		textureResourceDesc.MipLevels = 1;
		textureResourceDesc.Format = DXGI_FORMAT_UNKNOWN;
		textureResourceDesc.SampleDesc.Count = 1;
		textureResourceDesc.SampleDesc.Quality = 0;
		textureResourceDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
		textureResourceDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

		// TODO: Don't create a cpu texture each time especially for static textures and RT!
		D3D12_HEAP_PROPERTIES uploadHeapProperties = {};
		uploadHeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
		uploadHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
		uploadHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
		uploadHeapProperties.CreationNodeMask = 1;
		uploadHeapProperties.VisibleNodeMask = 1;

		ComPtr<ID3D12Resource> cpuTexture;
		AssertIfFailed(this->graphicsDevice->CreateCommittedResource(&uploadHeapProperties, D3D12_HEAP_FLAG_NONE, &textureResourceDesc, D3D12_RESOURCE_STATE_GENERIC_READ, nullptr, IID_PPV_ARGS(cpuTexture.ReleaseAndGetAddressOf())));
		this->cpuTextures[textureId] = cpuTexture;
	}

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
		this->srvtextureDescriptorOffets[textureId] = this->currentGlobalDescriptorOffset;
		this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;

		if (isRenderTarget)
		{
			D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
			rtvDesc.Format = ConvertTextureFormat(textureFormat);
			rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

			auto globalRtvDescriptorHeapHandle = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
			globalRtvDescriptorHeapHandle.ptr += this->currentGlobalRtvDescriptorOffset;

			this->graphicsDevice->CreateRenderTargetView(gpuTexture.Get(), &rtvDesc, globalRtvDescriptorHeapHandle);
			this->textureDescriptorOffets[textureId] = this->currentGlobalRtvDescriptorOffset;
			this->currentGlobalRtvDescriptorOffset += this->globalRtvDescriptorHandleSize;

			// UAV View
			D3D12_UNORDERED_ACCESS_VIEW_DESC  uavDesc = {};
			uavDesc.Format = ConvertTextureFormat(textureFormat);
			uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
			uavDesc.Texture2D.MipSlice = 0;

			globalDescriptorHeapHandle = this->globalDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
			globalDescriptorHeapHandle.ptr += this->currentGlobalDescriptorOffset;

			this->graphicsDevice->CreateUnorderedAccessView(gpuTexture.Get(), nullptr, &uavDesc, globalDescriptorHeapHandle);
			this->uavTextureDescriptorOffets[textureId] = this->currentGlobalDescriptorOffset;
			this->currentGlobalDescriptorOffset += this->globalDescriptorHandleSize;
		}
	}

    return 1;
}*/

void Direct3D12GraphicsService::DeleteTexture(unsigned int textureId)
{ 
	this->textureFootPrints.erase(textureId);
}

// TODO: To remove
struct IndirectCommand
{
	D3D12_GPU_VIRTUAL_ADDRESS cbv;
	D3D12_DRAW_ARGUMENTS drawArguments;
};

int Direct3D12GraphicsService::CreateIndirectCommandBuffer(unsigned int indirectCommandBufferId, int maxCommandCount)
{ 
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
	this->indirectCommandBufferSignatures[indirectCommandBufferId] = commandSignature;

	return CreateGraphicsBufferOld(indirectCommandBufferId, maxCommandCount * sizeof(IndirectCommand), false, "");
}

void Direct3D12GraphicsService::SetIndirectCommandBufferLabel(unsigned int indirectCommandBufferId, char* label)
{
	if (!this->graphicsBuffers.count(indirectCommandBufferId))
	{
		return;
	}

	this->graphicsBuffers[indirectCommandBufferId]->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteIndirectCommandBuffer(unsigned int indirectCommandBufferId)
{

}

int Direct3D12GraphicsService::CreateShader(unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength)
{ 
	auto shader = Shader() = {};

	auto currentDataPtr = (unsigned char*)shaderByteCode;

	auto rootSignatureByteCodeLength = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);
	auto rootSignatureBlob = CreateShaderBlob(currentDataPtr, rootSignatureByteCodeLength);
	currentDataPtr += rootSignatureByteCodeLength;

	ComPtr<ID3D12RootSignature> rootSignature;
	AssertIfFailed(this->graphicsDevice->CreateRootSignature(0, rootSignatureBlob->GetBufferPointer(), rootSignatureBlob->GetBufferSize(), IID_PPV_ARGS(rootSignature.ReleaseAndGetAddressOf())));

	shader.RootSignature = rootSignature;
	
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
			shader.VertexShaderMethod = shaderBlob;
		}

		else if (entryPointName == "PixelMain")
		{
			shader.PixelShaderMethod = shaderBlob;
		}

		else if (entryPointName == string(computeShaderFunction))
		{
			shader.ComputeShaderMethod = shaderBlob;
		}
	}

	this->shaders[shaderId] = shader;
	
    return 1;
}

void Direct3D12GraphicsService::SetShaderLabel(unsigned int shaderId, char* label)
{
	if (!this->shaders.count(shaderId))
	{
		return;
	}

	// TODO: Remove that hack
	auto rootSignatureName = wstring(label, label + strlen(label));

	if (rootSignatureName.compare(L"RenderMeshInstanceShader") == 0)
	{
		this->currentShaderIndirectCommand = this->shaders[shaderId];
	}

	this->shaders[shaderId].RootSignature->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeleteShader(unsigned int shaderId)
{ 

}

int Direct3D12GraphicsService::CreatePipelineState(unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor)
{ 
	if (shaderId == 0)
	{
		return true;
	}

	if (!this->shaders.count(shaderId))
	{
		return false;
	}

	auto shader = this->shaders[shaderId];

	ComPtr<ID3D12PipelineState> pipelineState;

	if (shader.ComputeShaderMethod == nullptr)
	{
		auto primitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;

		if (renderPassDescriptor.PrimitiveType == GraphicsPrimitiveType::Line)
		{
			primitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE;
		}

		D3D12_GRAPHICS_PIPELINE_STATE_DESC psoDesc = {};
		psoDesc.pRootSignature = shader.RootSignature.Get();

		psoDesc.VS = { shader.VertexShaderMethod->GetBufferPointer(), shader.VertexShaderMethod->GetBufferSize() };
		psoDesc.PS = { shader.PixelShaderMethod->GetBufferPointer(), shader.PixelShaderMethod->GetBufferSize() };

		psoDesc.SampleMask = 0xFFFFFF;
		psoDesc.PrimitiveTopologyType = primitiveTopologyType;

		if (!renderPassDescriptor.RenderTarget1TextureId.HasValue && !renderPassDescriptor.DepthTextureId.HasValue)
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
		psoDesc.pRootSignature = shader.RootSignature.Get();
		psoDesc.CS = { shader.ComputeShaderMethod->GetBufferPointer(), shader.ComputeShaderMethod->GetBufferSize() };

		AssertIfFailed(this->graphicsDevice->CreateComputePipelineState(&psoDesc, IID_PPV_ARGS(pipelineState.ReleaseAndGetAddressOf())));
	}

	this->pipelineStates[pipelineStateId] = pipelineState;

    return 1;
}

void Direct3D12GraphicsService::SetPipelineStateLabel(unsigned int pipelineStateId, char* label)
{
	if (!this->pipelineStates.count(pipelineStateId))
	{
		return;
	}

	this->pipelineStates[pipelineStateId]->SetName(wstring(label, label + strlen(label)).c_str());
}

void Direct3D12GraphicsService::DeletePipelineState(unsigned int pipelineStateId)
{ 
	// TODO: Do a delay release after the pso is not in flight anymore
	this->pipelineStates.erase(pipelineStateId);
}

int Direct3D12GraphicsService::CreateCommandBuffer(unsigned int commandBufferId, enum GraphicsCommandBufferType commandBufferType, char* label)
{ 
	auto listType = D3D12_COMMAND_LIST_TYPE_DIRECT;

	if (commandBufferType == GraphicsCommandBufferType::Copy)
	{
		listType = D3D12_COMMAND_LIST_TYPE_COPY;
	}

	else if (commandBufferType == GraphicsCommandBufferType::Compute)
	{
		listType = D3D12_COMMAND_LIST_TYPE_COMPUTE;
	}
	
	this->commandBufferTypes[commandBufferId] = listType;
	this->commandBufferLabels[commandBufferId] = wstring(label, label + strlen(label));

    return 1;
}

void Direct3D12GraphicsService::DeleteCommandBuffer(unsigned int commandBufferId)
{ 
	this->commandBuffers.erase(commandBufferId);
	this->commandBufferTypes.erase(commandBufferId);
	this->commandBufferLabels.erase(commandBufferId);
}

void Direct3D12GraphicsService::ResetCommandBuffer(unsigned int commandBufferId)
{ 
	auto listType = this->commandBufferTypes[commandBufferId];

	ComPtr<ID3D12CommandAllocator> commandAllocator = this->directCommandAllocators[this->currentAllocatorIndex];

	if (listType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandAllocator = this->copyCommandAllocators[this->currentAllocatorIndex];
	}

	else if (listType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandAllocator = this->computeCommandAllocators[this->currentAllocatorIndex];
	}

	if (!this->commandBuffers.count(commandBufferId))
	{
		ComPtr<ID3D12GraphicsCommandList> commandList;
		AssertIfFailed(this->graphicsDevice->CreateCommandList(0, listType, commandAllocator.Get(), nullptr, IID_PPV_ARGS(commandList.ReleaseAndGetAddressOf())));
		commandList->SetName(this->commandBufferLabels[commandBufferId].c_str());

		if (enableTiming)
		{
			if (listType != D3D12_COMMAND_LIST_TYPE_COPY)
			{
				commandList->EndQuery(this->queryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->queryHeapIndex);
				this->commandBufferStartQueryIndex[commandBufferId] = this->queryHeapIndex - this->startQueryIndex;
				this->queryHeapIndex = (this->queryHeapIndex + 1) % QueryHeapMaxSize;
			}

			else
			{
				commandList->EndQuery(this->copyQueryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->copyQueryHeapIndex);
				this->commandBufferStartQueryIndex[commandBufferId] = this->copyQueryHeapIndex - this->startCopyQueryIndex;
				this->copyQueryHeapIndex = (this->copyQueryHeapIndex + 1) % QueryHeapMaxSize;
			}
		}

		this->commandBuffers[commandBufferId] = commandList;

		if (listType != D3D12_COMMAND_LIST_TYPE_COPY)
		{
			ID3D12DescriptorHeap* descriptorHeaps[] = { this->globalDescriptorHeap.Get() };
			commandList->SetDescriptorHeaps(1, descriptorHeaps);
		}
	}

	else
	{
		auto commandBuffer = this->commandBuffers[commandBufferId];
		commandBuffer->Reset(commandAllocator.Get(), nullptr);

		if (enableTiming)
		{
			if (listType != D3D12_COMMAND_LIST_TYPE_COPY)
			{
				commandBuffer->EndQuery(this->queryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->queryHeapIndex);
				this->commandBufferStartQueryIndex[commandBufferId] = this->queryHeapIndex - this->startQueryIndex;
				this->queryHeapIndex = (this->queryHeapIndex + 1) % QueryHeapMaxSize;
			}

			else
			{
				commandBuffer->EndQuery(this->copyQueryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->copyQueryHeapIndex);
				this->commandBufferStartQueryIndex[commandBufferId] = this->copyQueryHeapIndex - this->startCopyQueryIndex;
				this->copyQueryHeapIndex = (this->copyQueryHeapIndex + 1) % QueryHeapMaxSize;
			}
		}

		if (listType != D3D12_COMMAND_LIST_TYPE_COPY)
		{
			ID3D12DescriptorHeap* descriptorHeaps[] = { this->globalDescriptorHeap.Get() };
			commandBuffer->SetDescriptorHeaps(1, descriptorHeaps);
		}
	}
	
	this->shaderBound = false;
}

void Direct3D12GraphicsService::ExecuteCommandBuffer(unsigned int commandBufferId)
{ 
	// TODO: Update Status
	if (this->commandBuffers.count(commandBufferId))
	{
		auto commandBufferType = this->commandBufferTypes[commandBufferId];
		ComPtr<ID3D12CommandQueue> commandQueue = this->directCommandQueue;
		ComPtr<ID3D12Fence1> fence = this->directFence;
		uint64_t fenceValue = this->directFenceValue++;

		if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COPY)
		{
			commandQueue = this->copyCommandQueue;
			fence = this->copyFence;
			fenceValue = this->copyFenceValue++;
		}

		else if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
		{
			commandQueue = this->computeCommandQueue;
			fence = this->computeFence;
			fenceValue = this->computeFenceValue++;
		}

		auto commandBuffer = this->commandBuffers[commandBufferId];

		if (enableTiming)
		{
			if (commandBufferType != D3D12_COMMAND_LIST_TYPE_COPY)
			{
				commandBuffer->EndQuery(this->queryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->queryHeapIndex);
				this->commandBufferEndQueryIndex[commandBufferId] = this->queryHeapIndex - this->startQueryIndex;
				this->queryHeapIndex = (this->queryHeapIndex + 1) % QueryHeapMaxSize;
			}

			else
			{
				commandBuffer->EndQuery(this->copyQueryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->copyQueryHeapIndex);
				this->commandBufferEndQueryIndex[commandBufferId] = this->copyQueryHeapIndex - this->startCopyQueryIndex;
				this->copyQueryHeapIndex = (this->copyQueryHeapIndex + 1) % QueryHeapMaxSize;
			}
		}

		if (this->isPresentBarrier)
		{
			if (enableTiming)
			{
				// TODO: Replace magic number with something better
				auto readBackBuffer = this->currentBackBufferIndex == 0 ? this->readBackBuffers[10000] : this->readBackBuffers[10001];
				commandBuffer->ResolveQueryData(this->queryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->startQueryIndex, this->queryHeapIndex - this->startQueryIndex, readBackBuffer.Get(), 0);

				// Resolve copy timings
				// TODO: Remove that command list creation!
				auto commandAllocator = this->copyCommandAllocators[this->currentAllocatorIndex];
				ComPtr<ID3D12GraphicsCommandList> commandList;
				AssertIfFailed(this->graphicsDevice->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_COPY, commandAllocator.Get(), nullptr, IID_PPV_ARGS(commandList.ReleaseAndGetAddressOf())));

				readBackBuffer = this->currentBackBufferIndex == 0 ? this->readBackBuffers[10002] : this->readBackBuffers[10003];
				commandList->ResolveQueryData(this->copyQueryHeap.Get(), D3D12_QUERY_TYPE_TIMESTAMP, this->startCopyQueryIndex, this->copyQueryHeapIndex - this->startCopyQueryIndex, readBackBuffer.Get(), 0);
				commandList->Close();

				ID3D12CommandList* copyCommandLists[] = { commandList.Get() };
				this->copyCommandQueue->ExecuteCommandLists(1, copyCommandLists);
			}

			this->isPresentBarrier = false;
		}

		commandBuffer->Close();

		ID3D12CommandList* commandLists[] = { commandBuffer.Get() };
		commandQueue->ExecuteCommandLists(1, commandLists);
		
		commandQueue->Signal(fence.Get(), fenceValue);
		this->commandBufferFenceValues[commandBufferId] = fenceValue;
	}
}

NullableGraphicsCommandBufferStatus Direct3D12GraphicsService::GetCommandBufferStatus(unsigned int commandBufferId)
{ 
	auto commandBufferType = this->commandBufferTypes[commandBufferId];
    auto status = NullableGraphicsCommandBufferStatus {};

    status.HasValue = 1;
    status.Value.State = GraphicsCommandBufferState::Completed;

	auto cpuQueryHeap = this->currentCpuQueryHeap;
	auto frequency = this->directQueueFrequency;

	if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		cpuQueryHeap = this->currentCpuCopyQueryHeap;
		frequency = this->copyQueueFrequency;
	}

	if (this->commandBufferEndQueryIndex.count(commandBufferId))
	{
		status.Value.ExecutionStartTime = (cpuQueryHeap[this->commandBufferStartQueryIndex[commandBufferId]] / (double)frequency) * 1000.0;
		status.Value.ExecutionEndTime = (cpuQueryHeap[this->commandBufferEndQueryIndex[commandBufferId]] / (double)frequency) * 1000.0;
	}

    return status; 
}

void Direct3D12GraphicsService::SetShaderBuffer(unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto commandBufferType = this->commandBufferTypes[this->commandListBuffers[commandListId]];
	auto gpuBuffer = this->graphicsBuffers[graphicsBufferId];

	if (commandBufferType == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		commandList->SetGraphicsRootShaderResourceView(slot, gpuBuffer->GetGPUVirtualAddress());
	}

	else if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		// TODO: Remove that hack
		if (slot == 1)
		{
			auto gpuAddress = gpuBuffer->GetGPUVirtualAddress();
			commandList->SetComputeRoot32BitConstants(0, 2, &gpuAddress, 0);
			commandList->SetComputeRootShaderResourceView(slot, gpuBuffer->GetGPUVirtualAddress());
		}
	}
}

void Direct3D12GraphicsService::SetShaderBuffers(unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index)
{ 
	
}

void Direct3D12GraphicsService::SetShaderTexture(unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}
	
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuTexture = this->gpuTextures[textureId];

	auto commandBufferType = this->commandBufferTypes[this->commandListBuffers[commandListId]];

	if (commandBufferType == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_GENERIC_READ);

		auto descriptorHeapdOffset = this->srvtextureDescriptorOffets[textureId];

		D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
		descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

		commandList->SetGraphicsRootDescriptorTable(slot, descriptorHeapHandle);
	}

	else if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		if (isReadOnly)
		{
			TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_GENERIC_READ);
			
			auto descriptorHeapdOffset = this->srvtextureDescriptorOffets[textureId];

			D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
			descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

			commandList->SetComputeRootDescriptorTable(slot, descriptorHeapHandle);
		}

		else
		{
			TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_GENERIC_READ);
			
			auto descriptorHeapdOffset = this->uavTextureDescriptorOffets[textureId];

			D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
			descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

			commandList->SetComputeRootDescriptorTable(slot, descriptorHeapHandle);
		}
	}
}

void Direct3D12GraphicsService::SetShaderTextures(unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	// TODO: Remove that hack
	auto commandBufferType = this->commandBufferTypes[this->commandListBuffers[commandListId]];

	if (commandBufferType != D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];

	ComPtr<ID3D12DescriptorHeap> srvDescriptorHeap;

	// Create Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
	descriptorHeapDesc.NumDescriptors = textureIdListLength;
	descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(srvDescriptorHeap.ReleaseAndGetAddressOf())));

	this->debugDescriptorHeaps[commandListId] = srvDescriptorHeap;

	int srvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	auto heapPtr = srvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
	
	for (int i = 0; i < textureIdListLength; i++)
	{
		auto textureId = textureIdList[i];
		auto gpuTexture = this->gpuTextures[textureId];

		if (gpuTexture->GetDesc().Format == DXGI_FORMAT_D32_FLOAT)
		{
			return;
		}

		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Format = gpuTexture->GetDesc().Format;
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Texture2D.MipLevels = gpuTexture->GetDesc().MipLevels;
		this->graphicsDevice->CreateShaderResourceView(gpuTexture.Get(), &srvDesc, heapPtr);

		heapPtr.ptr += srvDescriptorHandleSize;

		TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_GENERIC_READ);
	}

	// TODO: Change that because if the next shader invoke needs the global heap it will not work

	ID3D12DescriptorHeap* descriptorHeaps[] = { srvDescriptorHeap.Get() };
	commandList->SetDescriptorHeaps(1, descriptorHeaps);
	commandList->SetGraphicsRootDescriptorTable(slot, srvDescriptorHeap->GetGPUDescriptorHandleForHeapStart());
}

void Direct3D12GraphicsService::SetShaderIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index){ }
void Direct3D12GraphicsService::SetShaderIndirectCommandLists(unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	// TODO: Remove that hack
	if (slot != 30006)
	{
		return;
	}

	auto bufferId = indirectCommandListIdList[3];

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuBuffer = this->graphicsBuffers[bufferId];

	auto commandBufferType = this->commandBufferTypes[this->commandListBuffers[commandListId]];

	TransitionBufferToState(commandListId, bufferId, D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

	auto descriptorHeapdOffset = this->uavBufferDescriptorOffets[bufferId];

	D3D12_GPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
	descriptorHeapHandle.ptr = this->globalDescriptorHeap->GetGPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

	commandList->SetComputeRootDescriptorTable(2, descriptorHeapHandle);
}

int Direct3D12GraphicsService::CreateCopyCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label)
{
	// TODO: Find a way to erase the data mapping at some point in time
	this->commandListBuffers[commandListId] = commandBufferId;
    return 1;
}

void Direct3D12GraphicsService::CommitCopyCommandList(unsigned int commandListId)
{ 
}

void Direct3D12GraphicsService::CopyDataToGraphicsBuffer(unsigned int commandListId, unsigned int destinationGraphicsBufferId, unsigned int sourceGraphicsBufferId, int sizeInBytes)
{ 
	if (!this->graphicsBuffers.count(destinationGraphicsBufferId) && !this->graphicsBuffers.count(sourceGraphicsBufferId))
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto destinationGraphicsBuffer = this->graphicsBuffers[destinationGraphicsBufferId];
	auto sourceGraphicsBuffer = this->graphicsBuffers[sourceGraphicsBufferId];

	if (this->graphicsBufferPointers.count(destinationGraphicsBufferId))
	{
		D3D12_RANGE range = { 0, 0 };
		destinationGraphicsBuffer->Unmap(0, &range);
		this->graphicsBufferPointers.erase(destinationGraphicsBufferId);
	}

	commandList->CopyBufferRegion(destinationGraphicsBuffer.Get(), 0, sourceGraphicsBuffer.Get(), 0, sizeInBytes);
}

void Direct3D12GraphicsService::CopyDataToTexture(unsigned int commandListId, unsigned int destinationTextureId, unsigned int sourceGraphicsBufferId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
{
	if (!this->gpuTextures.count(destinationTextureId) && !this->graphicsBuffers.count(sourceGraphicsBufferId))
	{
		return;
	}

	// TODO: For the moment it only takes into account the mip level
	if (mipLevel > 0)
	{
		return;
	}

	TransitionTextureToState(commandListId, destinationTextureId, D3D12_RESOURCE_STATE_COPY_DEST);

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto destinationTexture = this->gpuTextures[destinationTextureId];
	auto sourceGraphicsBuffer = this->graphicsBuffers[sourceGraphicsBufferId];
	auto footPrint = this->textureFootPrints[destinationTextureId];

	D3D12_TEXTURE_COPY_LOCATION destinationLocation = {};
	destinationLocation.pResource = destinationTexture.Get();
	destinationLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
	destinationLocation.SubresourceIndex = mipLevel;

	D3D12_TEXTURE_COPY_LOCATION sourceLocation = {};
	sourceLocation.pResource = sourceGraphicsBuffer.Get();
	sourceLocation.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
	sourceLocation.PlacedFootprint = footPrint;

	commandList->CopyTextureRegion(&destinationLocation, 0, 0, 0, &sourceLocation, nullptr);
}

void Direct3D12GraphicsService::CopyTexture(unsigned int commandListId, unsigned int destinationTextureId, unsigned int sourceTextureId)
{
	if (!this->gpuTextures.count(destinationTextureId) && !this->gpuTextures.count(sourceTextureId))
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto destinationTexture = this->gpuTextures[destinationTextureId];
	auto sourceTexture = this->gpuTextures[sourceTextureId];

	commandList->CopyResource(destinationTexture.Get(), sourceTexture.Get());
}

void Direct3D12GraphicsService::ResetIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount){ }
void Direct3D12GraphicsService::OptimizeIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int maxCommandCount){ }

int Direct3D12GraphicsService::CreateComputeCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label)
{ 
	this->commandListBuffers[commandListId] = commandBufferId;
    return 1;
}

void Direct3D12GraphicsService::CommitComputeCommandList(unsigned int commandListId)
{ 
}

struct Vector3 Direct3D12GraphicsService::DispatchThreads(unsigned int commandListId, unsigned int threadCountX, unsigned int threadCountY, unsigned int threadCountZ)
{ 
	if (!this->shaderBound)
	{
		return Vector3 { 32, 32, 1 };
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];

	// TODO: Change that
	commandList->Dispatch(ceil(threadCountX / 32.0f), ceil(threadCountY / 32.0f), 1);

    return Vector3 { 32, 32, 1 };
}

int Direct3D12GraphicsService::CreateRenderCommandList(unsigned int commandListId, unsigned int commandBufferId, struct GraphicsRenderPassDescriptor renderDescriptor, char* label)
{ 
	if (!this->commandBuffers.count(commandBufferId))
	{
		return 0;
	}

	this->commandListBuffers[commandListId] = commandBufferId;
	this->commandListRenderPassDescriptors[commandListId] = renderDescriptor;
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];

	if (renderDescriptor.RenderTarget1TextureId.HasValue)
	{
		auto gpuTexture = this->gpuTextures[renderDescriptor.RenderTarget1TextureId.Value];
		auto descriptorHeapdOffset = this->textureDescriptorOffets[renderDescriptor.RenderTarget1TextureId.Value];

		D3D12_CPU_DESCRIPTOR_HANDLE descriptorHeapHandle = {};
		descriptorHeapHandle.ptr = this->globalRtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + descriptorHeapdOffset;

		TransitionTextureToState(commandListId, renderDescriptor.RenderTarget1TextureId.Value, D3D12_RESOURCE_STATE_RENDER_TARGET);
		commandList->OMSetRenderTargets(1, &descriptorHeapHandle, false, nullptr);

		if (renderDescriptor.RenderTarget1ClearColor.HasValue)
		{
			// float clearColor[4] = { renderDescriptor.RenderTarget1ClearColor.Value.X, renderDescriptor.RenderTarget1ClearColor.Value.Y, renderDescriptor.RenderTarget1ClearColor.Value.Z, renderDescriptor.RenderTarget1ClearColor.Value.W };
			float clearColor[4] = {};
			commandList->ClearRenderTargetView(descriptorHeapHandle, clearColor, 0, nullptr);
		}

		D3D12_VIEWPORT viewport = {};
		viewport.Width = (float)gpuTexture->GetDesc().Width;
		viewport.Height = (float)gpuTexture->GetDesc().Height;
		viewport.MaxDepth = 1.0f;
		commandList->RSSetViewports(1, &viewport);

		D3D12_RECT scissorRect = {};
		scissorRect.right = (long)gpuTexture->GetDesc().Width;
		scissorRect.bottom = (long)gpuTexture->GetDesc().Height;
		commandList->RSSetScissorRects(1, &scissorRect);
	}

	else if (!renderDescriptor.RenderTarget1TextureId.HasValue && !renderDescriptor.DepthTextureId.HasValue)
	{
		D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = GetCurrentRenderTargetViewHandle();

		commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(this->backBufferRenderTargets[this->currentBackBufferIndex].Get(), D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET));
		commandList->OMSetRenderTargets(1, &renderTargetViewHandle, false, nullptr);

		D3D12_VIEWPORT viewport = {};
		viewport.Width = (float)this->backBufferRenderTargets[this->currentBackBufferIndex]->GetDesc().Width;
		viewport.Height = (float)this->backBufferRenderTargets[this->currentBackBufferIndex]->GetDesc().Height;
		viewport.MaxDepth = 1.0f;
		commandList->RSSetViewports(1, &viewport);

		D3D12_RECT scissorRect = {};
		scissorRect.right = (long)this->backBufferRenderTargets[this->currentBackBufferIndex]->GetDesc().Width;
		scissorRect.bottom = (long)this->backBufferRenderTargets[this->currentBackBufferIndex]->GetDesc().Height;
		commandList->RSSetScissorRects(1, &scissorRect);
	}
	
    return 1;
}

void Direct3D12GraphicsService::CommitRenderCommandList(unsigned int commandListId)
{ 
	auto renderDescriptor = this->commandListRenderPassDescriptors[commandListId];

	if (renderDescriptor.RenderTarget1TextureId.HasValue)
	{
		auto gpuTexture = this->gpuTextures[renderDescriptor.RenderTarget1TextureId.Value];
		TransitionTextureToState(commandListId, renderDescriptor.RenderTarget1TextureId.Value, D3D12_RESOURCE_STATE_GENERIC_READ);
	}

	this->commandListRenderPassDescriptors.erase(commandListId);
}

void Direct3D12GraphicsService::SetPipelineState(unsigned int commandListId, unsigned int pipelineStateId)
{ 
	if (pipelineStateId == 0)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto pipelineState = this->pipelineStates[pipelineStateId];

	if (!pipelineState)
	{
		return;
	}

	commandList->SetPipelineState(pipelineState.Get());
}

void Direct3D12GraphicsService::SetShader(unsigned int commandListId, unsigned int shaderId)
{ 
	if (shaderId == 0)
	{
		this->shaderBound = false;
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto shader = this->shaders[shaderId];
	auto commandBufferType = this->commandBufferTypes[this->commandListBuffers[commandListId]];

	if (commandBufferType == D3D12_COMMAND_LIST_TYPE_DIRECT)
	{
		commandList->SetGraphicsRootSignature(shader.RootSignature.Get());
	}

	else if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandList->SetComputeRootSignature(shader.RootSignature.Get());
	}

	this->shaderBound = true;
}

void Direct3D12GraphicsService::BindGraphicsHeap(unsigned int commandListId, unsigned int graphicsHeapId)
{

}

void Direct3D12GraphicsService::ExecuteIndirectCommandBuffer(unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto indirectCommandBuffer = this->graphicsBuffers[indirectCommandBufferId];
	TransitionBufferToState(commandListId, indirectCommandBufferId, D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT);

	auto signature = this->indirectCommandBufferSignatures[indirectCommandBufferId];

	commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

	// TODO: Compute the count in the shader?
	commandList->ExecuteIndirect(signature.Get(), 1, indirectCommandBuffer.Get(), 0, nullptr, 0);
}

void Direct3D12GraphicsService::SetIndexBuffer(unsigned int commandListId, unsigned int graphicsBufferId)
{ 
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto indexBuffer = this->graphicsBuffers[graphicsBufferId];

	D3D12_INDEX_BUFFER_VIEW indexBufferView = {};
	indexBufferView.BufferLocation = indexBuffer->GetGPUVirtualAddress();
	indexBufferView.SizeInBytes = indexBuffer->GetDesc().Width;
	indexBufferView.Format = DXGI_FORMAT_R32_UINT;

	commandList->IASetIndexBuffer(&indexBufferView);
}

void Direct3D12GraphicsService::DrawIndexedPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];

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

	commandList->DrawIndexedInstanced(indexCount, instanceCount, startIndex, 0, baseInstanceId);
}

void Direct3D12GraphicsService::DrawPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];

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

void Direct3D12GraphicsService::WaitForCommandList(unsigned int commandListId, unsigned int commandListToWaitId)
{ 
	auto commandBufferType = this->commandBufferTypes[this->commandListBuffers[commandListId]];
	auto commandBufferWaitType = this->commandBufferTypes[this->commandListBuffers[commandListToWaitId]];

	auto commandQueue = this->directCommandQueue;
	auto fence = this->directFence;
	auto fenceValue = this->commandBufferFenceValues[this->commandListBuffers[commandListToWaitId]];

	if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandQueue = this->copyCommandQueue;
	}

	else if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandQueue = this->computeCommandQueue;
	}

	if (commandBufferWaitType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		fence = this->copyFence;
	}

	else if (commandBufferWaitType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		fence = this->computeFence;
	}

	commandQueue->Wait(fence.Get(), fenceValue);
}

void Direct3D12GraphicsService::PresentScreenBuffer(unsigned int commandBufferId)
{ 
	if (!this->commandBuffers.count(commandBufferId))
	{
		return;
	}
	
	auto commandList = this->commandBuffers[commandBufferId];
	commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(this->backBufferRenderTargets[this->currentBackBufferIndex].Get(), D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT));	
	
	this->isPresentBarrier = true;
}

void Direct3D12GraphicsService::WaitForAvailableScreenBuffer()
{ 
	this->directCommandQueue->GetTimestampFrequency(&this->directQueueFrequency);
	this->computeCommandQueue->GetTimestampFrequency(&this->computeQueueFrequency);
	this->copyCommandQueue->GetTimestampFrequency(&this->copyQueueFrequency);

	this->presentFences[this->currentBackBufferIndex] = this->directFenceValue;

	AssertIfFailed(this->swapChain->Present(1, 0));
	AssertIfFailed(this->directCommandQueue->Signal(this->directFence.Get(), this->directFenceValue++));
	AssertIfFailed(this->copyCommandQueue->Signal(this->copyFence.Get(), this->copyFenceValue++));
	AssertIfFailed(this->computeCommandQueue->Signal(this->computeFence.Get(), this->computeFenceValue++));

	this->currentBackBufferIndex = this->swapChain->GetCurrentBackBufferIndex();

	WaitForGlobalFence(false);

	this->currentAllocatorIndex = (this->currentAllocatorIndex + 1) % CommandAllocatorsCount;

	this->directCommandAllocators[this->currentAllocatorIndex]->Reset();
	this->copyCommandAllocators[this->currentAllocatorIndex]->Reset();
	this->computeCommandAllocators[this->currentAllocatorIndex]->Reset();

	// TODO: Change that hack
	if (this->queryHeapIndex > QueryHeapMaxSize - 100)
	{
		this->queryHeapIndex = 0;		
	}

	if (this->copyQueryHeapIndex > QueryHeapMaxSize - 100)
	{
		this->copyQueryHeapIndex = 0;		
	}

	this->startQueryIndex = this->queryHeapIndex;
	this->startCopyQueryIndex = this->copyQueryHeapIndex;

	// Copy data from currentFrame - 2
	D3D12_RANGE range = { 0, GetAlignedValue(QueryHeapMaxSize * sizeof(uint64_t), 64 * 1024) };
	uint64_t* pointer;

	uint32_t readBackBufferId = this->currentBackBufferIndex == 0 ? 10000 : 10001;
	AssertIfFailed(this->readBackBuffers[readBackBufferId]->Map(0, &range, (void**)&pointer));

	// TODO: Do we really need to do a copy of the readback buffer?
	memcpy(this->currentCpuQueryHeap, pointer, QueryHeapMaxSize);

	D3D12_RANGE unmapRange = { 0, 0 };
	this->readBackBuffers[readBackBufferId]->Unmap(0, &unmapRange);

	readBackBufferId = this->currentBackBufferIndex == 0 ? 10002 : 10003;
	AssertIfFailed(this->readBackBuffers[readBackBufferId]->Map(0, &range, (void**)&pointer));

	// TODO: Do we really need to do a copy of the readback buffer?
	memcpy(this->currentCpuCopyQueryHeap, pointer, QueryHeapMaxSize);

	this->readBackBuffers[readBackBufferId]->Unmap(0, &unmapRange);
}

void Direct3D12GraphicsService::WaitForGlobalFence(bool waitForAllPendingWork)
{
	if (!this->isWaitingForGlobalFence)
	{
		this->isWaitingForGlobalFence = true;

		if (!waitForAllPendingWork)
		{
			auto presentFenceValue = this->presentFences[this->currentBackBufferIndex];

			if (this->directFence->GetCompletedValue() < presentFenceValue)
			{
				this->directFence->SetEventOnCompletion(presentFenceValue, this->globalFenceEvent);
				WaitForSingleObject(this->globalFenceEvent, INFINITE);
			}
		}

		else
		{
			if (this->directFence->GetCompletedValue() < (this->directFenceValue - 1))
			{
				this->directFence->SetEventOnCompletion((this->directFenceValue - 1), this->globalFenceEvent);
				WaitForSingleObject(this->globalFenceEvent, INFINITE);
			}

			if (this->copyFence->GetCompletedValue() < (this->copyFenceValue - 1))
			{
				this->copyFence->SetEventOnCompletion((this->copyFenceValue - 1), this->globalFenceEvent);
				WaitForSingleObject(this->globalFenceEvent, INFINITE);
			}

			if (this->computeFence->GetCompletedValue() < (this->computeFenceValue - 1))
			{
				this->computeFence->SetEventOnCompletion((this->computeFenceValue - 1), this->globalFenceEvent);
				WaitForSingleObject(this->globalFenceEvent, INFINITE);
			}
		}

		this->isWaitingForGlobalFence = false;
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

	// Create the direct command queue
	D3D12_COMMAND_QUEUE_DESC directCommandQueueDesc = {};
	directCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	directCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&directCommandQueueDesc, IID_PPV_ARGS(this->directCommandQueue.ReleaseAndGetAddressOf())));
	this->directCommandQueue->SetName(L"DirectCommandQueue");

	ComPtr<ID3D12Fence1> directFence;
	AssertIfFailed(this->graphicsDevice->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(directFence.ReleaseAndGetAddressOf())));
	directFence->SetName(L"DirectQueueFence");
	this->directFence = directFence;

	// Create the copy command queue
	D3D12_COMMAND_QUEUE_DESC copyCommandQueueDesc = {};
	copyCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	copyCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COPY;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&copyCommandQueueDesc, IID_PPV_ARGS(this->copyCommandQueue.ReleaseAndGetAddressOf())));
	this->copyCommandQueue->SetName(L"CopyCommandQueue");

	ComPtr<ID3D12Fence1> copyFence;
	AssertIfFailed(this->graphicsDevice->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(copyFence.ReleaseAndGetAddressOf())));
	copyFence->SetName(L"CopyQueueFence");
	this->copyFence = copyFence;

	// Create the compute command queue
	D3D12_COMMAND_QUEUE_DESC computeCommandQueueDesc = {};
	computeCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	computeCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COMPUTE;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&computeCommandQueueDesc, IID_PPV_ARGS(this->computeCommandQueue.ReleaseAndGetAddressOf())));
	this->computeCommandQueue->SetName(L"ComputeCommandQueue");

	ComPtr<ID3D12Fence1> computeFence;
	AssertIfFailed(this->graphicsDevice->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(computeFence.ReleaseAndGetAddressOf())));
	computeFence->SetName(L"ComputeQueueFence");
	this->computeFence = computeFence;

	// Init command allocators for each frame in flight
	for (int i = 0; i < CommandAllocatorsCount; i++)
	{
		ComPtr<ID3D12CommandAllocator> renderCommandAllocator;
		AssertIfFailed(this->graphicsDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(renderCommandAllocator.ReleaseAndGetAddressOf())));

		wchar_t directBuff[64] = {};
  		swprintf(directBuff, L"DirectCommandAllocator%d", i);
		renderCommandAllocator->SetName(directBuff);
		this->directCommandAllocators[i] = renderCommandAllocator;

		ComPtr<ID3D12CommandAllocator> copyCommandAllocator;
		AssertIfFailed(this->graphicsDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_COPY, IID_PPV_ARGS(copyCommandAllocator.ReleaseAndGetAddressOf())));

		wchar_t copyBuff[64] = {};
  		swprintf(copyBuff, L"CopyCommandAllocator%d", i);
		copyCommandAllocator->SetName(copyBuff);
		this->copyCommandAllocators[i] = copyCommandAllocator;

		ComPtr<ID3D12CommandAllocator> computeCommandAllocator;
		AssertIfFailed(this->graphicsDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_COMPUTE, IID_PPV_ARGS(computeCommandAllocator.ReleaseAndGetAddressOf())));

		wchar_t computeBuff[64] = {};
  		swprintf(computeBuff, L"ComputeCommandAllocator%d", i);
		computeCommandAllocator->SetName(computeBuff);
		this->computeCommandAllocators[i] = computeCommandAllocator;
	}

	for (int i = 0; i < RenderBuffersCount; i++)
	{
		this->presentFences[i] = 0;
	}

	return true;
}

bool Direct3D12GraphicsService::CreateOrResizeSwapChain(int width, int height)
{
	if (width == 0 || height == 0)
	{
		return true;
	}

	this->currentRenderSize = { (float)width, (float)height };

	// Create the swap chain
	// TODO: Check for supported formats
	// TODO: Add support for HDR displays
	// TODO: Add support for resizing

	if (this->swapChain)
	{
		// Wait until all previous GPU work is complete.
		WaitForGlobalFence(true);

		// Release resources that are tied to the swap chain and update fence values.
		for (int i = 0; i < RenderBuffersCount; i++)
		{
			this->backBufferRenderTargets[i].Reset();
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

	// TODO: Move that to the global rtv descriptor heap

	// Describe and create a render target view (RTV) descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
	rtvHeapDesc.NumDescriptors = RenderBuffersCount;
	rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	
	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS(this->rtvDescriptorHeap.ReleaseAndGetAddressOf())));
	this->rtvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

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

	// Create readback heap
	heapDescriptor = {};
	heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_READBACK;
	heapDescriptor.Properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
	heapDescriptor.SizeInBytes = 1024 * 1024 * 100; // Allocate 100 MB for now
	heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS;

	AssertIfFailed(this->graphicsDevice->CreateHeap(&heapDescriptor, IID_PPV_ARGS(this->readBackHeap.ReleaseAndGetAddressOf())));

	// Create global GPU heap
	heapDescriptor = {};
	heapDescriptor.Properties.Type = D3D12_HEAP_TYPE_DEFAULT;
	heapDescriptor.SizeInBytes = 1024 * 1024 * 1024; // Allocate 1GB for now
	heapDescriptor.Flags = D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES;

	AssertIfFailed(this->graphicsDevice->CreateHeap(&heapDescriptor, IID_PPV_ARGS(this->globalHeap.ReleaseAndGetAddressOf())));

	this->currentUploadHeapOffset = 0;
	this->currentGlobalHeapOffset = 0;

	// Create global Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
	descriptorHeapDesc.NumDescriptors = 10000; //TODO: Change that
	descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(this->globalDescriptorHeap.ReleaseAndGetAddressOf())));
	this->globalDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	this->currentGlobalDescriptorOffset = 0;

	// Create global RTV Descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvDescriptorHeapDesc = {};
	rtvDescriptorHeapDesc.NumDescriptors = 1000; //TODO: Change that
	rtvDescriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvDescriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

	AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&rtvDescriptorHeapDesc, IID_PPV_ARGS(this->globalRtvDescriptorHeap.ReleaseAndGetAddressOf())));
	this->globalRtvDescriptorHandleSize = this->graphicsDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
	this->currentGlobalRtvDescriptorOffset = 0;

	return true;
}

D3D12_CPU_DESCRIPTOR_HANDLE Direct3D12GraphicsService::GetCurrentRenderTargetViewHandle()
{
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = {};
	renderTargetViewHandle.ptr = this->rtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + this->currentBackBufferIndex * this->rtvDescriptorHandleSize;

	return renderTargetViewHandle;
}

// TODO: Make it generic to all resource types
void Direct3D12GraphicsService::TransitionTextureToState(unsigned int commandListId, unsigned int textureId, D3D12_RESOURCE_STATES destinationState)
{
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuTexture = this->gpuTextures[textureId];
	auto actualState = this->textureResourceStates[textureId];

	if (actualState != destinationState)
	{
		commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(gpuTexture.Get(), actualState, destinationState));
		this->textureResourceStates[textureId] = destinationState;
	}
}

// TODO: Make it generic to all resource types
void Direct3D12GraphicsService::TransitionBufferToState(unsigned int commandListId, unsigned int bufferId, D3D12_RESOURCE_STATES destinationState)
{
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuBuffer = this->graphicsBuffers[bufferId];
	auto actualState = this->bufferResourceStates[bufferId];

	if (actualState != destinationState)
	{
		commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(gpuBuffer.Get(), actualState, destinationState));
		this->bufferResourceStates[bufferId] = destinationState;
	}
}

void Direct3D12GraphicsService::InitGpuProfiling()
{
	D3D12_QUERY_HEAP_DESC heapDesc = {};
	heapDesc.Count = QueryHeapMaxSize;
	heapDesc.NodeMask = 0;
	heapDesc.Type = D3D12_QUERY_HEAP_TYPE_TIMESTAMP;

	AssertIfFailed(this->graphicsDevice->CreateQueryHeap(&heapDesc, IID_PPV_ARGS(this->queryHeap.ReleaseAndGetAddressOf())));

	CreateGraphicsBufferOld(10000, QueryHeapMaxSize * sizeof(uint64_t), false, "QueryReadBackBuffer0");
	CreateGraphicsBufferOld(10001, QueryHeapMaxSize * sizeof(uint64_t), false, "QueryReadBackBuffer1");

	heapDesc = {};
	heapDesc.Count = QueryHeapMaxSize;
	heapDesc.NodeMask = 0;
	heapDesc.Type = D3D12_QUERY_HEAP_TYPE_COPY_QUEUE_TIMESTAMP;

	AssertIfFailed(this->graphicsDevice->CreateQueryHeap(&heapDesc, IID_PPV_ARGS(this->copyQueryHeap.ReleaseAndGetAddressOf())));

	CreateGraphicsBufferOld(10002, QueryHeapMaxSize * sizeof(uint64_t), false, "CopyQueryReadBackBuffer0");
	CreateGraphicsBufferOld(10003, QueryHeapMaxSize * sizeof(uint64_t), false, "CopyQueryReadBackBuffer1");

	this->currentCpuQueryHeap = new uint64_t[QueryHeapMaxSize];
	this->currentCpuCopyQueryHeap = new uint64_t[QueryHeapMaxSize];
}
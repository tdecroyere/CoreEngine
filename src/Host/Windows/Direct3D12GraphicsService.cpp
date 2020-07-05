#pragma once
#include "WindowsCommon.h"
#include "Direct3D12GraphicsService.h"
#include "Direct3D12GraphicsServiceUtils.h"

using namespace std;
using namespace Microsoft::WRL;

#define GetAlignedValue(value, alignement) (value + (alignement - (value % alignement)) % alignement)

Direct3D12GraphicsService::Direct3D12GraphicsService(HWND window, int width, int height, GameState* gameState)
{
	this->gameState = gameState;
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

int Direct3D12GraphicsService::CreateTexture(unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, int isRenderTarget, char* label)
{ 
	// TODO: Support mip levels
	// TODO: Switch to placed resources
	D3D12_RESOURCE_DESC textureDesc = {};
	textureDesc.MipLevels = 1;//mipLevels;
	textureDesc.Format = ConvertTextureFormat(textureFormat);
	textureDesc.Width = width;
	textureDesc.Height = height;
	textureDesc.DepthOrArraySize = 1;
	textureDesc.SampleDesc.Count = multisampleCount;
	textureDesc.SampleDesc.Quality = 0;
	textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
	textureDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;

	D3D12_CLEAR_VALUE* clearValue = nullptr;

	if (isRenderTarget) 
	{
		if (textureFormat == GraphicsTextureFormat::Depth32Float)
		{
			textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
		}

		else
		{
			textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

			// TODO: To remove?
			D3D12_CLEAR_VALUE rawClearValue = {};
			rawClearValue.Format = ConvertTextureFormat(textureFormat);
			clearValue = &rawClearValue;
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
	AssertIfFailed(this->graphicsDevice->CreateCommittedResource(&defaultHeapProperties, D3D12_HEAP_FLAG_NONE, &textureDesc, D3D12_RESOURCE_STATE_COPY_DEST, clearValue, IID_PPV_ARGS(gpuTexture.ReleaseAndGetAddressOf())));
	this->gpuTextures[textureId] = gpuTexture;
	this->textureResourceStates[textureId] = D3D12_RESOURCE_STATE_COPY_DEST;

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
		// Create Descriptor heap
		D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
		descriptorHeapDesc.NumDescriptors = 1;
		descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
		descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

		ComPtr<ID3D12DescriptorHeap> srvDescriptorHeap;
		AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(srvDescriptorHeap.ReleaseAndGetAddressOf())));

		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Format = ConvertTextureFormat(textureFormat);
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Texture2D.MipLevels = 1;//mipLevels;

		this->graphicsDevice->CreateShaderResourceView(gpuTexture.Get(), &srvDesc, srvDescriptorHeap->GetCPUDescriptorHandleForHeapStart());
		this->srvtextureDescriptorHeaps[textureId] = srvDescriptorHeap;

		if (isRenderTarget)
		{
			// Create Descriptor heap
			D3D12_DESCRIPTOR_HEAP_DESC descriptorHeapDesc = {};
			descriptorHeapDesc.NumDescriptors = 1;
			descriptorHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
			descriptorHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;

			ComPtr<ID3D12DescriptorHeap> descriptorHeap;
			AssertIfFailed(this->graphicsDevice->CreateDescriptorHeap(&descriptorHeapDesc, IID_PPV_ARGS(descriptorHeap.ReleaseAndGetAddressOf())));

			D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
			rtvDesc.Format = ConvertTextureFormat(textureFormat);
			rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

			this->graphicsDevice->CreateRenderTargetView(gpuTexture.Get(), &rtvDesc, descriptorHeap->GetCPUDescriptorHandleForHeapStart());
			this->textureDescriptorHeaps[textureId] = descriptorHeap;
		}
	}

    return 1;
}

void Direct3D12GraphicsService::DeleteTexture(unsigned int textureId)
{ 
	this->textureFootPrints.erase(textureId);
}

int Direct3D12GraphicsService::CreateIndirectCommandBuffer(unsigned int indirectCommandBufferId, int maxCommandCount, char* label)
{ 
    return 1;
}

int Direct3D12GraphicsService::CreateShader(unsigned int shaderId, char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength, char* label)
{ 
	auto currentDataPtr = (unsigned char*)shaderByteCode;
	
	auto vertexShaderByteCodeLength = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);

	auto vertexShaderBlob = CreateShaderBlob(currentDataPtr, vertexShaderByteCodeLength);
	currentDataPtr += vertexShaderByteCodeLength;

	auto pixelShaderByteCodeLength = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);

	auto pixelShaderBlob = CreateShaderBlob(currentDataPtr, pixelShaderByteCodeLength);
	currentDataPtr += pixelShaderByteCodeLength;

	auto rootSignatureByteCodeLength = (*(int*)currentDataPtr);
	currentDataPtr += sizeof(int);

	auto rootSignatureBlob = CreateShaderBlob(currentDataPtr, rootSignatureByteCodeLength);

	ComPtr<ID3D12RootSignature> rootSignature;
	AssertIfFailed(this->graphicsDevice->CreateRootSignature(0, rootSignatureBlob->GetBufferPointer(), rootSignatureBlob->GetBufferSize(), IID_PPV_ARGS(rootSignature.ReleaseAndGetAddressOf())));

	rootSignature->SetName((wstring(L"RootSignature") + wstring(label, label + strlen(label))).c_str());

	auto shader = Shader();
	shader.VertexShaderMethod = vertexShaderBlob;
	shader.PixelShaderMethod = pixelShaderBlob;
	shader.RootSignature = rootSignature;

	this->shaders[shaderId] = shader;
	
    return 1;
}

void Direct3D12GraphicsService::DeleteShader(unsigned int shaderId)
{ 

}

int Direct3D12GraphicsService::CreatePipelineState(unsigned int pipelineStateId, unsigned int shaderId, struct GraphicsRenderPassDescriptor renderPassDescriptor, char* label)
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
	auto labelString = string(label);

	auto primitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;

	// TODO: Remove that hack
	if (labelString == "DebugRender")
	{
		primitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE;
	}

	// Describe and create the graphics pipeline state object (PSO)
	const D3D12_RENDER_TARGET_BLEND_DESC defaultRenderTargetBlendDesc =
	{
		true,
		false,
		D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA, D3D12_BLEND_OP_ADD,
		D3D12_BLEND_ONE, D3D12_BLEND_ZERO, D3D12_BLEND_OP_ADD,
		D3D12_LOGIC_OP_NOOP,
		D3D12_COLOR_WRITE_ENABLE_ALL,
	};

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

	// for (int i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; ++i)
	// {
	psoDesc.BlendState.RenderTarget[0] = defaultRenderTargetBlendDesc;
	// }

	ComPtr<ID3D12PipelineState> pipelineState;
	AssertIfFailed(this->graphicsDevice->CreateGraphicsPipelineState(&psoDesc, IID_PPV_ARGS(pipelineState.ReleaseAndGetAddressOf())));
	pipelineState->SetName((wstring(L"PSO_") + wstring(label, label + strlen(label))).c_str());

	this->pipelineStates[pipelineStateId] = pipelineState;

    return 1;
}

void Direct3D12GraphicsService::DeletePipelineState(unsigned int pipelineStateId)
{ 
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

	ComPtr<ID3D12CommandAllocator> commandAllocator = this->directCommandAllocators[this->currentBackBufferIndex];

	if (listType == D3D12_COMMAND_LIST_TYPE_COPY)
	{
		commandAllocator = this->copyCommandAllocators[this->currentBackBufferIndex];
	}

	else if (listType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
	{
		commandAllocator = this->computeCommandAllocators[this->currentBackBufferIndex];
	}

	if (!this->commandBuffers.count(commandBufferId))
	{
		ComPtr<ID3D12GraphicsCommandList> commandList;
		AssertIfFailed(this->graphicsDevice->CreateCommandList(0, listType, commandAllocator.Get(), nullptr, IID_PPV_ARGS(commandList.ReleaseAndGetAddressOf())));
		commandList->SetName(this->commandBufferLabels[commandBufferId].c_str());

		this->commandBuffers[commandBufferId] = commandList;
	}

	else
	{
		auto commandBuffer = this->commandBuffers[commandBufferId];
		commandBuffer->Reset(commandAllocator.Get(), nullptr);
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

		if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COPY)
		{
			commandQueue = this->copyCommandQueue;
		}

		else if (commandBufferType == D3D12_COMMAND_LIST_TYPE_COMPUTE)
		{
			commandQueue = this->computeCommandQueue;
		}

		auto commandBuffer = this->commandBuffers[commandBufferId];
		commandBuffer->Close();

		ID3D12CommandList* commandLists[] = { commandBuffer.Get() };
		commandQueue->ExecuteCommandLists(1, commandLists);
	}
}

NullableGraphicsCommandBufferStatus Direct3D12GraphicsService::GetCommandBufferStatus(unsigned int commandBufferId)
{ 
    auto status = NullableGraphicsCommandBufferStatus {};

    status.HasValue = 1;
    status.Value.State = GraphicsCommandBufferState::Completed;

    return status; 
}

void Direct3D12GraphicsService::SetShaderBuffer(unsigned int commandListId, unsigned int graphicsBufferId, int slot, int isReadOnly, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuBuffer = this->gpuBuffers[graphicsBufferId];

	commandList->SetGraphicsRootShaderResourceView(slot, gpuBuffer->GetGPUVirtualAddress());
}

void Direct3D12GraphicsService::SetShaderBuffers(unsigned int commandListId, unsigned int* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index){ }

void Direct3D12GraphicsService::SetShaderTexture(unsigned int commandListId, unsigned int textureId, int slot, int isReadOnly, int index)
{ 
	if (!this->shaderBound)
	{
		return;
	}
	
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuTexture = this->gpuTextures[textureId];
	auto descriptorHeap = this->srvtextureDescriptorHeaps[textureId];

	TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_GENERIC_READ);

	ID3D12DescriptorHeap* descriptorHeaps[] = { descriptorHeap.Get() };
	commandList->SetDescriptorHeaps(1, descriptorHeaps);
	commandList->SetGraphicsRootDescriptorTable(slot, descriptorHeap->GetGPUDescriptorHandleForHeapStart());
}

void Direct3D12GraphicsService::SetShaderTextures(unsigned int commandListId, unsigned int* textureIdList, int textureIdListLength, int slot, int index)
{ 
	if (!this->shaderBound)
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

		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Format = gpuTexture->GetDesc().Format;
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Texture2D.MipLevels = gpuTexture->GetDesc().MipLevels;
		this->graphicsDevice->CreateShaderResourceView(gpuTexture.Get(), &srvDesc, heapPtr);

		heapPtr.ptr += srvDescriptorHandleSize;

		TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_GENERIC_READ);
	}

	ID3D12DescriptorHeap* descriptorHeaps[] = { srvDescriptorHeap.Get() };
	commandList->SetDescriptorHeaps(1, descriptorHeaps);
	commandList->SetGraphicsRootDescriptorTable(slot, srvDescriptorHeap->GetGPUDescriptorHandleForHeapStart());
}

void Direct3D12GraphicsService::SetShaderIndirectCommandList(unsigned int commandListId, unsigned int indirectCommandListId, int slot, int index){ }
void Direct3D12GraphicsService::SetShaderIndirectCommandLists(unsigned int commandListId, unsigned int* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index){ }

int Direct3D12GraphicsService::CreateCopyCommandList(unsigned int commandListId, unsigned int commandBufferId, char* label)
{
	this->commandListBuffers[commandListId] = commandBufferId;
    return 1;
}

void Direct3D12GraphicsService::CommitCopyCommandList(unsigned int commandListId)
{ 
	this->commandListBuffers.erase(commandListId);

	// TODO: Update Command List Fence value
}

void Direct3D12GraphicsService::UploadDataToGraphicsBuffer(unsigned int commandListId, unsigned int graphicsBufferId, void* data, int dataLength)
{ 
	if (!this->cpuBuffers.count(graphicsBufferId) && !this->gpuBuffers.count(graphicsBufferId))
	{
		return;
	}

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuBuffer = this->gpuBuffers[graphicsBufferId];
	auto cpuBuffer = this->cpuBuffers[graphicsBufferId];

	void* pointer = nullptr;
	D3D12_RANGE range = { 0, 0 };
	cpuBuffer->Map(0, &range, &pointer);

	memcpy(pointer, data, dataLength);

	commandList->CopyResource(gpuBuffer.Get(), cpuBuffer.Get());
}

void Direct3D12GraphicsService::CopyGraphicsBufferDataToCpu(unsigned int commandListId, unsigned int graphicsBufferId, int length){ }
void Direct3D12GraphicsService::ReadGraphicsBufferData(unsigned int graphicsBufferId, void* data, int dataLength){ }

void Direct3D12GraphicsService::UploadDataToTexture(unsigned int commandListId, unsigned int textureId, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, void* data, int dataLength)
{ 
	// TODO: For the moment it only takes into account the mip level
	if (mipLevel > 0)
	{
		return;
	}

	TransitionTextureToState(commandListId, textureId, D3D12_RESOURCE_STATE_COPY_DEST);

	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto gpuTexture = this->gpuTextures[textureId];
	auto cpuTexture = this->cpuTextures[textureId];
	auto footPrint = this->textureFootPrints[textureId];

	void* pointer = nullptr;
	D3D12_RANGE range = { 0, 0 };
	cpuTexture->Map(0, &range, &pointer);

	memcpy(pointer, data, dataLength);

	D3D12_TEXTURE_COPY_LOCATION destinationLocation = {};
	destinationLocation.pResource = gpuTexture.Get();
	destinationLocation.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
	destinationLocation.SubresourceIndex = mipLevel;

	D3D12_TEXTURE_COPY_LOCATION sourceLocation = {};
	sourceLocation.pResource = cpuTexture.Get();
	sourceLocation.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
	sourceLocation.PlacedFootprint = footPrint;

	commandList->CopyTextureRegion(&destinationLocation, 0, 0, 0, &sourceLocation, nullptr);
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
	this->commandListBuffers.erase(commandListId);

	// TODO: Update Command List Fence value
}

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
	
	this->commandListBuffers[commandListId] = commandBufferId;
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];

	if (renderDescriptor.RenderTarget1TextureId.HasValue)
	{
		auto gpuTexture = this->gpuTextures[renderDescriptor.RenderTarget1TextureId.Value];
		auto descriptorHeapHandle = this->textureDescriptorHeaps[renderDescriptor.RenderTarget1TextureId.Value]->GetCPUDescriptorHandleForHeapStart();

		TransitionTextureToState(commandListId, renderDescriptor.RenderTarget1TextureId.Value, D3D12_RESOURCE_STATE_RENDER_TARGET);
		commandList->OMSetRenderTargets(1, &descriptorHeapHandle, false, nullptr);

		if (renderDescriptor.RenderTarget1ClearColor.HasValue)
		{
			float clearColor[4] = { renderDescriptor.RenderTarget1ClearColor.Value.X, renderDescriptor.RenderTarget1ClearColor.Value.Y, renderDescriptor.RenderTarget1ClearColor.Value.Z, renderDescriptor.RenderTarget1ClearColor.Value.W };
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
	this->commandListBuffers.erase(commandListId);

	// TODO: Update Command List Fence value
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

	commandList->SetGraphicsRootSignature(shader.RootSignature.Get());
	this->shaderBound = true;
}

void Direct3D12GraphicsService::ExecuteIndirectCommandBuffer(unsigned int commandListId, unsigned int indirectCommandBufferId, int maxCommandCount){ }

void Direct3D12GraphicsService::SetIndexBuffer(unsigned int commandListId, unsigned int graphicsBufferId)
{ 
	auto commandList = this->commandBuffers[this->commandListBuffers[commandListId]];
	auto indexBuffer = this->gpuBuffers[graphicsBufferId];

	D3D12_INDEX_BUFFER_VIEW indexBufferView = {};
	indexBufferView.BufferLocation = indexBuffer->GetGPUVirtualAddress();
	indexBufferView.SizeInBytes = indexBuffer->GetDesc().Width;
	indexBufferView.Format = DXGI_FORMAT_R32_UINT;

	commandList->IASetIndexBuffer(&indexBufferView);
}

void Direct3D12GraphicsService::DrawIndexedPrimitives(unsigned int commandListId, enum GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
{ 
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

void Direct3D12GraphicsService::WaitForCommandList(unsigned int commandListId, unsigned int commandListToWaitId){ }

void Direct3D12GraphicsService::PresentScreenBuffer(unsigned int commandBufferId)
{ 
	if (!this->commandBuffers.count(commandBufferId))
	{
		return;
	}
	
	auto commandList = this->commandBuffers[commandBufferId];
	commandList->ResourceBarrier(1, &CreateTransitionResourceBarrier(this->backBufferRenderTargets[this->currentBackBufferIndex].Get(), D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT));	
}

// TODO: Do something better
bool GraphicsProcessMessage(const MSG& message)
{
	if (message.message == WM_QUIT)
	{
		return false;
	}

	TranslateMessage(&message);
	DispatchMessageA(&message);

	return true;
}

bool GraphicsProcessPendingMessages()
{
	bool gameRunning = true;
	MSG message;

	// NOTE: The 2 loops are needed only because of RawInput which require that we let the WM_INPUT messages
	// in the windows message queue...
	while (PeekMessageA(&message, nullptr, 0, WM_INPUT - 1, PM_REMOVE))
	{
		gameRunning = GraphicsProcessMessage(message);
	}

	while (PeekMessageA(&message, nullptr, WM_INPUT + 1, 0xFFFFFFFF, PM_REMOVE))
	{
		gameRunning = GraphicsProcessMessage(message);
	}

	return gameRunning;
}

void Direct3D12GraphicsService::WaitForAvailableScreenBuffer()
{ 
	AssertIfFailed(this->swapChain->Present(1, 0));
	WaitForGlobalFence();

	this->currentBackBufferIndex = this->swapChain->GetCurrentBackBufferIndex();

	this->directCommandAllocators[this->currentBackBufferIndex]->Reset();
	this->copyCommandAllocators[this->currentBackBufferIndex]->Reset();
	this->computeCommandAllocators[this->currentBackBufferIndex]->Reset();
}

void Direct3D12GraphicsService::WaitForGlobalFence()
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
			
			while (WaitForSingleObject(this->globalFenceEvent, 0))
			{
				this->gameState->GameRunning = GraphicsProcessPendingMessages();
			}
		}
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

	// Create the direct command queue
	D3D12_COMMAND_QUEUE_DESC directCommandQueueDesc = {};
	directCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	directCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&directCommandQueueDesc, IID_PPV_ARGS(this->directCommandQueue.ReleaseAndGetAddressOf())));
	this->directCommandQueue->SetName(L"DirectCommandQueue");

	// Create the copy command queue
	D3D12_COMMAND_QUEUE_DESC copyCommandQueueDesc = {};
	copyCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	copyCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COPY;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&copyCommandQueueDesc, IID_PPV_ARGS(this->copyCommandQueue.ReleaseAndGetAddressOf())));
	this->copyCommandQueue->SetName(L"CopyCommandQueue");

	// Create the compute command queue
	D3D12_COMMAND_QUEUE_DESC computeCommandQueueDesc = {};
	computeCommandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	computeCommandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_COMPUTE;

	AssertIfFailed(this->graphicsDevice->CreateCommandQueue(&computeCommandQueueDesc, IID_PPV_ARGS(this->computeCommandQueue.ReleaseAndGetAddressOf())));
	this->computeCommandQueue->SetName(L"ComputeCommandQueue");

	for (int i = 0; i < RenderBuffersCount; i++)
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
		WaitForGlobalFence();

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

bool Direct3D12GraphicsService::SwitchScreenMode()
{
	BOOL fullscreenState;
	this->swapChain->GetFullscreenState(&fullscreenState, nullptr);
	AssertIfFailed(this->swapChain->SetFullscreenState(!fullscreenState, nullptr));
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

DXGI_FORMAT Direct3D12GraphicsService::ConvertTextureFormat(GraphicsTextureFormat textureFormat) 
{
	switch (textureFormat)
	{
		case GraphicsTextureFormat::Bgra8UnormSrgb:
			return DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;
	
		case GraphicsTextureFormat::Depth32Float:
			return DXGI_FORMAT_D32_FLOAT;
	
		case GraphicsTextureFormat::Rgba16Float:
			return DXGI_FORMAT_R16G16B16A16_FLOAT;
	
		case GraphicsTextureFormat::R16Float:
			return DXGI_FORMAT_R16_FLOAT;
	
		case GraphicsTextureFormat::BC1Srgb:
			return DXGI_FORMAT_BC1_UNORM_SRGB;

		case GraphicsTextureFormat::BC2Srgb:
			return DXGI_FORMAT_BC2_UNORM_SRGB;

		case GraphicsTextureFormat::BC3Srgb:
			return DXGI_FORMAT_BC3_UNORM_SRGB;

		case GraphicsTextureFormat::BC4:
			return DXGI_FORMAT_BC4_UNORM;

		case GraphicsTextureFormat::BC5:
			return DXGI_FORMAT_BC5_UNORM;

		case GraphicsTextureFormat::BC6:
			return DXGI_FORMAT_BC6H_UF16;

		case GraphicsTextureFormat::BC7Srgb:
			return DXGI_FORMAT_BC7_UNORM_SRGB;

		case GraphicsTextureFormat::Rgba32Float:
			return DXGI_FORMAT_R32G32B32A32_FLOAT;

		case GraphicsTextureFormat::Rgba16Unorm:
			return DXGI_FORMAT_R16G16B16A16_UNORM;
	}
        
	return DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;
}
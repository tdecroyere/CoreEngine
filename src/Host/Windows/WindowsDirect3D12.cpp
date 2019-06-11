#pragma once

#include "WindowsCommon.h"
#include "WindowsDirect3D12.h"

using namespace winrt;
using namespace Windows::UI::Core;

#define IID_PPV_ARGS_WINRT(ppType) __uuidof(ppType), ppType.put_void()

// TODO: To Remove and to replace with the string class
int StringLength(char* string)
{
	int length = 0;

	while (*string++)
	{
		length++;
	}

	return length;
}

D3D12_RESOURCE_BARRIER Direct3D12::CreateTransitionResourceBarrier(ID3D12Resource* resource, D3D12_RESOURCE_STATES stateBefore, D3D12_RESOURCE_STATES stateAfter)
{
	D3D12_RESOURCE_BARRIER resourceBarrier = {};

	resourceBarrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
	resourceBarrier.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
	resourceBarrier.Transition.pResource = resource;
	resourceBarrier.Transition.StateBefore = stateBefore;
	resourceBarrier.Transition.StateAfter = stateAfter;
	resourceBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;

	return resourceBarrier;
}

Direct3D12Texture Direct3D12::Direct3D12CreateTexture(ID3D12Device* device, int width, int height, DXGI_FORMAT format)
{
	Direct3D12Texture texture = {};
	texture.Width = width;
	texture.Height = height;
	texture.Format = format;
	texture.Pitch = width * 4;

	D3D12_RESOURCE_DESC textureDesc = {};
	textureDesc.MipLevels = 1;
	textureDesc.Format = format;
	textureDesc.Width = texture.Width;
	textureDesc.Height = texture.Height;
	textureDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
	textureDesc.DepthOrArraySize = 1;
	textureDesc.SampleDesc.Count = 1;
	textureDesc.SampleDesc.Quality = 0;
	textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;

	D3D12_HEAP_PROPERTIES uploadHeapProperties = {};
	uploadHeapProperties.Type = D3D12_HEAP_TYPE_UPLOAD;
	uploadHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
	uploadHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
	uploadHeapProperties.CreationNodeMask = 1;
	uploadHeapProperties.VisibleNodeMask = 1;

	D3D12_HEAP_PROPERTIES defaultHeapProperties = {};
	defaultHeapProperties.Type = D3D12_HEAP_TYPE_DEFAULT;
	defaultHeapProperties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
	defaultHeapProperties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
	defaultHeapProperties.CreationNodeMask = 1;
	defaultHeapProperties.VisibleNodeMask = 1;

	HRESULT result = device->CreateCommittedResource(&defaultHeapProperties,
		D3D12_HEAP_FLAG_NONE,
		&textureDesc,
		D3D12_RESOURCE_STATE_COPY_DEST,
		nullptr, 
		__uuidof(texture.Resource), texture.Resource.put_void());

	if (FAILED(result))
	{
		// TODO: Handle error
		OutputDebugStringA("ERROR: Texture creation has failed!\n");
	}

	UINT64 uploadBufferSize;
	device->GetCopyableFootprints(&textureDesc, 0, 1, 0, &texture.SubResourceFootPrint, nullptr, nullptr, &uploadBufferSize);

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

	// Create the GPU upload buffer.
	result = device->CreateCommittedResource(
		&uploadHeapProperties,
		D3D12_HEAP_FLAG_NONE,
		&textureResourceDesc,
		D3D12_RESOURCE_STATE_GENERIC_READ,
		nullptr, 
		IID_PPV_ARGS_WINRT(texture.UploadHeap));

	if (FAILED(result))
	{
		// TODO: Handle error
		OutputDebugStringA("ERROR: Texture upload heap creation has failed!\n");
	}

	texture.CopyDestToPixelShaderBarrier = CreateTransitionResourceBarrier(texture.Resource.get(), 
		D3D12_RESOURCE_STATE_COPY_DEST, 
		D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);

	texture.PixelShaderToCopyDestBarrier = CreateTransitionResourceBarrier(texture.Resource.get(), 
		D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE, 
		D3D12_RESOURCE_STATE_COPY_DEST);

	// TODO: Can we keep the map open during the entire lifetime of the application?
	result = texture.UploadHeap->Map(0, NULL, &texture.UploadHeapData);

	if (FAILED(result))
	{
		// TODO: Handle error
		OutputDebugStringA("ERROR: Texture heap map has failed!\n");
	}

	return texture;
}

void Direct3D12::UploadTextureData(ID3D12GraphicsCommandList* commandList, const Direct3D12Texture& texture)
{
	commandList->ResourceBarrier(1, &texture.PixelShaderToCopyDestBarrier);

	D3D12_TEXTURE_COPY_LOCATION Dst = {};
	Dst.pResource = texture.Resource.get();
	Dst.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
	Dst.SubresourceIndex = 0;

	D3D12_TEXTURE_COPY_LOCATION Src = {};
	Src.pResource = texture.UploadHeap.get();
	Src.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
	Src.PlacedFootprint = texture.SubResourceFootPrint;

	commandList->CopyTextureRegion(&Dst, 0, 0, 0, &Src, nullptr);
	commandList->ResourceBarrier(1, &texture.CopyDestToPixelShaderBarrier);
}

D3D12_CPU_DESCRIPTOR_HANDLE Direct3D12::GetCurrentRenderTargetViewHandle()
{
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = {};
	renderTargetViewHandle.ptr = this->RtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + this->CurrentBackBufferIndex * this->RtvDescriptorHandleSize;

	return renderTargetViewHandle;
}

void Direct3D12::Direct32D2EnableDebugLayer()
{
	// If the project is in a debug build, enable debugging via SDK Layers.
	com_ptr<ID3D12Debug> debugController;
	
	D3D12GetDebugInterface(IID_PPV_ARGS_WINRT(debugController));

	if (debugController)
	{
		debugController->EnableDebugLayer();
	}
}

void Direct3D12::Direct32D2WaitForPreviousFrame()
{
	// TODO:
	// WAITING FOR THE FRAME TO COMPLETE BEFORE CONTINUING IS NOT BEST PRACTICE.
	// This is code implemented as such for simplicity. More advanced samples 
	// illustrate how to use fences for efficient resource usage.

	// Signal and increment the fence value
	const UINT64 fence = this->FenceValue;
	this->CommandQueue->Signal(this->Fence.get(), fence);
	this->FenceValue++;

	// Wait until the previous frame is finished
	if (this->Fence->GetCompletedValue() < fence)
	{
		this->Fence->SetEventOnCompletion(fence, this->FenceEvent);
		WaitForSingleObject(this->FenceEvent, INFINITE);
	}

	this->CurrentBackBufferIndex = this->SwapChain->GetCurrentBackBufferIndex();
}

bool Direct3D12::Direct3D12CreateDevice(const CoreWindow& window, int width, int height)
{
#ifdef DEBUG
	Direct32D2EnableDebugLayer();
#endif

	// Get the DXGI factory used to create the swap chain
	com_ptr<IDXGIFactory4> dxgiFactory;
	ReturnIfFailed(CreateDXGIFactory2(0, IID_PPV_ARGS_WINRT(dxgiFactory)));

	// Created Direct3D Device
	HRESULT result = D3D12CreateDevice(nullptr, D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS_WINRT(this->Device));

	if (FAILED(result))
	{
		// If hardware initialization fail, fall back to the WARP driver
		OutputDebugStringA("Direct3D hardware device initialization failed. Falling back to WARP driver.\n");

		com_ptr<IDXGIAdapter> warpAdapter;
		dxgiFactory->EnumWarpAdapter(IID_PPV_ARGS_WINRT(warpAdapter));

		ReturnIfFailed(D3D12CreateDevice(warpAdapter.get(), D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS_WINRT(this->Device)));
	}

	// Create the command queue and command allocator
	D3D12_COMMAND_QUEUE_DESC commandQueueDesc = {};
	commandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	commandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	ReturnIfFailed(this->Device->CreateCommandQueue(&commandQueueDesc, IID_PPV_ARGS_WINRT(this->CommandQueue)));
	ReturnIfFailed(this->Device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS_WINRT(this->CommandAllocator)));
	ReturnIfFailed(this->Device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, this->CommandAllocator.get(), nullptr, IID_PPV_ARGS_WINRT(this->CommandList)));


	// Describe and create the swap chain.
	DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
	swapChainDesc.BufferCount = this->RenderBuffersCount;
	swapChainDesc.Width = width;
	swapChainDesc.Height = height;
	swapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	swapChainDesc.Scaling = DXGI_SCALING_ASPECT_RATIO_STRETCH;
	swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
	swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_IGNORE;
	swapChainDesc.SampleDesc.Count = 1;
	
	ReturnIfFailed(dxgiFactory->CreateSwapChainForCoreWindow(this->CommandQueue.get(), get_unknown(window), &swapChainDesc, nullptr, (IDXGISwapChain1**)this->SwapChain.put()));

	// Describe and create a render target view (RTV) descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
	rtvHeapDesc.NumDescriptors = this->RenderBuffersCount;
	rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	
	ReturnIfFailed(this->Device->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS_WINRT(this->RtvDescriptorHeap)));

	// Describe and create a shader resource view (SRV) heap for the texture
	D3D12_DESCRIPTOR_HEAP_DESC srvHeapDesc = {};
	srvHeapDesc.NumDescriptors = 1;
	srvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	srvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	ReturnIfFailed(this->Device->CreateDescriptorHeap(&srvHeapDesc, IID_PPV_ARGS_WINRT(this->SrvDescriptorHeap)));
	this->RtvDescriptorHandleSize = this->Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

	// Create a fence object used to synchronize the CPU with the GPU
	ReturnIfFailed(this->Device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS_WINRT(this->Fence)));
	this->FenceValue = 1;

	// Create an event handle to use for frame synchronization
	this->FenceEvent = CreateEventA(nullptr, false, false, nullptr);
	
	if (this->FenceEvent == nullptr)
	{
		return false;
	}

	return true;
}

bool Direct3D12::Direct3D12InitSizeDependentResources()
{
	int width = this->Width;
	int height = this->Height;

	for (UINT n = 0; n < this->RenderBuffersCount; n++)
	{
		this->RenderTargets[n] = nullptr;
	}

	// Resize the swap chain to the desired dimensions.
	DXGI_SWAP_CHAIN_DESC desc = {};
	this->SwapChain->GetDesc(&desc);

	ReturnIfFailed(this->SwapChain->ResizeBuffers(RenderBuffersCountConst, width, height, desc.BufferDesc.Format, desc.Flags));

	// Reset the frame index to the current back buffer index.
	this->CurrentBackBufferIndex = this->SwapChain->GetCurrentBackBufferIndex();

	// Create frame resources.
	D3D12_CPU_DESCRIPTOR_HANDLE rtvDecriptorHandle = this->RtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();

	// Change the format of the renderview target so it can be specified in SRGB
	D3D12_RENDER_TARGET_VIEW_DESC rtvDesc = {};
	rtvDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;
	rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;

	// Create a RTV for each frame.
	for (int i = 0; i < this->RenderBuffersCount; ++i)
	{
		ReturnIfFailed(this->SwapChain->GetBuffer(i, IID_PPV_ARGS_WINRT(this->RenderTargets[i])));

		this->Device->CreateRenderTargetView(this->RenderTargets[i].get(), &rtvDesc, rtvDecriptorHandle);
		rtvDecriptorHandle.ptr += this->RtvDescriptorHandleSize;

		this->PresentToRenderTargetBarriers[i] = CreateTransitionResourceBarrier(this->RenderTargets[i].get(), D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET);
		this->RenderTargetToPresentBarriers[i] = CreateTransitionResourceBarrier(this->RenderTargets[i].get(), D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT);
	}

	this->Viewport = {};
	this->Viewport.Width = (float)width;
	this->Viewport.Height = (float)height;
	this->Viewport.MaxDepth = 1.0f;

	this->ScissorRect = {};
	this->ScissorRect.right = (long)width;
	this->ScissorRect.bottom = (long)height;

	return true;
}

bool Direct3D12::Direct3D12CreateSpriteRootSignature()
{
	D3D12_DESCRIPTOR_RANGE ranges[1];
	ranges[0].RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV;
	ranges[0].NumDescriptors = 1;
	ranges[0].BaseShaderRegister = 0;
	ranges[0].RegisterSpace = 0;
	ranges[0].OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND;

	D3D12_ROOT_PARAMETER rootParameters[1];
	rootParameters[0].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
	rootParameters[0].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;
	rootParameters[0].DescriptorTable.NumDescriptorRanges = 1;
	rootParameters[0].DescriptorTable.pDescriptorRanges = &ranges[0];
	
	D3D12_STATIC_SAMPLER_DESC sampler = {};
	sampler.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
	sampler.AddressU = D3D12_TEXTURE_ADDRESS_MODE_BORDER;
	sampler.AddressV = D3D12_TEXTURE_ADDRESS_MODE_BORDER;
	sampler.AddressW = D3D12_TEXTURE_ADDRESS_MODE_BORDER;
	sampler.MipLODBias = 0;
	sampler.MaxAnisotropy = 0;
	sampler.ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER;
	sampler.BorderColor = D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK;
	sampler.MinLOD = 0.0f;
	sampler.MaxLOD = D3D12_FLOAT32_MAX;
	sampler.ShaderRegister = 0;
	sampler.RegisterSpace = 0;
	sampler.ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

	D3D12_ROOT_SIGNATURE_DESC rootSignatureDesc = {};
	rootSignatureDesc.NumParameters = ArrayCount(rootParameters);
	rootSignatureDesc.pParameters = rootParameters;
	rootSignatureDesc.NumStaticSamplers = 1;
	rootSignatureDesc.pStaticSamplers = &sampler;
	rootSignatureDesc.Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT;

	com_ptr<ID3DBlob> signature;
	com_ptr<ID3DBlob> error;

	ReturnIfFailed(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, signature.put(), error.put()));
	ReturnIfFailed((this->Device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS_WINRT(this->SpriteRootSignature))));

	return true;
}

bool Direct3D12::Direct3D12CreateCheckBoardRootSignature()
{
	D3D12_ROOT_PARAMETER rootParameter = {};
	rootParameter.ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS;
	rootParameter.ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;
	rootParameter.Constants.RegisterSpace = 0;
	rootParameter.Constants.ShaderRegister = 0;
	rootParameter.Constants.Num32BitValues = 12;

	D3D12_ROOT_SIGNATURE_DESC rootSignatureDesc = {};
	rootSignatureDesc.NumParameters = 1;
	rootSignatureDesc.pParameters = &rootParameter;
	rootSignatureDesc.NumStaticSamplers = 0;
	rootSignatureDesc.pStaticSamplers = nullptr;
	rootSignatureDesc.Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT;

	com_ptr<ID3DBlob> signature;
	com_ptr<ID3DBlob> error;

	ReturnIfFailed(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, signature.put(), error.put()));
	ReturnIfFailed((this->Device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS_WINRT(this->CheckBoardRootSignature))));

	return true;
}

com_ptr<ID3D12PipelineState> Direct3D12::Direct3D12CreatePipelineState(ID3D12RootSignature* rootSignature, char* shaderCode)
{
	// Create the pipeline state, which includes compiling and loading shaders
	com_ptr<ID3DBlob> vertexShader;
	com_ptr<ID3DBlob> pixelShader;

#if DEBUG
	// Enable better shader debugging with the graphics debugging tools.
	UINT compileFlags = D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION;
#else
	UINT compileFlags = 0;
#endif

	ReturnIfFailed(D3DCompile(shaderCode, StringLength(shaderCode), "ProjectGaia_Internal.hlsl", nullptr, nullptr, "VSMain", "vs_5_0", compileFlags, 0, vertexShader.put(), nullptr));
	ReturnIfFailed(D3DCompile(shaderCode, StringLength(shaderCode), "ProjectGaia_Internal.hlsl", nullptr, nullptr, "PSMain", "ps_5_0", compileFlags, 0, pixelShader.put(), nullptr));

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
	psoDesc.pRootSignature = rootSignature;
	psoDesc.VS = { vertexShader->GetBufferPointer(), vertexShader->GetBufferSize() };
	psoDesc.PS = { pixelShader->GetBufferPointer(), pixelShader->GetBufferSize() };
	psoDesc.SampleMask = 0xFFFFFF;
	psoDesc.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
	psoDesc.NumRenderTargets = 1;
	psoDesc.RTVFormats[0] = DXGI_FORMAT_R8G8B8A8_UNORM;
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
	psoDesc.BlendState.AlphaToCoverageEnable = FALSE;
	psoDesc.BlendState.IndependentBlendEnable = FALSE;

	for (int i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; ++i)
	{
		psoDesc.BlendState.RenderTarget[i] = defaultRenderTargetBlendDesc;
	}

	com_ptr<ID3D12PipelineState> pipelineState;
	ReturnIfFailed(this->Device->CreateGraphicsPipelineState(&psoDesc, IID_PPV_ARGS_WINRT(pipelineState)));

	return pipelineState;
}

bool Direct3D12::Direct3D12CreateSpritePSO()
{
	// TODO: Move the shader code to a separate file
	char* shaderCode = "struct PSInput\
						{\
								float4 position : SV_POSITION;\
							float2 uv : TEXCOORD;\
						};\
						\
						Texture2D g_texture : register(t0);\
						SamplerState g_sampler : register(s0);\
						\
						PSInput VSMain(uint vertexId : SV_VertexID)\
						{\
							PSInput result;\
						\
							if(vertexId == 0)\
							{\
								result.position = float4(-1.0, -1.0f, 0.0f, 1.0f);\
								result.uv = float2(0.0, 1.0);\
							}\
					\
							else if (vertexId == 1)\
							{\
								result.position = float4(-1.0, 1.0f, 0.0f, 1.0f); \
								result.uv = float2(0.0, 0.0); \
							}\
\
							else if (vertexId == 2)\
							{\
								result.position = float4(1.0, -1.0f, 0.0f, 1.0f); \
								result.uv = float2(1.0, 1.0); \
							}\
\
							else if (vertexId == 3)\
							{\
								result.position = float4(1.0, 1.0f, 0.0f, 1.0f); \
								result.uv = float2(1.0, 0.0); \
							}\
							return result;\
						}\
						\
						float4 PSMain(PSInput input) : SV_TARGET\
						{\
							return g_texture.Sample(g_sampler, input.uv);\
						}";

	this->SpritePSO = Direct3D12CreatePipelineState(this->SpriteRootSignature.get(), shaderCode);

	if (this->SpritePSO == nullptr)
	{
		return false;
	}

	return true;
}

bool Direct3D12::Direct3D12CreateCheckBoardPSO()
{
	// TODO: Move the shader code to a separate file
	char* shaderCode = "struct PSInput\
						{\
								float4 position : SV_POSITION;\
								float2 uv : TEXCOORD;\
						};\
						\
						cbuffer RootConstants : register(b0)\
						{\
							float2 Offset;\
							float2 Size;\
							float4 Color1;\
							float4 Color2;\
						}\
						PSInput VSMain(uint vertexId : SV_VertexID)\
						{\
							PSInput result;\
						\
							if(vertexId == 0)\
							{\
								result.position = float4(-1.0, -1.0f, 0.0f, 1.0f);\
								result.uv = float2(0.0, 1.0);\
							}\
					\
							else if (vertexId == 1)\
							{\
								result.position = float4(-1.0, 1.0f, 0.0f, 1.0f); \
								result.uv = float2(0.0, 0.0); \
							}\
\
							else if (vertexId == 2)\
							{\
								result.position = float4(1.0, -1.0f, 0.0f, 1.0f); \
								result.uv = float2(1.0, 1.0); \
							}\
\
							else if (vertexId == 3)\
							{\
								result.position = float4(1.0, 1.0f, 0.0f, 1.0f); \
								result.uv = float2(1.0, 0.0); \
							}\
							return result;\
						}\
						\
						float mod(float x, float y)\
						{\
							return x - y * floor(x / y); \
						}\
						\
						float4 PSMain(PSInput input) : SV_TARGET\
						{\
							float i = floor((input.uv.y * Size.y + Offset.y) / 32);\
							float j = floor((input.uv.x * Size.x + Offset.x) / 32);\
							\
							if(mod(i, 2) == mod(j, 2))\
							{\
								return Color1;\
							}\
							\
							else\
							{\
								return Color2;\
							}\
						}";

	this->CheckBoardPSO = Direct3D12CreatePipelineState(this->CheckBoardRootSignature.get(), shaderCode);

	if (this->CheckBoardPSO == nullptr)
	{
		return false;
	}

	return true;
}

bool Direct3D12::Direct3D12CreateResources()
{
	this->Texture = Direct3D12CreateTexture(this->Device.get(), this->Width, this->Height, DXGI_FORMAT_B8G8R8A8_UNORM);
	
	// Describe and create a SRV for the texture.
	D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
	srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	srvDesc.Format = this->Texture.Format;
	srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
	srvDesc.Texture2D.MipLevels = 1;

	this->Device->CreateShaderResourceView(this->Texture.Resource.get(), &srvDesc, this->SrvDescriptorHeap->GetCPUDescriptorHandleForHeapStart());
	this->CommandList->Close();

	return true;
}

void Direct3D12::Direct3D12Init(const CoreWindow& window, int width, int height, int refreshRate)
{
	this->RenderBuffersCount = RenderBuffersCountConst;
	this->Width = width;
	this->Height = height;
	this->BytesPerPixel = 4;
	this->RefreshRate = refreshRate;
	this->Pitch = this->Width * this->BytesPerPixel;

	bool result = Direct3D12CreateDevice(window, width, height);

	if (!result)
	{
		return;
	}

	result = Direct3D12InitSizeDependentResources();

	if (!result)
	{
		return;
	}

	result = Direct3D12CreateSpriteRootSignature();

	if (!result)
	{
		return;
	}

	result = Direct3D12CreateSpritePSO();

	if (!result)
	{
		return;
	}

	result = Direct3D12CreateCheckBoardRootSignature();

	if (!result)
	{
		return;
	}

	result = Direct3D12CreateCheckBoardPSO();

	if (!result)
	{
		return;
	}

	result = Direct3D12CreateResources();
	
	if (!result)
	{
		return;
	}

	this->IsInitialized = true;
}

void Direct3D12::Direct3D12BeginFrame()
{
	// TODO: Add more log on return codes
	this->CommandAllocator->Reset();
	this->CommandList->Reset(this->CommandAllocator.get(), nullptr);

	this->CommandList->RSSetViewports(1, &this->Viewport);
	this->CommandList->RSSetScissorRects(1, &this->ScissorRect);

	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = GetCurrentRenderTargetViewHandle();

	this->CommandList->ResourceBarrier(1, &this->PresentToRenderTargetBarriers[this->CurrentBackBufferIndex]);
	this->CommandList->OMSetRenderTargets(1, &renderTargetViewHandle, false, nullptr);

	float clearColor[4] = { 0.0f, 0.215f, 1.0f, 0.0f };
	this->CommandList->ClearRenderTargetView(renderTargetViewHandle, clearColor, 0, nullptr);
}

void Direct3D12::Direct3D12EndFrame()
{
	this->CommandList->SetPipelineState(this->SpritePSO.get());
	this->CommandList->SetGraphicsRootSignature(this->SpriteRootSignature.get());

	ID3D12DescriptorHeap* ppHeaps[] = { this->SrvDescriptorHeap.get() };
	this->CommandList->SetDescriptorHeaps(ArrayCount(ppHeaps), ppHeaps);

	this->CommandList->SetGraphicsRootDescriptorTable(0, this->SrvDescriptorHeap->GetGPUDescriptorHandleForHeapStart());

	//UploadTextureData(direct3D12->CommandList.get(), direct3D12->Texture);

	this->CommandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
	this->CommandList->DrawInstanced(4, 1, 0, 0);

	this->CommandList->ResourceBarrier(1, &this->RenderTargetToPresentBarriers[this->CurrentBackBufferIndex]);
	this->CommandList->Close();

	ID3D12CommandList* commandLists[] = { this->CommandList.get() };
	this->CommandQueue->ExecuteCommandLists(1, commandLists);
}

void Direct3D12::Direct3D12PresentScreenBuffer()
{
	// TODO: Take into account the refresh rate passed in init method (and compute the present delay from the real
	// monitor refresh rate)
	int presentInterval = 1;

	if (!this->VSync)
	{
		presentInterval = 0;
	}

	else if (this->RefreshRate == 30 || this->RefreshRate == 29)
	{
		presentInterval = 2;
	}

	this->SwapChain->Present(presentInterval, 0);

	// TODO: Change the way the GPU sync with the CPU
	Direct32D2WaitForPreviousFrame();
}

bool Direct3D12::Direct3D12SwitchScreenMode()
{
	BOOL fullscreenState;
	ReturnIfFailed(this->SwapChain->GetFullscreenState(&fullscreenState, nullptr));

	if (FAILED(this->SwapChain->SetFullscreenState(!fullscreenState, nullptr)))
	{
		// Transitions to fullscreen mode can fail when running apps over
		// terminal services or for some other unexpected reason.  Consider
		// notifying the user in some way when this happens.
		OutputDebugStringA("Fullscreen transition failed");
		return false;
	}

	this->IsFullscreen = !fullscreenState;
	return true;
}

void Direct3D12::Direct3D12Destroy()
{
	// Ensure that the GPU is no longer referencing resources that are about to be
	// cleaned up by the destructor.
	Direct32D2WaitForPreviousFrame();

	// Fullscreen state should always be false before exiting the app.
	this->SwapChain->SetFullscreenState(false, nullptr);

	CloseHandle(this->FenceEvent);
}
#pragma once

#include <dxgi1_4.h>
#include <d3d12.h>
#include <d3dcompiler.h>
#include <wrl\client.h>


// TODO: To Remove and to replace with the string class
internal inline uint32 StringLength(char* string)
{
	uint32 length = 0;

	while (*string++)
	{
		length++;
	}

	return length;
}

#define ReturnIfFailed(expression) if (FAILED(expression)) { OutputDebugStringA("ERROR: DirectX12 Init error!\n"); return false; };

using namespace Microsoft::WRL;

typedef HRESULT CreateDXGIFactory2Func(UINT Flags, REFIID riid, void** ppFactory);
typedef HRESULT D3D12CreateDeviceFunc(IUnknown *pAdapter, D3D_FEATURE_LEVEL MinimumFeatureLevel, REFIID riid, void** ppDevice);
typedef HRESULT D3D12GetDebugInterfaceFunc(REFIID riid, void **ppvDebug);
typedef HRESULT D3D12SerializeRootSignatureFunc(const D3D12_ROOT_SIGNATURE_DESC* pRootSignature, D3D_ROOT_SIGNATURE_VERSION Version, ID3DBlob** ppBlob, ID3DBlob** ppErrorBlob);
typedef HRESULT D3DCompileFunc(LPCVOID pSrcData, SIZE_T SrcDataSize, LPCSTR pSourceName, D3D_SHADER_MACRO* pDefines, ID3DInclude* pInclude, LPCSTR pEntrypoint, LPCSTR pTarget, UINT Flags1, UINT Flags2, ID3DBlob** ppCode, ID3DBlob** ppErrorMsgs);

static const uint32 RenderBuffersCountConst = 2;

struct Direct3D12Texture
{
	uint32 Width;
	uint32 Height;
	uint32 Pitch;
	DXGI_FORMAT Format;
	ComPtr<ID3D12Resource> Resource;
	ComPtr<ID3D12Resource> UploadHeap;
	void* UploadHeapData;
	D3D12_RESOURCE_BARRIER PixelShaderToCopyDestBarrier;
	D3D12_RESOURCE_BARRIER CopyDestToPixelShaderBarrier;
	D3D12_PLACED_SUBRESOURCE_FOOTPRINT SubResourceFootPrint;
};

struct Direct3D12
{
	bool32 IsInitialized;
	bool32 IsFullscreen;
	uint32 Width;
	uint32 Height;
	uint32 BytesPerPixel;
	uint32 Pitch;
	uint32 RefreshRate;
	bool32 VSync;

	HMODULE DXGILibrary;
	HMODULE Direct3D12Library;
	HMODULE Direct3DCompilerLibrary;
	uint32 RenderBuffersCount;
	ComPtr<ID3D12Device> Device;
	ComPtr<ID3D12CommandQueue> CommandQueue;
	ComPtr<ID3D12CommandAllocator> CommandAllocator;
	ComPtr<ID3D12GraphicsCommandList> CommandList;
	ComPtr<IDXGISwapChain3> SwapChain;
	ComPtr<ID3D12DescriptorHeap> RtvDescriptorHeap;
	ComPtr<ID3D12DescriptorHeap> SrvDescriptorHeap;
	uint32 RtvDescriptorHandleSize;
	ComPtr<ID3D12Resource> RenderTargets[RenderBuffersCountConst];
	D3D12_RESOURCE_BARRIER PresentToRenderTargetBarriers[RenderBuffersCountConst];
	D3D12_RESOURCE_BARRIER RenderTargetToPresentBarriers[RenderBuffersCountConst];
	ComPtr<ID3D12PipelineState> SpritePSO;
	ComPtr<ID3D12RootSignature> SpriteRootSignature;
	ComPtr<ID3D12PipelineState> CheckBoardPSO;
	ComPtr<ID3D12RootSignature> CheckBoardRootSignature;

	Direct3D12Texture Texture;

	D3D12_VIEWPORT Viewport;
	D3D12_RECT ScissorRect;

	// Synchronization objects
	uint32 CurrentBackBufferIndex;
	HANDLE FenceEvent;
	ComPtr<ID3D12Fence> Fence;
	uint64 FenceValue;
};


inline D3D12_RESOURCE_BARRIER CreateTransitionResourceBarrier(ID3D12Resource* resource, D3D12_RESOURCE_STATES stateBefore, D3D12_RESOURCE_STATES stateAfter)
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

internal Direct3D12Texture Direct3D12CreateTexture(ID3D12Device* device, uint32 width, uint32 height, DXGI_FORMAT format)
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
		IID_PPV_ARGS(&texture.Resource));

	if (FAILED(result))
	{
		// TODO: Handle error
		OutputDebugStringA("ERROR: Texture creation has failed!\n");
	}

	uint64 uploadBufferSize;
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
		IID_PPV_ARGS(&texture.UploadHeap));

	if (FAILED(result))
	{
		// TODO: Handle error
		OutputDebugStringA("ERROR: Texture upload heap creation has failed!\n");
	}

	texture.CopyDestToPixelShaderBarrier = CreateTransitionResourceBarrier(texture.Resource.Get(), 
		D3D12_RESOURCE_STATE_COPY_DEST, 
		D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);

	texture.PixelShaderToCopyDestBarrier = CreateTransitionResourceBarrier(texture.Resource.Get(), 
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

internal void UploadTextureData(ID3D12GraphicsCommandList* commandList, const Direct3D12Texture& texture)
{
	commandList->ResourceBarrier(1, &texture.PixelShaderToCopyDestBarrier);

	D3D12_TEXTURE_COPY_LOCATION Dst = {};
	Dst.pResource = texture.Resource.Get();
	Dst.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
	Dst.SubresourceIndex = 0;

	D3D12_TEXTURE_COPY_LOCATION Src = {};
	Src.pResource = texture.UploadHeap.Get();
	Src.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
	Src.PlacedFootprint = texture.SubResourceFootPrint;

	commandList->CopyTextureRegion(&Dst, 0, 0, 0, &Src, nullptr);
	commandList->ResourceBarrier(1, &texture.CopyDestToPixelShaderBarrier);
}

internal D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetViewHandle(Direct3D12* direct3D12)
{
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = {};
	renderTargetViewHandle.ptr = direct3D12->RtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart().ptr + direct3D12->CurrentBackBufferIndex * direct3D12->RtvDescriptorHandleSize;

	return renderTargetViewHandle;
}

internal void Direct32D2EnableDebugLayer(HMODULE direct3D12Library)
{
	D3D12GetDebugInterfaceFunc* D3D12GetDebugInterface = (D3D12GetDebugInterfaceFunc*)GetProcAddress(direct3D12Library, "D3D12GetDebugInterface");

	if (D3D12GetDebugInterface)
	{
		// If the project is in a debug build, enable debugging via SDK Layers.
		ComPtr<ID3D12Debug> debugController;
		D3D12GetDebugInterface(IID_PPV_ARGS(&debugController));

		if (debugController)
		{
			debugController->EnableDebugLayer();
		}
	}

	else
	{
		// TODO: Implement diagnostics
		OutputDebugStringA("ERROR: D3D12GetDebugInterface function was not found!\n");
	}
}

internal void Direct32D2WaitForPreviousFrame(Direct3D12* direct3D12)
{
	// TODO:
	// WAITING FOR THE FRAME TO COMPLETE BEFORE CONTINUING IS NOT BEST PRACTICE.
	// This is code implemented as such for simplicity. More advanced samples 
	// illustrate how to use fences for efficient resource usage.

	// Signal and increment the fence value
	const uint64 fence = direct3D12->FenceValue;
	direct3D12->CommandQueue->Signal(direct3D12->Fence.Get(), fence);
	direct3D12->FenceValue++;

	// Wait until the previous frame is finished
	if (direct3D12->Fence->GetCompletedValue() < fence)
	{
		direct3D12->Fence->SetEventOnCompletion(fence, direct3D12->FenceEvent);
		WaitForSingleObject(direct3D12->FenceEvent, INFINITE);
	}

	direct3D12->CurrentBackBufferIndex = direct3D12->SwapChain->GetCurrentBackBufferIndex();
}


internal bool32 Direct3D12CreateDevice(Direct3D12* direct3D12, HWND window, uint32 width, uint32 height)
{
	CreateDXGIFactory2Func* CreateDXGIFactory2 = (CreateDXGIFactory2Func*)GetProcAddress(direct3D12->DXGILibrary, "CreateDXGIFactory2");
	D3D12CreateDeviceFunc* D3D12CreateDevice = (D3D12CreateDeviceFunc*)GetProcAddress(direct3D12->Direct3D12Library, "D3D12CreateDevice");

#ifdef PROJECTGAIA_INTERNAL
	Direct32D2EnableDebugLayer(direct3D12->Direct3D12Library);
#endif

	// Get the DXGI factory used to create the swap chain
	ComPtr<IDXGIFactory4> dxgiFactory;
	ReturnIfFailed(CreateDXGIFactory2(0, IID_PPV_ARGS(&dxgiFactory)));

	// Created Direct3D Device
	HRESULT result = D3D12CreateDevice(nullptr, D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&direct3D12->Device));

	if (FAILED(result))
	{
		// If hardware initialization fail, fall back to the WARP driver
		OutputDebugStringA("Direct3D hardware device initialization failed. Falling back to WARP driver.\n");

		ComPtr<IDXGIAdapter> warpAdapter;
		dxgiFactory->EnumWarpAdapter(IID_PPV_ARGS(&warpAdapter));

		ReturnIfFailed(D3D12CreateDevice(warpAdapter.Get(), D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&direct3D12->Device)));
	}

	// Create the command queue and command allocator
	D3D12_COMMAND_QUEUE_DESC commandQueueDesc = {};
	commandQueueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	commandQueueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	ReturnIfFailed(direct3D12->Device->CreateCommandQueue(&commandQueueDesc, IID_PPV_ARGS(&direct3D12->CommandQueue)));
	ReturnIfFailed(direct3D12->Device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&direct3D12->CommandAllocator)));
	ReturnIfFailed(direct3D12->Device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, direct3D12->CommandAllocator.Get(), nullptr, IID_PPV_ARGS(&direct3D12->CommandList)));


	// Describe and create the swap chain.
	DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
	swapChainDesc.BufferCount = direct3D12->RenderBuffersCount;
	swapChainDesc.Width = width;
	swapChainDesc.Height = height;
	swapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	//swapChainDesc.Scaling = DXGI_SCALING_NONE;
	swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
	swapChainDesc.SampleDesc.Count = 1;
	
	ReturnIfFailed(dxgiFactory->CreateSwapChainForHwnd(direct3D12->CommandQueue.Get(), window, &swapChainDesc, nullptr, nullptr, (IDXGISwapChain1**)direct3D12->SwapChain.GetAddressOf()));

	// Describe and create a render target view (RTV) descriptor heap
	D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
	rtvHeapDesc.NumDescriptors = direct3D12->RenderBuffersCount;
	rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	
	ReturnIfFailed(direct3D12->Device->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS(&direct3D12->RtvDescriptorHeap)));

	// Describe and create a shader resource view (SRV) heap for the texture
	D3D12_DESCRIPTOR_HEAP_DESC srvHeapDesc = {};
	srvHeapDesc.NumDescriptors = 1;
	srvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	srvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;

	ReturnIfFailed(direct3D12->Device->CreateDescriptorHeap(&srvHeapDesc, IID_PPV_ARGS(&direct3D12->SrvDescriptorHeap)));
	direct3D12->RtvDescriptorHandleSize = direct3D12->Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

	// Create a fence object used to synchronize the CPU with the GPU
	ReturnIfFailed(direct3D12->Device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&direct3D12->Fence)));
	direct3D12->FenceValue = 1;

	// Create an event handle to use for frame synchronization
	direct3D12->FenceEvent = CreateEventA(nullptr, false, false, nullptr);
	
	if (direct3D12->FenceEvent == nullptr)
	{
		return false;
	}

	return true;
}

internal bool32 Direct3D12InitSizeDependentResources(Direct3D12* direct3D12)
{
	uint32 width = direct3D12->Width;
	uint32 height = direct3D12->Height;

	for (UINT n = 0; n < direct3D12->RenderBuffersCount; n++)
	{
		direct3D12->RenderTargets[n].Reset();
	}

	// Resize the swap chain to the desired dimensions.
	DXGI_SWAP_CHAIN_DESC desc = {};
	direct3D12->SwapChain->GetDesc(&desc);

	ReturnIfFailed(direct3D12->SwapChain->ResizeBuffers(RenderBuffersCountConst, width, height, desc.BufferDesc.Format, desc.Flags));

	// Reset the frame index to the current back buffer index.
	direct3D12->CurrentBackBufferIndex = direct3D12->SwapChain->GetCurrentBackBufferIndex();

	// Create frame resources.
	D3D12_CPU_DESCRIPTOR_HANDLE rtvDecriptorHandle = direct3D12->RtvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();

	// Create a RTV for each frame.
	for (uint32 i = 0; i < direct3D12->RenderBuffersCount; ++i)
	{
		ReturnIfFailed(direct3D12->SwapChain->GetBuffer(i, IID_PPV_ARGS(&direct3D12->RenderTargets[i])));

		direct3D12->Device->CreateRenderTargetView(direct3D12->RenderTargets[i].Get(), nullptr, rtvDecriptorHandle);
		rtvDecriptorHandle.ptr += direct3D12->RtvDescriptorHandleSize;

		direct3D12->PresentToRenderTargetBarriers[i] = CreateTransitionResourceBarrier(direct3D12->RenderTargets[i].Get(), D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET);
		direct3D12->RenderTargetToPresentBarriers[i] = CreateTransitionResourceBarrier(direct3D12->RenderTargets[i].Get(), D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT);
	}

	direct3D12->Viewport = {};
	direct3D12->Viewport.Width = (real32)width;
	direct3D12->Viewport.Height = (real32)height;
	direct3D12->Viewport.MaxDepth = 1.0f;

	direct3D12->ScissorRect = {};
	direct3D12->ScissorRect.right = (uint64)width;
	direct3D12->ScissorRect.bottom = (uint64)height;

	return true;
}

internal bool32 Direct3D12CreateSpriteRootSignature(Direct3D12* direct3D12)
{
	D3D12SerializeRootSignatureFunc* D3D12SerializeRootSignature = (D3D12SerializeRootSignatureFunc*)GetProcAddress(direct3D12->Direct3D12Library, "D3D12SerializeRootSignature");

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

	ComPtr<ID3DBlob> signature;
	ComPtr<ID3DBlob> error;

	ReturnIfFailed(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	ReturnIfFailed((direct3D12->Device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&direct3D12->SpriteRootSignature))));

	return true;
}

internal bool32 Direct3D12CreateCheckBoardRootSignature(Direct3D12* direct3D12)
{
	D3D12SerializeRootSignatureFunc* D3D12SerializeRootSignature = (D3D12SerializeRootSignatureFunc*)GetProcAddress(direct3D12->Direct3D12Library, "D3D12SerializeRootSignature");

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

	ComPtr<ID3DBlob> signature;
	ComPtr<ID3DBlob> error;

	ReturnIfFailed(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	ReturnIfFailed((direct3D12->Device->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&direct3D12->CheckBoardRootSignature))));

	return true;
}

internal ComPtr<ID3D12PipelineState> Direct3D12CreatePipelineState(Direct3D12* direct3D12, ID3D12RootSignature* rootSignature, char* shaderCode)
{
	D3DCompileFunc* D3DCompile = (D3DCompileFunc*)GetProcAddress(direct3D12->Direct3DCompilerLibrary, "D3DCompile");
	
	// Create the pipeline state, which includes compiling and loading shaders
	ComPtr<ID3DBlob> vertexShader;
	ComPtr<ID3DBlob> pixelShader;

#if PROJECTGAIA_SLOW
	// Enable better shader debugging with the graphics debugging tools.
	UINT compileFlags = D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION;
#else
	UINT compileFlags = 0;
#endif

	ReturnIfFailed(D3DCompile(shaderCode, StringLength(shaderCode), "ProjectGaia_Internal.hlsl", nullptr, nullptr, "VSMain", "vs_5_0", compileFlags, 0, &vertexShader, nullptr));
	ReturnIfFailed(D3DCompile(shaderCode, StringLength(shaderCode), "ProjectGaia_Internal.hlsl", nullptr, nullptr, "PSMain", "ps_5_0", compileFlags, 0, &pixelShader, nullptr));

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
	psoDesc.VS = { (uint8*)(vertexShader->GetBufferPointer()), vertexShader->GetBufferSize() };
	psoDesc.PS = { (uint8*)(pixelShader->GetBufferPointer()), pixelShader->GetBufferSize() };
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

	for (uint32 i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; ++i)
	{
		psoDesc.BlendState.RenderTarget[i] = defaultRenderTargetBlendDesc;
	}

	ComPtr<ID3D12PipelineState> pipelineState;
	ReturnIfFailed(direct3D12->Device->CreateGraphicsPipelineState(&psoDesc, IID_PPV_ARGS(&pipelineState)));

	return pipelineState;
}

internal bool32 Direct3D12CreateSpritePSO(Direct3D12* direct3D12)
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

	direct3D12->SpritePSO = Direct3D12CreatePipelineState(direct3D12, direct3D12->SpriteRootSignature.Get(), shaderCode);

	if (direct3D12->SpritePSO == nullptr)
	{
		return false;
	}

	return true;
}

internal bool32 Direct3D12CreateCheckBoardPSO(Direct3D12* direct3D12)
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

	direct3D12->CheckBoardPSO = Direct3D12CreatePipelineState(direct3D12, direct3D12->CheckBoardRootSignature.Get(), shaderCode);

	if (direct3D12->CheckBoardPSO == nullptr)
	{
		return false;
	}

	return true;
}

internal bool32 Direct3D12CreateResources(Direct3D12* direct3D12)
{
	direct3D12->Texture = Direct3D12CreateTexture(direct3D12->Device.Get(), direct3D12->Width, direct3D12->Height, DXGI_FORMAT_B8G8R8A8_UNORM);
	
	// Describe and create a SRV for the texture.
	D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
	srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	srvDesc.Format = direct3D12->Texture.Format;
	srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
	srvDesc.Texture2D.MipLevels = 1;

	direct3D12->Device->CreateShaderResourceView(direct3D12->Texture.Resource.Get(), &srvDesc, direct3D12->SrvDescriptorHeap->GetCPUDescriptorHandleForHeapStart());
	direct3D12->CommandList->Close();

	return true;
}

internal Direct3D12 Direct3D12Init(HWND window, uint32 width, uint32 height, uint32 refreshRate)
{
	Direct3D12 direct3D12 = {};
	direct3D12.RenderBuffersCount = RenderBuffersCountConst;
	direct3D12.Width = width;
	direct3D12.Height = height;
	direct3D12.BytesPerPixel = 4;
	direct3D12.RefreshRate = refreshRate;
	direct3D12.Pitch = direct3D12.Width * direct3D12.BytesPerPixel;

	direct3D12.DXGILibrary = LoadLibraryA("Dxgi.dll");
	direct3D12.Direct3D12Library = LoadLibraryA("D3D12.dll");

	// TODO: Handle all other versions of D3DCompile_XXX.dll
	direct3D12.Direct3DCompilerLibrary = LoadLibraryA("D3DCompiler_47.dll");

	if (!direct3D12.DXGILibrary || !direct3D12.Direct3D12Library || !direct3D12.Direct3DCompilerLibrary)
	{
		// TODO: Implement diagnostics
		OutputDebugStringA("ERROR: DXGI or D3D12 Library or D3DCompiler could not be loaded!\n");
		return direct3D12;
	}

	bool32 result = Direct3D12CreateDevice(&direct3D12, window, width, height);

	if (!result)
	{
		return direct3D12;
	}

	result = Direct3D12InitSizeDependentResources(&direct3D12);

	if (!result)
	{
		return direct3D12;
	}

	result = Direct3D12CreateSpriteRootSignature(&direct3D12);

	if (!result)
	{
		return direct3D12;
	}

	result = Direct3D12CreateSpritePSO(&direct3D12);

	if (!result)
	{
		return direct3D12;
	}

	result = Direct3D12CreateCheckBoardRootSignature(&direct3D12);

	if (!result)
	{
		return direct3D12;
	}

	result = Direct3D12CreateCheckBoardPSO(&direct3D12);

	if (!result)
	{
		return direct3D12;
	}

	result = Direct3D12CreateResources(&direct3D12);
	
	if (!result)
	{
		return direct3D12;
	}

	direct3D12.IsInitialized = true;
	return direct3D12;
}

internal void Direct3D12BeginFrame(Direct3D12* direct3D12)
{
	// TODO: Add more log on return codes
	direct3D12->CommandAllocator->Reset();
	direct3D12->CommandList->Reset(direct3D12->CommandAllocator.Get(), nullptr);

	direct3D12->CommandList->RSSetViewports(1, &direct3D12->Viewport);
	direct3D12->CommandList->RSSetScissorRects(1, &direct3D12->ScissorRect);

	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetViewHandle = GetCurrentRenderTargetViewHandle(direct3D12);

	direct3D12->CommandList->ResourceBarrier(1, &direct3D12->PresentToRenderTargetBarriers[direct3D12->CurrentBackBufferIndex]);
	direct3D12->CommandList->OMSetRenderTargets(1, &renderTargetViewHandle, false, nullptr);

	real32 clearColor[4] = { 1.0f, 0.0f, 0.0f, 0.0f };
	direct3D12->CommandList->ClearRenderTargetView(renderTargetViewHandle, clearColor, 0, nullptr);
}

internal void Direct3D12EndFrame(Direct3D12* direct3D12)
{
	direct3D12->CommandList->SetPipelineState(direct3D12->SpritePSO.Get());
	direct3D12->CommandList->SetGraphicsRootSignature(direct3D12->SpriteRootSignature.Get());

	ID3D12DescriptorHeap* ppHeaps[] = { direct3D12->SrvDescriptorHeap.Get() };
	direct3D12->CommandList->SetDescriptorHeaps(ArrayCount(ppHeaps), ppHeaps);

	direct3D12->CommandList->SetGraphicsRootDescriptorTable(0, direct3D12->SrvDescriptorHeap->GetGPUDescriptorHandleForHeapStart());

	UploadTextureData(direct3D12->CommandList.Get(), direct3D12->Texture);

	direct3D12->CommandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
	direct3D12->CommandList->DrawInstanced(4, 1, 0, 0);

	direct3D12->CommandList->ResourceBarrier(1, &direct3D12->RenderTargetToPresentBarriers[direct3D12->CurrentBackBufferIndex]);
	direct3D12->CommandList->Close();

	ID3D12CommandList* commandLists[] = { direct3D12->CommandList.Get() };
	direct3D12->CommandQueue->ExecuteCommandLists(1, commandLists);
}

internal void Direct3D12PresentScreenBuffer(Direct3D12* direct3D12)
{
	// TODO: Take into account the refresh rate passed in init method (and compute the present delay from the real
	// monitor refresh rate)
	uint32 presentInterval = 1;

	if (!direct3D12->VSync)
	{
		presentInterval = 0;
	}

	else if (direct3D12->RefreshRate == 30 || direct3D12->RefreshRate == 29)
	{
		presentInterval = 2;
	}

	direct3D12->SwapChain->Present(presentInterval, 0);

	// TODO: Change the way the GPU sync with the CPU
	Direct32D2WaitForPreviousFrame(direct3D12);
}

internal bool32 Direct3D12SwitchScreenMode(Direct3D12* direct3D12)
{
	BOOL fullscreenState;
	ReturnIfFailed(direct3D12->SwapChain->GetFullscreenState(&fullscreenState, nullptr));

	if (FAILED(direct3D12->SwapChain->SetFullscreenState(!fullscreenState, nullptr)))
	{
		// Transitions to fullscreen mode can fail when running apps over
		// terminal services or for some other unexpected reason.  Consider
		// notifying the user in some way when this happens.
		OutputDebugStringA("Fullscreen transition failed");
		return false;
	}

	direct3D12->IsFullscreen = !fullscreenState;
	return true;
}

internal void Direct3D12Destroy(Direct3D12* direct3D12)
{
	// Ensure that the GPU is no longer referencing resources that are about to be
	// cleaned up by the destructor.
	Direct32D2WaitForPreviousFrame(direct3D12);

	// Fullscreen state should always be false before exiting the app.
	direct3D12->SwapChain->SetFullscreenState(false, nullptr);

	CloseHandle(direct3D12->FenceEvent);
}
#pragma once
#include "WindowsCommon.h"

#define PsoSubObject(name, subObjectType, subObject) 	struct alignas(void*) Def##name \
						{ \
						private: \
							D3D12_PIPELINE_STATE_SUBOBJECT_TYPE type; \
							subObject innerObject; \
 \
						public: \
							Def##name() noexcept : type(subObjectType), innerObject()  \
							{ \
							} \
 \
							Def##name(subObject const& i) : type(subObjectType), innerObject(i) {} \
							Def##name& operator=(subObject const& i) { innerObject = i; return *this; } \
							operator subObject() const { return innerObject; } \
							operator subObject() { return innerObject; } \
						} name; 

struct GraphicsPsoLegacy
{
public:
	PsoSubObject(RootSignature, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE, ID3D12RootSignature*);
	PsoSubObject(VS, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VS, D3D12_SHADER_BYTECODE);
	PsoSubObject(PS, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PS, D3D12_SHADER_BYTECODE);
	PsoSubObject(PrimitiveTopologyType, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY, D3D12_PRIMITIVE_TOPOLOGY_TYPE);
	PsoSubObject(RenderTargets, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS, D3D12_RT_FORMAT_ARRAY);
	PsoSubObject(SampleDesc, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC, DXGI_SAMPLE_DESC);
	PsoSubObject(RasterizerState, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER, D3D12_RASTERIZER_DESC);
	PsoSubObject(DepthStencilState, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL, D3D12_DEPTH_STENCIL_DESC);
	PsoSubObject(BlendState, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND, D3D12_BLEND_DESC);
};

struct GraphicsPso
{
public:
	PsoSubObject(RootSignature, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE, ID3D12RootSignature*);
	PsoSubObject(AS, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_AS, D3D12_SHADER_BYTECODE);
	PsoSubObject(MS, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_MS, D3D12_SHADER_BYTECODE);
	PsoSubObject(PS, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PS, D3D12_SHADER_BYTECODE);
	PsoSubObject(PrimitiveTopologyType, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY, D3D12_PRIMITIVE_TOPOLOGY_TYPE);
	PsoSubObject(RenderTargets, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS, D3D12_RT_FORMAT_ARRAY);
	PsoSubObject(SampleDesc, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC, DXGI_SAMPLE_DESC);
	PsoSubObject(RasterizerState, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER, D3D12_RASTERIZER_DESC);
	PsoSubObject(DepthStencilFormat, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL_FORMAT, DXGI_FORMAT);
	PsoSubObject(DepthStencilState, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL, D3D12_DEPTH_STENCIL_DESC);
	PsoSubObject(BlendState, D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND, D3D12_BLEND_DESC);
};

ComPtr<ID3DBlob> CreateShaderBlob(void* data, int dataLength)
{
    ComPtr<ID3DBlob> shaderBlob;
    D3DCreateBlob(dataLength, shaderBlob.ReleaseAndGetAddressOf());

    auto shaderByteCode = shaderBlob->GetBufferPointer();
    memcpy(shaderByteCode, data, dataLength);

    return shaderBlob;
}

DXGI_FORMAT ConvertTextureFormat(GraphicsTextureFormat textureFormat, bool noSrgb = false) 
{
	switch (textureFormat)
	{
		case GraphicsTextureFormat::Bgra8UnormSrgb:
			return noSrgb ? DXGI_FORMAT_B8G8R8A8_UNORM : DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;
	
		case GraphicsTextureFormat::Depth32Float:
			return DXGI_FORMAT_D32_FLOAT;

		case GraphicsTextureFormat::R32Float:
			return DXGI_FORMAT_R32_FLOAT;

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

DXGI_FORMAT ConvertSRVTextureFormat(DXGI_FORMAT textureFormat) 
{
	switch (textureFormat)
	{
		case DXGI_FORMAT_D32_FLOAT:
			return DXGI_FORMAT_R32_FLOAT;
	}
        
	return textureFormat;
}

D3D12_RESOURCE_DESC CreateTextureResourceDescription(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
{
	D3D12_RESOURCE_DESC textureDesc = {};
	textureDesc.MipLevels = mipLevels;
	textureDesc.Format = ConvertTextureFormat(textureFormat);
	textureDesc.Width = width;
	textureDesc.Height = height;
	textureDesc.DepthOrArraySize = 1;
	textureDesc.SampleDesc.Count = multisampleCount;
	textureDesc.SampleDesc.Quality = 0;
	textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
	textureDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
	textureDesc.Flags = D3D12_RESOURCE_FLAG_NONE;

	if (usage == GraphicsTextureUsage::RenderTarget && textureFormat == GraphicsTextureFormat::Depth32Float)
	{
		textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
	}

	else if (usage == GraphicsTextureUsage::RenderTarget) 
	{
		textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
	}

	else if (usage == GraphicsTextureUsage::ShaderWrite) 
	{
		textureDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
	}

	return textureDesc;
}

D3D12_RESOURCE_BARRIER CreateTransitionResourceBarrier(ID3D12Resource* resource, D3D12_RESOURCE_STATES stateBefore, D3D12_RESOURCE_STATES stateAfter)
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

D3D12_RENDER_TARGET_BLEND_DESC InitBlendState(GraphicsBlendOperation blendOperation)
{
	switch (blendOperation)
	{
		case GraphicsBlendOperation::AlphaBlending:
			return {
				true,
				false,
				D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA, D3D12_BLEND_OP_ADD,
				D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA, D3D12_BLEND_OP_ADD,
				D3D12_LOGIC_OP_NOOP,
				D3D12_COLOR_WRITE_ENABLE_ALL,
			};

		default:
			return {
				false,
				false,
				D3D12_BLEND_ONE, D3D12_BLEND_ZERO, D3D12_BLEND_OP_ADD,
				D3D12_BLEND_ONE, D3D12_BLEND_ZERO, D3D12_BLEND_OP_ADD,
				D3D12_LOGIC_OP_NOOP,
				D3D12_COLOR_WRITE_ENABLE_ALL,
			};
	}
}
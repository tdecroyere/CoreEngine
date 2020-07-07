#pragma once
#include "WindowsCommon.h"

// using namespace winrt;

ComPtr<ID3DBlob> CreateShaderBlob(void* data, int dataLength)
{
    ComPtr<ID3DBlob> shaderBlob;
    D3DCreateBlob(dataLength, shaderBlob.ReleaseAndGetAddressOf());

    auto shaderByteCode = shaderBlob->GetBufferPointer();
    memcpy(shaderByteCode, data, dataLength);

    return shaderBlob;
}

D3D12_RESOURCE_BARRIER CreateTransitionResourceBarrier(ID3D12Resource* resource, D3D12_RESOURCE_STATES stateBefore, D3D12_RESOURCE_STATES stateAfter, bool isUAV)
{
	D3D12_RESOURCE_BARRIER resourceBarrier = {};

	resourceBarrier.Type = isUAV ? D3D12_RESOURCE_BARRIER_TYPE_UAV : D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
	resourceBarrier.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;

	if (isUAV)
	{
		resourceBarrier.UAV.pResource = resource;
	}

	else
	{
		resourceBarrier.Transition.pResource = resource;
		resourceBarrier.Transition.StateBefore = stateBefore;
		resourceBarrier.Transition.StateAfter = stateAfter;
		resourceBarrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
	}

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
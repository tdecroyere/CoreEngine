#pragma once
#include "WindowsCommon.h"

// using namespace winrt;

// com_ptr<ID3DBlob> CreateShaderBlob(void* data, int dataLength)
// {
//     com_ptr<ID3DBlob> shaderBlob;
//     D3DCreateBlob(dataLength, shaderBlob.put());

//     auto shaderByteCode = shaderBlob->GetBufferPointer();
//     memcpy(shaderByteCode, data, dataLength);

//     return shaderBlob;
// }

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
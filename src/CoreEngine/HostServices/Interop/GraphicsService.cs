using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe struct GraphicsService : IGraphicsService
    {
        private IntPtr context { get; }

        private delegate* unmanaged[Cdecl]<IntPtr, byte*, void> graphicsService_GetGraphicsAdapterNameDelegate { get; }
        public unsafe string GetGraphicsAdapterName()
        {
            var output = ArrayPool<byte>.Shared.Rent(255);
            if (this.graphicsService_GetGraphicsAdapterNameDelegate != null)
            {
                fixed (byte* outputPinned = output)
this.graphicsService_GetGraphicsAdapterNameDelegate(this.context, outputPinned);
                    var result = System.Text.Encoding.UTF8.GetString(output).TrimEnd(' ');
                    ArrayPool<byte>.Shared.Return(output);
                    return result;
                }

            return string.Empty;
        }

        private delegate* unmanaged[Cdecl]<IntPtr, GraphicsTextureFormat, GraphicsTextureUsage, int, int, int, int, int, GraphicsAllocationInfos> graphicsService_GetTextureAllocationInfosDelegate { get; }
        public unsafe GraphicsAllocationInfos GetTextureAllocationInfos(GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
        {
            if (this.graphicsService_GetTextureAllocationInfosDelegate != null)
            {
                return this.graphicsService_GetTextureAllocationInfosDelegate(this.context, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
            }

            return default(GraphicsAllocationInfos);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, GraphicsServiceCommandType, IntPtr> graphicsService_CreateCommandQueueDelegate { get; }
        public unsafe IntPtr CreateCommandQueue(GraphicsServiceCommandType commandQueueType)
        {
            if (this.graphicsService_CreateCommandQueueDelegate != null)
            {
                return this.graphicsService_CreateCommandQueueDelegate(this.context, commandQueueType);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetCommandQueueLabelDelegate { get; }
        public unsafe void SetCommandQueueLabel(IntPtr commandQueuePointer, string label)
        {
            if (this.graphicsService_SetCommandQueueLabelDelegate != null)
            {
                this.graphicsService_SetCommandQueueLabelDelegate(this.context, commandQueuePointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteCommandQueueDelegate { get; }
        public unsafe void DeleteCommandQueue(IntPtr commandQueuePointer)
        {
            if (this.graphicsService_DeleteCommandQueueDelegate != null)
            {
                this.graphicsService_DeleteCommandQueueDelegate(this.context, commandQueuePointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_ResetCommandQueueDelegate { get; }
        public unsafe void ResetCommandQueue(IntPtr commandQueuePointer)
        {
            if (this.graphicsService_ResetCommandQueueDelegate != null)
            {
                this.graphicsService_ResetCommandQueueDelegate(this.context, commandQueuePointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, ulong> graphicsService_GetCommandQueueTimestampFrequencyDelegate { get; }
        public unsafe ulong GetCommandQueueTimestampFrequency(IntPtr commandQueuePointer)
        {
            if (this.graphicsService_GetCommandQueueTimestampFrequencyDelegate != null)
            {
                return this.graphicsService_GetCommandQueueTimestampFrequencyDelegate(this.context, commandQueuePointer);
            }

            return default(ulong);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr*, int, bool, ulong> graphicsService_ExecuteCommandListsDelegate { get; }
        public unsafe ulong ExecuteCommandLists(IntPtr commandQueuePointer, ReadOnlySpan<IntPtr> commandLists, bool isAwaitable)
        {
            if (this.graphicsService_ExecuteCommandListsDelegate != null)
            {
                fixed (IntPtr* commandListsPinned = commandLists)
                    return this.graphicsService_ExecuteCommandListsDelegate(this.context, commandQueuePointer, commandListsPinned, commandLists.Length, isAwaitable);
                }

            return default(ulong);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, ulong, void> graphicsService_WaitForCommandQueueDelegate { get; }
        public unsafe void WaitForCommandQueue(IntPtr commandQueuePointer, IntPtr commandQueueToWaitPointer, ulong fenceValue)
        {
            if (this.graphicsService_WaitForCommandQueueDelegate != null)
            {
                this.graphicsService_WaitForCommandQueueDelegate(this.context, commandQueuePointer, commandQueueToWaitPointer, fenceValue);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, ulong, void> graphicsService_WaitForCommandQueueOnCpuDelegate { get; }
        public unsafe void WaitForCommandQueueOnCpu(IntPtr commandQueueToWaitPointer, ulong fenceValue)
        {
            if (this.graphicsService_WaitForCommandQueueOnCpuDelegate != null)
            {
                this.graphicsService_WaitForCommandQueueOnCpuDelegate(this.context, commandQueueToWaitPointer, fenceValue);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr> graphicsService_CreateCommandListDelegate { get; }
        public unsafe IntPtr CreateCommandList(IntPtr commandQueuePointer)
        {
            if (this.graphicsService_CreateCommandListDelegate != null)
            {
                return this.graphicsService_CreateCommandListDelegate(this.context, commandQueuePointer);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetCommandListLabelDelegate { get; }
        public unsafe void SetCommandListLabel(IntPtr commandListPointer, string label)
        {
            if (this.graphicsService_SetCommandListLabelDelegate != null)
            {
                this.graphicsService_SetCommandListLabelDelegate(this.context, commandListPointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteCommandListDelegate { get; }
        public unsafe void DeleteCommandList(IntPtr commandListPointer)
        {
            if (this.graphicsService_DeleteCommandListDelegate != null)
            {
                this.graphicsService_DeleteCommandListDelegate(this.context, commandListPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_ResetCommandListDelegate { get; }
        public unsafe void ResetCommandList(IntPtr commandListPointer)
        {
            if (this.graphicsService_ResetCommandListDelegate != null)
            {
                this.graphicsService_ResetCommandListDelegate(this.context, commandListPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_CommitCommandListDelegate { get; }
        public unsafe void CommitCommandList(IntPtr commandListPointer)
        {
            if (this.graphicsService_CommitCommandListDelegate != null)
            {
                this.graphicsService_CommitCommandListDelegate(this.context, commandListPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, GraphicsServiceHeapType, ulong, IntPtr> graphicsService_CreateGraphicsHeapDelegate { get; }
        public unsafe IntPtr CreateGraphicsHeap(GraphicsServiceHeapType type, ulong length)
        {
            if (this.graphicsService_CreateGraphicsHeapDelegate != null)
            {
                return this.graphicsService_CreateGraphicsHeapDelegate(this.context, type, length);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetGraphicsHeapLabelDelegate { get; }
        public unsafe void SetGraphicsHeapLabel(IntPtr graphicsHeapPointer, string label)
        {
            if (this.graphicsService_SetGraphicsHeapLabelDelegate != null)
            {
                this.graphicsService_SetGraphicsHeapLabelDelegate(this.context, graphicsHeapPointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteGraphicsHeapDelegate { get; }
        public unsafe void DeleteGraphicsHeap(IntPtr graphicsHeapPointer)
        {
            if (this.graphicsService_DeleteGraphicsHeapDelegate != null)
            {
                this.graphicsService_DeleteGraphicsHeapDelegate(this.context, graphicsHeapPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, ulong, bool, int, IntPtr> graphicsService_CreateGraphicsBufferDelegate { get; }
        public unsafe IntPtr CreateGraphicsBuffer(IntPtr graphicsHeapPointer, ulong heapOffset, bool isAliasable, int sizeInBytes)
        {
            if (this.graphicsService_CreateGraphicsBufferDelegate != null)
            {
                return this.graphicsService_CreateGraphicsBufferDelegate(this.context, graphicsHeapPointer, heapOffset, isAliasable, sizeInBytes);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetGraphicsBufferLabelDelegate { get; }
        public unsafe void SetGraphicsBufferLabel(IntPtr graphicsBufferPointer, string label)
        {
            if (this.graphicsService_SetGraphicsBufferLabelDelegate != null)
            {
                this.graphicsService_SetGraphicsBufferLabelDelegate(this.context, graphicsBufferPointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteGraphicsBufferDelegate { get; }
        public unsafe void DeleteGraphicsBuffer(IntPtr graphicsBufferPointer)
        {
            if (this.graphicsService_DeleteGraphicsBufferDelegate != null)
            {
                this.graphicsService_DeleteGraphicsBufferDelegate(this.context, graphicsBufferPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr> graphicsService_GetGraphicsBufferCpuPointerDelegate { get; }
        public unsafe IntPtr GetGraphicsBufferCpuPointer(IntPtr graphicsBufferPointer)
        {
            if (this.graphicsService_GetGraphicsBufferCpuPointerDelegate != null)
            {
                return this.graphicsService_GetGraphicsBufferCpuPointerDelegate(this.context, graphicsBufferPointer);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, ulong, bool, GraphicsTextureFormat, GraphicsTextureUsage, int, int, int, int, int, IntPtr> graphicsService_CreateTextureDelegate { get; }
        public unsafe IntPtr CreateTexture(IntPtr graphicsHeapPointer, ulong heapOffset, bool isAliasable, GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
        {
            if (this.graphicsService_CreateTextureDelegate != null)
            {
                return this.graphicsService_CreateTextureDelegate(this.context, graphicsHeapPointer, heapOffset, isAliasable, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetTextureLabelDelegate { get; }
        public unsafe void SetTextureLabel(IntPtr texturePointer, string label)
        {
            if (this.graphicsService_SetTextureLabelDelegate != null)
            {
                this.graphicsService_SetTextureLabelDelegate(this.context, texturePointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteTextureDelegate { get; }
        public unsafe void DeleteTexture(IntPtr texturePointer)
        {
            if (this.graphicsService_DeleteTextureDelegate != null)
            {
                this.graphicsService_DeleteTextureDelegate(this.context, texturePointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, int, GraphicsTextureFormat, IntPtr> graphicsService_CreateSwapChainDelegate { get; }
        public unsafe IntPtr CreateSwapChain(IntPtr windowPointer, IntPtr commandQueuePointer, int width, int height, GraphicsTextureFormat textureFormat)
        {
            if (this.graphicsService_CreateSwapChainDelegate != null)
            {
                return this.graphicsService_CreateSwapChainDelegate(this.context, windowPointer, commandQueuePointer, width, height, textureFormat);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, int, void> graphicsService_ResizeSwapChainDelegate { get; }
        public unsafe void ResizeSwapChain(IntPtr swapChainPointer, int width, int height)
        {
            if (this.graphicsService_ResizeSwapChainDelegate != null)
            {
                this.graphicsService_ResizeSwapChainDelegate(this.context, swapChainPointer, width, height);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr> graphicsService_GetSwapChainBackBufferTextureDelegate { get; }
        public unsafe IntPtr GetSwapChainBackBufferTexture(IntPtr swapChainPointer)
        {
            if (this.graphicsService_GetSwapChainBackBufferTextureDelegate != null)
            {
                return this.graphicsService_GetSwapChainBackBufferTextureDelegate(this.context, swapChainPointer);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, ulong> graphicsService_PresentSwapChainDelegate { get; }
        public unsafe ulong PresentSwapChain(IntPtr swapChainPointer)
        {
            if (this.graphicsService_PresentSwapChainDelegate != null)
            {
                return this.graphicsService_PresentSwapChainDelegate(this.context, swapChainPointer);
            }

            return default(ulong);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr> graphicsService_CreateIndirectCommandBufferDelegate { get; }
        public unsafe IntPtr CreateIndirectCommandBuffer(int maxCommandCount)
        {
            if (this.graphicsService_CreateIndirectCommandBufferDelegate != null)
            {
                return this.graphicsService_CreateIndirectCommandBufferDelegate(this.context, maxCommandCount);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetIndirectCommandBufferLabelDelegate { get; }
        public unsafe void SetIndirectCommandBufferLabel(IntPtr indirectCommandBufferPointer, string label)
        {
            if (this.graphicsService_SetIndirectCommandBufferLabelDelegate != null)
            {
                this.graphicsService_SetIndirectCommandBufferLabelDelegate(this.context, indirectCommandBufferPointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteIndirectCommandBufferDelegate { get; }
        public unsafe void DeleteIndirectCommandBuffer(IntPtr indirectCommandBufferPointer)
        {
            if (this.graphicsService_DeleteIndirectCommandBufferDelegate != null)
            {
                this.graphicsService_DeleteIndirectCommandBufferDelegate(this.context, indirectCommandBufferPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, GraphicsQueryBufferType, int, IntPtr> graphicsService_CreateQueryBufferDelegate { get; }
        public unsafe IntPtr CreateQueryBuffer(GraphicsQueryBufferType queryBufferType, int length)
        {
            if (this.graphicsService_CreateQueryBufferDelegate != null)
            {
                return this.graphicsService_CreateQueryBufferDelegate(this.context, queryBufferType, length);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetQueryBufferLabelDelegate { get; }
        public unsafe void SetQueryBufferLabel(IntPtr queryBufferPointer, string label)
        {
            if (this.graphicsService_SetQueryBufferLabelDelegate != null)
            {
                this.graphicsService_SetQueryBufferLabelDelegate(this.context, queryBufferPointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteQueryBufferDelegate { get; }
        public unsafe void DeleteQueryBuffer(IntPtr queryBufferPointer)
        {
            if (this.graphicsService_DeleteQueryBufferDelegate != null)
            {
                this.graphicsService_DeleteQueryBufferDelegate(this.context, queryBufferPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, string?, byte*, int, IntPtr> graphicsService_CreateShaderDelegate { get; }
        public unsafe IntPtr CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode)
        {
            if (this.graphicsService_CreateShaderDelegate != null)
            {
                fixed (byte* shaderByteCodePinned = shaderByteCode)
                    return this.graphicsService_CreateShaderDelegate(this.context, computeShaderFunction, shaderByteCodePinned, shaderByteCode.Length);
                }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetShaderLabelDelegate { get; }
        public unsafe void SetShaderLabel(IntPtr shaderPointer, string label)
        {
            if (this.graphicsService_SetShaderLabelDelegate != null)
            {
                this.graphicsService_SetShaderLabelDelegate(this.context, shaderPointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeleteShaderDelegate { get; }
        public unsafe void DeleteShader(IntPtr shaderPointer)
        {
            if (this.graphicsService_DeleteShaderDelegate != null)
            {
                this.graphicsService_DeleteShaderDelegate(this.context, shaderPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GraphicsRenderPassDescriptor, IntPtr> graphicsService_CreatePipelineStateDelegate { get; }
        public unsafe IntPtr CreatePipelineState(IntPtr shaderPointer, GraphicsRenderPassDescriptor renderPassDescriptor)
        {
            if (this.graphicsService_CreatePipelineStateDelegate != null)
            {
                return this.graphicsService_CreatePipelineStateDelegate(this.context, shaderPointer, renderPassDescriptor);
            }

            return default(IntPtr);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, string, void> graphicsService_SetPipelineStateLabelDelegate { get; }
        public unsafe void SetPipelineStateLabel(IntPtr pipelineStatePointer, string label)
        {
            if (this.graphicsService_SetPipelineStateLabelDelegate != null)
            {
                this.graphicsService_SetPipelineStateLabelDelegate(this.context, pipelineStatePointer, label);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_DeletePipelineStateDelegate { get; }
        public unsafe void DeletePipelineState(IntPtr pipelineStatePointer)
        {
            if (this.graphicsService_DeletePipelineStateDelegate != null)
            {
                this.graphicsService_DeletePipelineStateDelegate(this.context, pipelineStatePointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, bool, int, void> graphicsService_SetShaderBufferDelegate { get; }
        public unsafe void SetShaderBuffer(IntPtr commandListPointer, IntPtr graphicsBufferPointer, int slot, bool isReadOnly, int index)
        {
            if (this.graphicsService_SetShaderBufferDelegate != null)
            {
                this.graphicsService_SetShaderBufferDelegate(this.context, commandListPointer, graphicsBufferPointer, slot, isReadOnly, index);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr*, int, int, int, void> graphicsService_SetShaderBuffersDelegate { get; }
        public unsafe void SetShaderBuffers(IntPtr commandListPointer, ReadOnlySpan<IntPtr> graphicsBufferPointerList, int slot, int index)
        {
            if (this.graphicsService_SetShaderBuffersDelegate != null)
            {
                fixed (IntPtr* graphicsBufferPointerListPinned = graphicsBufferPointerList)
                    this.graphicsService_SetShaderBuffersDelegate(this.context, commandListPointer, graphicsBufferPointerListPinned, graphicsBufferPointerList.Length, slot, index);
                }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, bool, int, void> graphicsService_SetShaderTextureDelegate { get; }
        public unsafe void SetShaderTexture(IntPtr commandListPointer, IntPtr texturePointer, int slot, bool isReadOnly, int index)
        {
            if (this.graphicsService_SetShaderTextureDelegate != null)
            {
                this.graphicsService_SetShaderTextureDelegate(this.context, commandListPointer, texturePointer, slot, isReadOnly, index);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr*, int, int, int, void> graphicsService_SetShaderTexturesDelegate { get; }
        public unsafe void SetShaderTextures(IntPtr commandListPointer, ReadOnlySpan<IntPtr> texturePointerList, int slot, int index)
        {
            if (this.graphicsService_SetShaderTexturesDelegate != null)
            {
                fixed (IntPtr* texturePointerListPinned = texturePointerList)
                    this.graphicsService_SetShaderTexturesDelegate(this.context, commandListPointer, texturePointerListPinned, texturePointerList.Length, slot, index);
                }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, int, void> graphicsService_SetShaderIndirectCommandListDelegate { get; }
        public unsafe void SetShaderIndirectCommandList(IntPtr commandListPointer, IntPtr indirectCommandListPointer, int slot, int index)
        {
            if (this.graphicsService_SetShaderIndirectCommandListDelegate != null)
            {
                this.graphicsService_SetShaderIndirectCommandListDelegate(this.context, commandListPointer, indirectCommandListPointer, slot, index);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr*, int, int, int, void> graphicsService_SetShaderIndirectCommandListsDelegate { get; }
        public unsafe void SetShaderIndirectCommandLists(IntPtr commandListPointer, ReadOnlySpan<IntPtr> indirectCommandListPointerList, int slot, int index)
        {
            if (this.graphicsService_SetShaderIndirectCommandListsDelegate != null)
            {
                fixed (IntPtr* indirectCommandListPointerListPinned = indirectCommandListPointerList)
                    this.graphicsService_SetShaderIndirectCommandListsDelegate(this.context, commandListPointer, indirectCommandListPointerListPinned, indirectCommandListPointerList.Length, slot, index);
                }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, int, void> graphicsService_CopyDataToGraphicsBufferDelegate { get; }
        public unsafe void CopyDataToGraphicsBuffer(IntPtr commandListPointer, IntPtr destinationGraphicsBufferPointer, IntPtr sourceGraphicsBufferPointer, int length)
        {
            if (this.graphicsService_CopyDataToGraphicsBufferDelegate != null)
            {
                this.graphicsService_CopyDataToGraphicsBufferDelegate(this.context, commandListPointer, destinationGraphicsBufferPointer, sourceGraphicsBufferPointer, length);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, GraphicsTextureFormat, int, int, int, int, void> graphicsService_CopyDataToTextureDelegate { get; }
        public unsafe void CopyDataToTexture(IntPtr commandListPointer, IntPtr destinationTexturePointer, IntPtr sourceGraphicsBufferPointer, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
        {
            if (this.graphicsService_CopyDataToTextureDelegate != null)
            {
                this.graphicsService_CopyDataToTextureDelegate(this.context, commandListPointer, destinationTexturePointer, sourceGraphicsBufferPointer, textureFormat, width, height, slice, mipLevel);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void> graphicsService_CopyTextureDelegate { get; }
        public unsafe void CopyTexture(IntPtr commandListPointer, IntPtr destinationTexturePointer, IntPtr sourceTexturePointer)
        {
            if (this.graphicsService_CopyTextureDelegate != null)
            {
                this.graphicsService_CopyTextureDelegate(this.context, commandListPointer, destinationTexturePointer, sourceTexturePointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, void> graphicsService_ResetIndirectCommandListDelegate { get; }
        public unsafe void ResetIndirectCommandList(IntPtr commandListPointer, IntPtr indirectCommandListPointer, int maxCommandCount)
        {
            if (this.graphicsService_ResetIndirectCommandListDelegate != null)
            {
                this.graphicsService_ResetIndirectCommandListDelegate(this.context, commandListPointer, indirectCommandListPointer, maxCommandCount);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, void> graphicsService_OptimizeIndirectCommandListDelegate { get; }
        public unsafe void OptimizeIndirectCommandList(IntPtr commandListPointer, IntPtr indirectCommandListPointer, int maxCommandCount)
        {
            if (this.graphicsService_OptimizeIndirectCommandListDelegate != null)
            {
                this.graphicsService_OptimizeIndirectCommandListDelegate(this.context, commandListPointer, indirectCommandListPointer, maxCommandCount);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, uint, uint, uint, Vector3> graphicsService_DispatchThreadsDelegate { get; }
        public unsafe Vector3 DispatchThreads(IntPtr commandListPointer, uint threadCountX, uint threadCountY, uint threadCountZ)
        {
            if (this.graphicsService_DispatchThreadsDelegate != null)
            {
                return this.graphicsService_DispatchThreadsDelegate(this.context, commandListPointer, threadCountX, threadCountY, threadCountZ);
            }

            return default(Vector3);
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GraphicsRenderPassDescriptor, void> graphicsService_BeginRenderPassDelegate { get; }
        public unsafe void BeginRenderPass(IntPtr commandListPointer, GraphicsRenderPassDescriptor renderPassDescriptor)
        {
            if (this.graphicsService_BeginRenderPassDelegate != null)
            {
                this.graphicsService_BeginRenderPassDelegate(this.context, commandListPointer, renderPassDescriptor);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> graphicsService_EndRenderPassDelegate { get; }
        public unsafe void EndRenderPass(IntPtr commandListPointer)
        {
            if (this.graphicsService_EndRenderPassDelegate != null)
            {
                this.graphicsService_EndRenderPassDelegate(this.context, commandListPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> graphicsService_SetPipelineStateDelegate { get; }
        public unsafe void SetPipelineState(IntPtr commandListPointer, IntPtr pipelineStatePointer)
        {
            if (this.graphicsService_SetPipelineStateDelegate != null)
            {
                this.graphicsService_SetPipelineStateDelegate(this.context, commandListPointer, pipelineStatePointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> graphicsService_SetShaderDelegate { get; }
        public unsafe void SetShader(IntPtr commandListPointer, IntPtr shaderPointer)
        {
            if (this.graphicsService_SetShaderDelegate != null)
            {
                this.graphicsService_SetShaderDelegate(this.context, commandListPointer, shaderPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, void> graphicsService_ExecuteIndirectCommandBufferDelegate { get; }
        public unsafe void ExecuteIndirectCommandBuffer(IntPtr commandListPointer, IntPtr indirectCommandBufferPointer, int maxCommandCount)
        {
            if (this.graphicsService_ExecuteIndirectCommandBufferDelegate != null)
            {
                this.graphicsService_ExecuteIndirectCommandBufferDelegate(this.context, commandListPointer, indirectCommandBufferPointer, maxCommandCount);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> graphicsService_SetIndexBufferDelegate { get; }
        public unsafe void SetIndexBuffer(IntPtr commandListPointer, IntPtr graphicsBufferPointer)
        {
            if (this.graphicsService_SetIndexBufferDelegate != null)
            {
                this.graphicsService_SetIndexBufferDelegate(this.context, commandListPointer, graphicsBufferPointer);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GraphicsPrimitiveType, int, int, int, int, void> graphicsService_DrawIndexedPrimitivesDelegate { get; }
        public unsafe void DrawIndexedPrimitives(IntPtr commandListPointer, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (this.graphicsService_DrawIndexedPrimitivesDelegate != null)
            {
                this.graphicsService_DrawIndexedPrimitivesDelegate(this.context, commandListPointer, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, GraphicsPrimitiveType, int, int, void> graphicsService_DrawPrimitivesDelegate { get; }
        public unsafe void DrawPrimitives(IntPtr commandListPointer, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
        {
            if (this.graphicsService_DrawPrimitivesDelegate != null)
            {
                this.graphicsService_DrawPrimitivesDelegate(this.context, commandListPointer, primitiveType, startVertex, vertexCount);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, void> graphicsService_QueryTimestampDelegate { get; }
        public unsafe void QueryTimestamp(IntPtr commandListPointer, IntPtr queryBufferPointer, int index)
        {
            if (this.graphicsService_QueryTimestampDelegate != null)
            {
                this.graphicsService_QueryTimestampDelegate(this.context, commandListPointer, queryBufferPointer, index);
            }
        }

        private delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, int, int, void> graphicsService_ResolveQueryDataDelegate { get; }
        public unsafe void ResolveQueryData(IntPtr commandListPointer, IntPtr queryBufferPointer, IntPtr destinationBufferPointer, int startIndex, int endIndex)
        {
            if (this.graphicsService_ResolveQueryDataDelegate != null)
            {
                this.graphicsService_ResolveQueryDataDelegate(this.context, commandListPointer, queryBufferPointer, destinationBufferPointer, startIndex, endIndex);
            }
        }
    }
}

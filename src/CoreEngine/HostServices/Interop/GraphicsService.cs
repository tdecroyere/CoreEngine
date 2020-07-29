using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe struct GraphicsService : IGraphicsService
    {
        private IntPtr context { get; }

        private delegate* cdecl<IntPtr, byte*, void> graphicsService_GetGraphicsAdapterNameDelegate { get; }
        public unsafe string GetGraphicsAdapterName()
        {
            var output = ArrayPool<byte>.Shared.Rent(255);
            if (this.context != null && this.graphicsService_GetGraphicsAdapterNameDelegate != null)
            {
                fixed (byte* outputPinned = output)
this.graphicsService_GetGraphicsAdapterNameDelegate(this.context, outputPinned);
                    var result = System.Text.Encoding.UTF8.GetString(output).TrimEnd(' ');
                    ArrayPool<byte>.Shared.Return(output);
                    return result;
                }

            return string.Empty;
        }

        private delegate* cdecl<IntPtr, Vector2> graphicsService_GetRenderSizeDelegate { get; }
        public unsafe Vector2 GetRenderSize()
        {
            if (this.context != null && this.graphicsService_GetRenderSizeDelegate != null)
            {
                return this.graphicsService_GetRenderSizeDelegate(this.context);
            }

            return default(Vector2);
        }

        private delegate* cdecl<IntPtr, GraphicsTextureFormat, GraphicsTextureUsage, int, int, int, int, int, GraphicsAllocationInfos> graphicsService_GetTextureAllocationInfosDelegate { get; }
        public unsafe GraphicsAllocationInfos GetTextureAllocationInfos(GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
        {
            if (this.context != null && this.graphicsService_GetTextureAllocationInfosDelegate != null)
            {
                return this.graphicsService_GetTextureAllocationInfosDelegate(this.context, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
            }

            return default(GraphicsAllocationInfos);
        }

        private delegate* cdecl<IntPtr, uint, GraphicsServiceHeapType, ulong, bool> graphicsService_CreateGraphicsHeapDelegate { get; }
        public unsafe bool CreateGraphicsHeap(uint graphicsHeapId, GraphicsServiceHeapType type, ulong length)
        {
            if (this.context != null && this.graphicsService_CreateGraphicsHeapDelegate != null)
            {
                return this.graphicsService_CreateGraphicsHeapDelegate(this.context, graphicsHeapId, type, length);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetGraphicsHeapLabelDelegate { get; }
        public unsafe void SetGraphicsHeapLabel(uint graphicsHeapId, string label)
        {
            if (this.context != null && this.graphicsService_SetGraphicsHeapLabelDelegate != null)
            {
                this.graphicsService_SetGraphicsHeapLabelDelegate(this.context, graphicsHeapId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteGraphicsHeapDelegate { get; }
        public unsafe void DeleteGraphicsHeap(uint graphicsHeapId)
        {
            if (this.context != null && this.graphicsService_DeleteGraphicsHeapDelegate != null)
            {
                this.graphicsService_DeleteGraphicsHeapDelegate(this.context, graphicsHeapId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, ulong, bool, int, bool> graphicsService_CreateGraphicsBufferDelegate { get; }
        public unsafe bool CreateGraphicsBuffer(uint graphicsBufferId, uint graphicsHeapId, ulong heapOffset, bool isAliasable, int sizeInBytes)
        {
            if (this.context != null && this.graphicsService_CreateGraphicsBufferDelegate != null)
            {
                return this.graphicsService_CreateGraphicsBufferDelegate(this.context, graphicsBufferId, graphicsHeapId, heapOffset, isAliasable, sizeInBytes);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetGraphicsBufferLabelDelegate { get; }
        public unsafe void SetGraphicsBufferLabel(uint graphicsBufferId, string label)
        {
            if (this.context != null && this.graphicsService_SetGraphicsBufferLabelDelegate != null)
            {
                this.graphicsService_SetGraphicsBufferLabelDelegate(this.context, graphicsBufferId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteGraphicsBufferDelegate { get; }
        public unsafe void DeleteGraphicsBuffer(uint graphicsBufferId)
        {
            if (this.context != null && this.graphicsService_DeleteGraphicsBufferDelegate != null)
            {
                this.graphicsService_DeleteGraphicsBufferDelegate(this.context, graphicsBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, IntPtr> graphicsService_GetGraphicsBufferCpuPointerDelegate { get; }
        public unsafe IntPtr GetGraphicsBufferCpuPointer(uint graphicsBufferId)
        {
            if (this.context != null && this.graphicsService_GetGraphicsBufferCpuPointerDelegate != null)
            {
                return this.graphicsService_GetGraphicsBufferCpuPointerDelegate(this.context, graphicsBufferId);
            }

            return default(IntPtr);
        }

        private delegate* cdecl<IntPtr, uint, uint, ulong, bool, GraphicsTextureFormat, GraphicsTextureUsage, int, int, int, int, int, bool> graphicsService_CreateTextureDelegate { get; }
        public unsafe bool CreateTexture(uint textureId, uint graphicsHeapId, ulong heapOffset, bool isAliasable, GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
        {
            if (this.context != null && this.graphicsService_CreateTextureDelegate != null)
            {
                return this.graphicsService_CreateTextureDelegate(this.context, textureId, graphicsHeapId, heapOffset, isAliasable, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetTextureLabelDelegate { get; }
        public unsafe void SetTextureLabel(uint textureId, string label)
        {
            if (this.context != null && this.graphicsService_SetTextureLabelDelegate != null)
            {
                this.graphicsService_SetTextureLabelDelegate(this.context, textureId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteTextureDelegate { get; }
        public unsafe void DeleteTexture(uint textureId)
        {
            if (this.context != null && this.graphicsService_DeleteTextureDelegate != null)
            {
                this.graphicsService_DeleteTextureDelegate(this.context, textureId);
            }
        }

        private delegate* cdecl<IntPtr, uint, int, bool> graphicsService_CreateIndirectCommandBufferDelegate { get; }
        public unsafe bool CreateIndirectCommandBuffer(uint indirectCommandBufferId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_CreateIndirectCommandBufferDelegate != null)
            {
                return this.graphicsService_CreateIndirectCommandBufferDelegate(this.context, indirectCommandBufferId, maxCommandCount);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetIndirectCommandBufferLabelDelegate { get; }
        public unsafe void SetIndirectCommandBufferLabel(uint indirectCommandBufferId, string label)
        {
            if (this.context != null && this.graphicsService_SetIndirectCommandBufferLabelDelegate != null)
            {
                this.graphicsService_SetIndirectCommandBufferLabelDelegate(this.context, indirectCommandBufferId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteIndirectCommandBufferDelegate { get; }
        public unsafe void DeleteIndirectCommandBuffer(uint indirectCommandBufferId)
        {
            if (this.context != null && this.graphicsService_DeleteIndirectCommandBufferDelegate != null)
            {
                this.graphicsService_DeleteIndirectCommandBufferDelegate(this.context, indirectCommandBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, string?, byte*, int, bool> graphicsService_CreateShaderDelegate { get; }
        public unsafe bool CreateShader(uint shaderId, string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode)
        {
            if (this.context != null && this.graphicsService_CreateShaderDelegate != null)
            {
                fixed (byte* shaderByteCodePinned = shaderByteCode)
                    return this.graphicsService_CreateShaderDelegate(this.context, shaderId, computeShaderFunction, shaderByteCodePinned, shaderByteCode.Length);
                }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetShaderLabelDelegate { get; }
        public unsafe void SetShaderLabel(uint shaderId, string label)
        {
            if (this.context != null && this.graphicsService_SetShaderLabelDelegate != null)
            {
                this.graphicsService_SetShaderLabelDelegate(this.context, shaderId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteShaderDelegate { get; }
        public unsafe void DeleteShader(uint shaderId)
        {
            if (this.context != null && this.graphicsService_DeleteShaderDelegate != null)
            {
                this.graphicsService_DeleteShaderDelegate(this.context, shaderId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, GraphicsRenderPassDescriptor, bool> graphicsService_CreatePipelineStateDelegate { get; }
        public unsafe bool CreatePipelineState(uint pipelineStateId, uint shaderId, GraphicsRenderPassDescriptor renderPassDescriptor)
        {
            if (this.context != null && this.graphicsService_CreatePipelineStateDelegate != null)
            {
                return this.graphicsService_CreatePipelineStateDelegate(this.context, pipelineStateId, shaderId, renderPassDescriptor);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetPipelineStateLabelDelegate { get; }
        public unsafe void SetPipelineStateLabel(uint pipelineStateId, string label)
        {
            if (this.context != null && this.graphicsService_SetPipelineStateLabelDelegate != null)
            {
                this.graphicsService_SetPipelineStateLabelDelegate(this.context, pipelineStateId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeletePipelineStateDelegate { get; }
        public unsafe void DeletePipelineState(uint pipelineStateId)
        {
            if (this.context != null && this.graphicsService_DeletePipelineStateDelegate != null)
            {
                this.graphicsService_DeletePipelineStateDelegate(this.context, pipelineStateId);
            }
        }

        private delegate* cdecl<IntPtr, uint, GraphicsQueryBufferType, int, bool> graphicsService_CreateQueryBufferDelegate { get; }
        public unsafe bool CreateQueryBuffer(uint queryBufferId, GraphicsQueryBufferType queryBufferType, int length)
        {
            if (this.context != null && this.graphicsService_CreateQueryBufferDelegate != null)
            {
                return this.graphicsService_CreateQueryBufferDelegate(this.context, queryBufferId, queryBufferType, length);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, string, void> graphicsService_SetQueryBufferLabelDelegate { get; }
        public unsafe void SetQueryBufferLabel(uint queryBufferId, string label)
        {
            if (this.context != null && this.graphicsService_SetQueryBufferLabelDelegate != null)
            {
                this.graphicsService_SetQueryBufferLabelDelegate(this.context, queryBufferId, label);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteQueryBufferDelegate { get; }
        public unsafe void DeleteQueryBuffer(uint queryBufferId)
        {
            if (this.context != null && this.graphicsService_DeleteQueryBufferDelegate != null)
            {
                this.graphicsService_DeleteQueryBufferDelegate(this.context, queryBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, IntPtr> graphicsService_GetQueryBufferCpuPointerDelegate { get; }
        public unsafe IntPtr GetQueryBufferCpuPointer(uint queryBufferId)
        {
            if (this.context != null && this.graphicsService_GetQueryBufferCpuPointerDelegate != null)
            {
                return this.graphicsService_GetQueryBufferCpuPointerDelegate(this.context, queryBufferId);
            }

            return default(IntPtr);
        }

        private delegate* cdecl<IntPtr, uint, GraphicsCommandBufferType, string, bool> graphicsService_CreateCommandBufferDelegate { get; }
        public unsafe bool CreateCommandBuffer(uint commandBufferId, GraphicsCommandBufferType commandBufferType, string label)
        {
            if (this.context != null && this.graphicsService_CreateCommandBufferDelegate != null)
            {
                return this.graphicsService_CreateCommandBufferDelegate(this.context, commandBufferId, commandBufferType, label);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_DeleteCommandBufferDelegate { get; }
        public unsafe void DeleteCommandBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_DeleteCommandBufferDelegate != null)
            {
                this.graphicsService_DeleteCommandBufferDelegate(this.context, commandBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_ResetCommandBufferDelegate { get; }
        public unsafe void ResetCommandBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_ResetCommandBufferDelegate != null)
            {
                this.graphicsService_ResetCommandBufferDelegate(this.context, commandBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_ExecuteCommandBufferDelegate { get; }
        public unsafe void ExecuteCommandBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_ExecuteCommandBufferDelegate != null)
            {
                this.graphicsService_ExecuteCommandBufferDelegate(this.context, commandBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, NullableGraphicsCommandBufferStatus> graphicsService_GetCommandBufferStatusDelegate { get; }
        public unsafe GraphicsCommandBufferStatus? GetCommandBufferStatus(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_GetCommandBufferStatusDelegate != null)
            {
            {
                var returnedValue = this.graphicsService_GetCommandBufferStatusDelegate(this.context, commandBufferId);
                if (returnedValue.HasValue) return returnedValue.Value;
            }
            }

            return default(GraphicsCommandBufferStatus?);
        }

        private delegate* cdecl<IntPtr, uint, uint, int, bool, int, void> graphicsService_SetShaderBufferDelegate { get; }
        public unsafe void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, bool isReadOnly, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderBufferDelegate != null)
            {
                this.graphicsService_SetShaderBufferDelegate(this.context, commandListId, graphicsBufferId, slot, isReadOnly, index);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint*, int, int, int, void> graphicsService_SetShaderBuffersDelegate { get; }
        public unsafe void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderBuffersDelegate != null)
            {
                fixed (uint* graphicsBufferIdListPinned = graphicsBufferIdList)
                    this.graphicsService_SetShaderBuffersDelegate(this.context, commandListId, graphicsBufferIdListPinned, graphicsBufferIdList.Length, slot, index);
                }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, bool, int, void> graphicsService_SetShaderTextureDelegate { get; }
        public unsafe void SetShaderTexture(uint commandListId, uint textureId, int slot, bool isReadOnly, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderTextureDelegate != null)
            {
                this.graphicsService_SetShaderTextureDelegate(this.context, commandListId, textureId, slot, isReadOnly, index);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint*, int, int, int, void> graphicsService_SetShaderTexturesDelegate { get; }
        public unsafe void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderTexturesDelegate != null)
            {
                fixed (uint* textureIdListPinned = textureIdList)
                    this.graphicsService_SetShaderTexturesDelegate(this.context, commandListId, textureIdListPinned, textureIdList.Length, slot, index);
                }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, int, void> graphicsService_SetShaderIndirectCommandListDelegate { get; }
        public unsafe void SetShaderIndirectCommandList(uint commandListId, uint indirectCommandListId, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderIndirectCommandListDelegate != null)
            {
                this.graphicsService_SetShaderIndirectCommandListDelegate(this.context, commandListId, indirectCommandListId, slot, index);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint*, int, int, int, void> graphicsService_SetShaderIndirectCommandListsDelegate { get; }
        public unsafe void SetShaderIndirectCommandLists(uint commandListId, ReadOnlySpan<uint> indirectCommandListIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderIndirectCommandListsDelegate != null)
            {
                fixed (uint* indirectCommandListIdListPinned = indirectCommandListIdList)
                    this.graphicsService_SetShaderIndirectCommandListsDelegate(this.context, commandListId, indirectCommandListIdListPinned, indirectCommandListIdList.Length, slot, index);
                }
        }

        private delegate* cdecl<IntPtr, uint, uint, string, bool> graphicsService_CreateCopyCommandListDelegate { get; }
        public unsafe bool CreateCopyCommandList(uint commandListId, uint commandBufferId, string label)
        {
            if (this.context != null && this.graphicsService_CreateCopyCommandListDelegate != null)
            {
                return this.graphicsService_CreateCopyCommandListDelegate(this.context, commandListId, commandBufferId, label);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_CommitCopyCommandListDelegate { get; }
        public unsafe void CommitCopyCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CommitCopyCommandListDelegate != null)
            {
                this.graphicsService_CommitCopyCommandListDelegate(this.context, commandListId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, uint, int, void> graphicsService_CopyDataToGraphicsBufferDelegate { get; }
        public unsafe void CopyDataToGraphicsBuffer(uint commandListId, uint destinationGraphicsBufferId, uint sourceGraphicsBufferId, int length)
        {
            if (this.context != null && this.graphicsService_CopyDataToGraphicsBufferDelegate != null)
            {
                this.graphicsService_CopyDataToGraphicsBufferDelegate(this.context, commandListId, destinationGraphicsBufferId, sourceGraphicsBufferId, length);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, uint, GraphicsTextureFormat, int, int, int, int, void> graphicsService_CopyDataToTextureDelegate { get; }
        public unsafe void CopyDataToTexture(uint commandListId, uint destinationTextureId, uint sourceGraphicsBufferId, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel)
        {
            if (this.context != null && this.graphicsService_CopyDataToTextureDelegate != null)
            {
                this.graphicsService_CopyDataToTextureDelegate(this.context, commandListId, destinationTextureId, sourceGraphicsBufferId, textureFormat, width, height, slice, mipLevel);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, uint, void> graphicsService_CopyTextureDelegate { get; }
        public unsafe void CopyTexture(uint commandListId, uint destinationTextureId, uint sourceTextureId)
        {
            if (this.context != null && this.graphicsService_CopyTextureDelegate != null)
            {
                this.graphicsService_CopyTextureDelegate(this.context, commandListId, destinationTextureId, sourceTextureId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, void> graphicsService_ResetIndirectCommandListDelegate { get; }
        public unsafe void ResetIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_ResetIndirectCommandListDelegate != null)
            {
                this.graphicsService_ResetIndirectCommandListDelegate(this.context, commandListId, indirectCommandListId, maxCommandCount);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, void> graphicsService_OptimizeIndirectCommandListDelegate { get; }
        public unsafe void OptimizeIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_OptimizeIndirectCommandListDelegate != null)
            {
                this.graphicsService_OptimizeIndirectCommandListDelegate(this.context, commandListId, indirectCommandListId, maxCommandCount);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, string, bool> graphicsService_CreateComputeCommandListDelegate { get; }
        public unsafe bool CreateComputeCommandList(uint commandListId, uint commandBufferId, string label)
        {
            if (this.context != null && this.graphicsService_CreateComputeCommandListDelegate != null)
            {
                return this.graphicsService_CreateComputeCommandListDelegate(this.context, commandListId, commandBufferId, label);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_CommitComputeCommandListDelegate { get; }
        public unsafe void CommitComputeCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CommitComputeCommandListDelegate != null)
            {
                this.graphicsService_CommitComputeCommandListDelegate(this.context, commandListId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, uint, uint, Vector3> graphicsService_DispatchThreadsDelegate { get; }
        public unsafe Vector3 DispatchThreads(uint commandListId, uint threadCountX, uint threadCountY, uint threadCountZ)
        {
            if (this.context != null && this.graphicsService_DispatchThreadsDelegate != null)
            {
                return this.graphicsService_DispatchThreadsDelegate(this.context, commandListId, threadCountX, threadCountY, threadCountZ);
            }

            return default(Vector3);
        }

        private delegate* cdecl<IntPtr, uint, uint, GraphicsRenderPassDescriptor, string, bool> graphicsService_CreateRenderCommandListDelegate { get; }
        public unsafe bool CreateRenderCommandList(uint commandListId, uint commandBufferId, GraphicsRenderPassDescriptor renderDescriptor, string label)
        {
            if (this.context != null && this.graphicsService_CreateRenderCommandListDelegate != null)
            {
                return this.graphicsService_CreateRenderCommandListDelegate(this.context, commandListId, commandBufferId, renderDescriptor, label);
            }

            return default(bool);
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_CommitRenderCommandListDelegate { get; }
        public unsafe void CommitRenderCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CommitRenderCommandListDelegate != null)
            {
                this.graphicsService_CommitRenderCommandListDelegate(this.context, commandListId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, void> graphicsService_SetPipelineStateDelegate { get; }
        public unsafe void SetPipelineState(uint commandListId, uint pipelineStateId)
        {
            if (this.context != null && this.graphicsService_SetPipelineStateDelegate != null)
            {
                this.graphicsService_SetPipelineStateDelegate(this.context, commandListId, pipelineStateId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, void> graphicsService_SetShaderDelegate { get; }
        public unsafe void SetShader(uint commandListId, uint shaderId)
        {
            if (this.context != null && this.graphicsService_SetShaderDelegate != null)
            {
                this.graphicsService_SetShaderDelegate(this.context, commandListId, shaderId);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, void> graphicsService_ExecuteIndirectCommandBufferDelegate { get; }
        public unsafe void ExecuteIndirectCommandBuffer(uint commandListId, uint indirectCommandBufferId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_ExecuteIndirectCommandBufferDelegate != null)
            {
                this.graphicsService_ExecuteIndirectCommandBufferDelegate(this.context, commandListId, indirectCommandBufferId, maxCommandCount);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, void> graphicsService_SetIndexBufferDelegate { get; }
        public unsafe void SetIndexBuffer(uint commandListId, uint graphicsBufferId)
        {
            if (this.context != null && this.graphicsService_SetIndexBufferDelegate != null)
            {
                this.graphicsService_SetIndexBufferDelegate(this.context, commandListId, graphicsBufferId);
            }
        }

        private delegate* cdecl<IntPtr, uint, GraphicsPrimitiveType, int, int, int, int, void> graphicsService_DrawIndexedPrimitivesDelegate { get; }
        public unsafe void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (this.context != null && this.graphicsService_DrawIndexedPrimitivesDelegate != null)
            {
                this.graphicsService_DrawIndexedPrimitivesDelegate(this.context, commandListId, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId);
            }
        }

        private delegate* cdecl<IntPtr, uint, GraphicsPrimitiveType, int, int, void> graphicsService_DrawPrimitivesDelegate { get; }
        public unsafe void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
        {
            if (this.context != null && this.graphicsService_DrawPrimitivesDelegate != null)
            {
                this.graphicsService_DrawPrimitivesDelegate(this.context, commandListId, primitiveType, startVertex, vertexCount);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, void> graphicsService_QueryTimestampDelegate { get; }
        public unsafe void QueryTimestamp(uint commandListId, uint queryBufferId, int index)
        {
            if (this.context != null && this.graphicsService_QueryTimestampDelegate != null)
            {
                this.graphicsService_QueryTimestampDelegate(this.context, commandListId, queryBufferId, index);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, int, int, void> graphicsService_ResolveQueryDataDelegate { get; }
        public unsafe void ResolveQueryData(uint commandListId, uint queryBufferId, int startIndex, int endIndex)
        {
            if (this.context != null && this.graphicsService_ResolveQueryDataDelegate != null)
            {
                this.graphicsService_ResolveQueryDataDelegate(this.context, commandListId, queryBufferId, startIndex, endIndex);
            }
        }

        private delegate* cdecl<IntPtr, uint, uint, void> graphicsService_WaitForCommandListDelegate { get; }
        public unsafe void WaitForCommandList(uint commandListId, uint commandListToWaitId)
        {
            if (this.context != null && this.graphicsService_WaitForCommandListDelegate != null)
            {
                this.graphicsService_WaitForCommandListDelegate(this.context, commandListId, commandListToWaitId);
            }
        }

        private delegate* cdecl<IntPtr, uint, void> graphicsService_PresentScreenBufferDelegate { get; }
        public unsafe void PresentScreenBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_PresentScreenBufferDelegate != null)
            {
                this.graphicsService_PresentScreenBufferDelegate(this.context, commandBufferId);
            }
        }

        private delegate* cdecl<IntPtr, void> graphicsService_WaitForAvailableScreenBufferDelegate { get; }
        public unsafe void WaitForAvailableScreenBuffer()
        {
            if (this.context != null && this.graphicsService_WaitForAvailableScreenBufferDelegate != null)
            {
                this.graphicsService_WaitForAvailableScreenBufferDelegate(this.context);
            }
        }
    }

    public struct NullableGraphicsCommandBufferStatus
    {
        public bool HasValue { get; }
        public GraphicsCommandBufferStatus Value { get; }
    }
}

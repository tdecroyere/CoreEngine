using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate Vector2 GraphicsService_GetRenderSizeDelegate(IntPtr context);
    internal unsafe delegate string GraphicsService_GetGraphicsAdapterNameDelegate(IntPtr context);
    internal unsafe delegate bool GraphicsService_CreateGraphicsBufferDelegate(IntPtr context, uint graphicsBufferId, int length, bool isWriteOnly, string label);
    internal unsafe delegate bool GraphicsService_CreateTextureDelegate(IntPtr context, uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isRenderTarget, string label);
    internal unsafe delegate void GraphicsService_DeleteTextureDelegate(IntPtr context, uint textureId);
    internal unsafe delegate bool GraphicsService_CreateIndirectCommandBufferDelegate(IntPtr context, uint indirectCommandBufferId, int maxCommandCount, string label);
    internal unsafe delegate bool GraphicsService_CreateShaderDelegate(IntPtr context, uint shaderId, string? computeShaderFunction, byte* shaderByteCode, int shaderByteCodeLength, string label);
    internal unsafe delegate void GraphicsService_DeleteShaderDelegate(IntPtr context, uint shaderId);
    internal unsafe delegate bool GraphicsService_CreatePipelineStateDelegate(IntPtr context, uint pipelineStateId, uint shaderId, GraphicsRenderPassDescriptor renderPassDescriptor, string label);
    internal unsafe delegate void GraphicsService_DeletePipelineStateDelegate(IntPtr context, uint pipelineStateId);
    internal unsafe delegate bool GraphicsService_CreateCommandBufferDelegate(IntPtr context, uint commandBufferId, string label);
    internal unsafe delegate void GraphicsService_DeleteCommandBufferDelegate(IntPtr context, uint commandBufferId);
    internal unsafe delegate void GraphicsService_ResetCommandBufferDelegate(IntPtr context, uint commandBufferId);
    internal unsafe delegate void GraphicsService_ExecuteCommandBufferDelegate(IntPtr context, uint commandBufferId);
    internal unsafe delegate NullableGraphicsCommandBufferStatus GraphicsService_GetCommandBufferStatusDelegate(IntPtr context, uint commandBufferId);
    internal unsafe delegate void GraphicsService_SetShaderBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, int slot, bool isReadOnly, int index);
    internal unsafe delegate void GraphicsService_SetShaderBuffersDelegate(IntPtr context, uint commandListId, uint* graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
    internal unsafe delegate void GraphicsService_SetShaderTextureDelegate(IntPtr context, uint commandListId, uint textureId, int slot, bool isReadOnly, int index);
    internal unsafe delegate void GraphicsService_SetShaderTexturesDelegate(IntPtr context, uint commandListId, uint* textureIdList, int textureIdListLength, int slot, int index);
    internal unsafe delegate void GraphicsService_SetShaderIndirectCommandListDelegate(IntPtr context, uint commandListId, uint indirectCommandListId, int slot, int index);
    internal unsafe delegate void GraphicsService_SetShaderIndirectCommandListsDelegate(IntPtr context, uint commandListId, uint* indirectCommandListIdList, int indirectCommandListIdListLength, int slot, int index);
    internal unsafe delegate bool GraphicsService_CreateCopyCommandListDelegate(IntPtr context, uint commandListId, uint commandBufferId, string label);
    internal unsafe delegate void GraphicsService_CommitCopyCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void GraphicsService_UploadDataToGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, byte* data, int dataLength);
    internal unsafe delegate void GraphicsService_CopyGraphicsBufferDataToCpuDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, int length);
    internal unsafe delegate void GraphicsService_ReadGraphicsBufferDataDelegate(IntPtr context, uint graphicsBufferId, byte* data, int dataLength);
    internal unsafe delegate void GraphicsService_UploadDataToTextureDelegate(IntPtr context, uint commandListId, uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, byte* data, int dataLength);
    internal unsafe delegate void GraphicsService_ResetIndirectCommandListDelegate(IntPtr context, uint commandListId, uint indirectCommandListId, int maxCommandCount);
    internal unsafe delegate void GraphicsService_OptimizeIndirectCommandListDelegate(IntPtr context, uint commandListId, uint indirectCommandListId, int maxCommandCount);
    internal unsafe delegate bool GraphicsService_CreateComputeCommandListDelegate(IntPtr context, uint commandListId, uint commandBufferId, string label);
    internal unsafe delegate void GraphicsService_CommitComputeCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate Vector3 GraphicsService_DispatchThreadsDelegate(IntPtr context, uint commandListId, uint threadCountX, uint threadCountY, uint threadCountZ);
    internal unsafe delegate bool GraphicsService_CreateRenderCommandListDelegate(IntPtr context, uint commandListId, uint commandBufferId, GraphicsRenderPassDescriptor renderDescriptor, string label);
    internal unsafe delegate void GraphicsService_CommitRenderCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void GraphicsService_SetPipelineStateDelegate(IntPtr context, uint commandListId, uint pipelineStateId);
    internal unsafe delegate void GraphicsService_SetShaderDelegate(IntPtr context, uint commandListId, uint shaderId);
    internal unsafe delegate void GraphicsService_ExecuteIndirectCommandBufferDelegate(IntPtr context, uint commandListId, uint indirectCommandBufferId, int maxCommandCount);
    internal unsafe delegate void GraphicsService_SetIndexBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId);
    internal unsafe delegate void GraphicsService_DrawIndexedPrimitivesDelegate(IntPtr context, uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
    internal unsafe delegate void GraphicsService_DrawPrimitivesDelegate(IntPtr context, uint commandListId, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);
    internal unsafe delegate void GraphicsService_WaitForCommandListDelegate(IntPtr context, uint commandListId, uint commandListToWaitId);
    internal unsafe delegate void GraphicsService_PresentScreenBufferDelegate(IntPtr context, uint commandBufferId);
    internal unsafe delegate void GraphicsService_WaitForAvailableScreenBufferDelegate(IntPtr context);

    public struct GraphicsService : IGraphicsService
    {
        private IntPtr context { get; }

        private GraphicsService_GetRenderSizeDelegate graphicsService_GetRenderSizeDelegate { get; }
        public unsafe Vector2 GetRenderSize()
        {
            if (this.context != null && this.graphicsService_GetRenderSizeDelegate != null)
                return this.graphicsService_GetRenderSizeDelegate(this.context);

            return default(Vector2);
        }

        private GraphicsService_GetGraphicsAdapterNameDelegate graphicsService_GetGraphicsAdapterNameDelegate { get; }
        public unsafe string GetGraphicsAdapterName()
        {
            if (this.context != null && this.graphicsService_GetGraphicsAdapterNameDelegate != null)
                return this.graphicsService_GetGraphicsAdapterNameDelegate(this.context);

            return string.Empty;
        }

        private GraphicsService_CreateGraphicsBufferDelegate graphicsService_CreateGraphicsBufferDelegate { get; }
        public unsafe bool CreateGraphicsBuffer(uint graphicsBufferId, int length, bool isWriteOnly, string label)
        {
            if (this.context != null && this.graphicsService_CreateGraphicsBufferDelegate != null)
                return this.graphicsService_CreateGraphicsBufferDelegate(this.context, graphicsBufferId, length, isWriteOnly, label);

            return default(bool);
        }

        private GraphicsService_CreateTextureDelegate graphicsService_CreateTextureDelegate { get; }
        public unsafe bool CreateTexture(uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isRenderTarget, string label)
        {
            if (this.context != null && this.graphicsService_CreateTextureDelegate != null)
                return this.graphicsService_CreateTextureDelegate(this.context, textureId, textureFormat, width, height, faceCount, mipLevels, multisampleCount, isRenderTarget, label);

            return default(bool);
        }

        private GraphicsService_DeleteTextureDelegate graphicsService_DeleteTextureDelegate { get; }
        public unsafe void DeleteTexture(uint textureId)
        {
            if (this.context != null && this.graphicsService_DeleteTextureDelegate != null)
                this.graphicsService_DeleteTextureDelegate(this.context, textureId);
        }

        private GraphicsService_CreateIndirectCommandBufferDelegate graphicsService_CreateIndirectCommandBufferDelegate { get; }
        public unsafe bool CreateIndirectCommandBuffer(uint indirectCommandBufferId, int maxCommandCount, string label)
        {
            if (this.context != null && this.graphicsService_CreateIndirectCommandBufferDelegate != null)
                return this.graphicsService_CreateIndirectCommandBufferDelegate(this.context, indirectCommandBufferId, maxCommandCount, label);

            return default(bool);
        }

        private GraphicsService_CreateShaderDelegate graphicsService_CreateShaderDelegate { get; }
        public unsafe bool CreateShader(uint shaderId, string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string label)
        {
            if (this.context != null && this.graphicsService_CreateShaderDelegate != null)
                fixed (byte* shaderByteCodePinned = shaderByteCode)
                    return this.graphicsService_CreateShaderDelegate(this.context, shaderId, computeShaderFunction, shaderByteCodePinned, shaderByteCode.Length, label);

            return default(bool);
        }

        private GraphicsService_DeleteShaderDelegate graphicsService_DeleteShaderDelegate { get; }
        public unsafe void DeleteShader(uint shaderId)
        {
            if (this.context != null && this.graphicsService_DeleteShaderDelegate != null)
                this.graphicsService_DeleteShaderDelegate(this.context, shaderId);
        }

        private GraphicsService_CreatePipelineStateDelegate graphicsService_CreatePipelineStateDelegate { get; }
        public unsafe bool CreatePipelineState(uint pipelineStateId, uint shaderId, GraphicsRenderPassDescriptor renderPassDescriptor, string label)
        {
            if (this.context != null && this.graphicsService_CreatePipelineStateDelegate != null)
                return this.graphicsService_CreatePipelineStateDelegate(this.context, pipelineStateId, shaderId, renderPassDescriptor, label);

            return default(bool);
        }

        private GraphicsService_DeletePipelineStateDelegate graphicsService_DeletePipelineStateDelegate { get; }
        public unsafe void DeletePipelineState(uint pipelineStateId)
        {
            if (this.context != null && this.graphicsService_DeletePipelineStateDelegate != null)
                this.graphicsService_DeletePipelineStateDelegate(this.context, pipelineStateId);
        }

        private GraphicsService_CreateCommandBufferDelegate graphicsService_CreateCommandBufferDelegate { get; }
        public unsafe bool CreateCommandBuffer(uint commandBufferId, string label)
        {
            if (this.context != null && this.graphicsService_CreateCommandBufferDelegate != null)
                return this.graphicsService_CreateCommandBufferDelegate(this.context, commandBufferId, label);

            return default(bool);
        }

        private GraphicsService_DeleteCommandBufferDelegate graphicsService_DeleteCommandBufferDelegate { get; }
        public unsafe void DeleteCommandBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_DeleteCommandBufferDelegate != null)
                this.graphicsService_DeleteCommandBufferDelegate(this.context, commandBufferId);
        }

        private GraphicsService_ResetCommandBufferDelegate graphicsService_ResetCommandBufferDelegate { get; }
        public unsafe void ResetCommandBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_ResetCommandBufferDelegate != null)
                this.graphicsService_ResetCommandBufferDelegate(this.context, commandBufferId);
        }

        private GraphicsService_ExecuteCommandBufferDelegate graphicsService_ExecuteCommandBufferDelegate { get; }
        public unsafe void ExecuteCommandBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_ExecuteCommandBufferDelegate != null)
                this.graphicsService_ExecuteCommandBufferDelegate(this.context, commandBufferId);
        }

        private GraphicsService_GetCommandBufferStatusDelegate graphicsService_GetCommandBufferStatusDelegate { get; }
        public unsafe GraphicsCommandBufferStatus? GetCommandBufferStatus(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_GetCommandBufferStatusDelegate != null)
            {
                var returnedValue = this.graphicsService_GetCommandBufferStatusDelegate(this.context, commandBufferId);
                if (returnedValue.HasValue) return returnedValue.Value;
            }

            return default(GraphicsCommandBufferStatus?);
        }

        private GraphicsService_SetShaderBufferDelegate graphicsService_SetShaderBufferDelegate { get; }
        public unsafe void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, bool isReadOnly, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderBufferDelegate != null)
                this.graphicsService_SetShaderBufferDelegate(this.context, commandListId, graphicsBufferId, slot, isReadOnly, index);
        }

        private GraphicsService_SetShaderBuffersDelegate graphicsService_SetShaderBuffersDelegate { get; }
        public unsafe void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderBuffersDelegate != null)
                fixed (uint* graphicsBufferIdListPinned = graphicsBufferIdList)
                    this.graphicsService_SetShaderBuffersDelegate(this.context, commandListId, graphicsBufferIdListPinned, graphicsBufferIdList.Length, slot, index);
        }

        private GraphicsService_SetShaderTextureDelegate graphicsService_SetShaderTextureDelegate { get; }
        public unsafe void SetShaderTexture(uint commandListId, uint textureId, int slot, bool isReadOnly, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderTextureDelegate != null)
                this.graphicsService_SetShaderTextureDelegate(this.context, commandListId, textureId, slot, isReadOnly, index);
        }

        private GraphicsService_SetShaderTexturesDelegate graphicsService_SetShaderTexturesDelegate { get; }
        public unsafe void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderTexturesDelegate != null)
                fixed (uint* textureIdListPinned = textureIdList)
                    this.graphicsService_SetShaderTexturesDelegate(this.context, commandListId, textureIdListPinned, textureIdList.Length, slot, index);
        }

        private GraphicsService_SetShaderIndirectCommandListDelegate graphicsService_SetShaderIndirectCommandListDelegate { get; }
        public unsafe void SetShaderIndirectCommandList(uint commandListId, uint indirectCommandListId, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderIndirectCommandListDelegate != null)
                this.graphicsService_SetShaderIndirectCommandListDelegate(this.context, commandListId, indirectCommandListId, slot, index);
        }

        private GraphicsService_SetShaderIndirectCommandListsDelegate graphicsService_SetShaderIndirectCommandListsDelegate { get; }
        public unsafe void SetShaderIndirectCommandLists(uint commandListId, ReadOnlySpan<uint> indirectCommandListIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderIndirectCommandListsDelegate != null)
                fixed (uint* indirectCommandListIdListPinned = indirectCommandListIdList)
                    this.graphicsService_SetShaderIndirectCommandListsDelegate(this.context, commandListId, indirectCommandListIdListPinned, indirectCommandListIdList.Length, slot, index);
        }

        private GraphicsService_CreateCopyCommandListDelegate graphicsService_CreateCopyCommandListDelegate { get; }
        public unsafe bool CreateCopyCommandList(uint commandListId, uint commandBufferId, string label)
        {
            if (this.context != null && this.graphicsService_CreateCopyCommandListDelegate != null)
                return this.graphicsService_CreateCopyCommandListDelegate(this.context, commandListId, commandBufferId, label);

            return default(bool);
        }

        private GraphicsService_CommitCopyCommandListDelegate graphicsService_CommitCopyCommandListDelegate { get; }
        public unsafe void CommitCopyCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CommitCopyCommandListDelegate != null)
                this.graphicsService_CommitCopyCommandListDelegate(this.context, commandListId);
        }

        private GraphicsService_UploadDataToGraphicsBufferDelegate graphicsService_UploadDataToGraphicsBufferDelegate { get; }
        public unsafe void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.graphicsService_UploadDataToGraphicsBufferDelegate != null)
                fixed (byte* dataPinned = data)
                    this.graphicsService_UploadDataToGraphicsBufferDelegate(this.context, commandListId, graphicsBufferId, dataPinned, data.Length);
        }

        private GraphicsService_CopyGraphicsBufferDataToCpuDelegate graphicsService_CopyGraphicsBufferDataToCpuDelegate { get; }
        public unsafe void CopyGraphicsBufferDataToCpu(uint commandListId, uint graphicsBufferId, int length)
        {
            if (this.context != null && this.graphicsService_CopyGraphicsBufferDataToCpuDelegate != null)
                this.graphicsService_CopyGraphicsBufferDataToCpuDelegate(this.context, commandListId, graphicsBufferId, length);
        }

        private GraphicsService_ReadGraphicsBufferDataDelegate graphicsService_ReadGraphicsBufferDataDelegate { get; }
        public unsafe void ReadGraphicsBufferData(uint graphicsBufferId, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.graphicsService_ReadGraphicsBufferDataDelegate != null)
                fixed (byte* dataPinned = data)
                    this.graphicsService_ReadGraphicsBufferDataDelegate(this.context, graphicsBufferId, dataPinned, data.Length);
        }

        private GraphicsService_UploadDataToTextureDelegate graphicsService_UploadDataToTextureDelegate { get; }
        public unsafe void UploadDataToTexture(uint commandListId, uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.graphicsService_UploadDataToTextureDelegate != null)
                fixed (byte* dataPinned = data)
                    this.graphicsService_UploadDataToTextureDelegate(this.context, commandListId, textureId, textureFormat, width, height, slice, mipLevel, dataPinned, data.Length);
        }

        private GraphicsService_ResetIndirectCommandListDelegate graphicsService_ResetIndirectCommandListDelegate { get; }
        public unsafe void ResetIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_ResetIndirectCommandListDelegate != null)
                this.graphicsService_ResetIndirectCommandListDelegate(this.context, commandListId, indirectCommandListId, maxCommandCount);
        }

        private GraphicsService_OptimizeIndirectCommandListDelegate graphicsService_OptimizeIndirectCommandListDelegate { get; }
        public unsafe void OptimizeIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_OptimizeIndirectCommandListDelegate != null)
                this.graphicsService_OptimizeIndirectCommandListDelegate(this.context, commandListId, indirectCommandListId, maxCommandCount);
        }

        private GraphicsService_CreateComputeCommandListDelegate graphicsService_CreateComputeCommandListDelegate { get; }
        public unsafe bool CreateComputeCommandList(uint commandListId, uint commandBufferId, string label)
        {
            if (this.context != null && this.graphicsService_CreateComputeCommandListDelegate != null)
                return this.graphicsService_CreateComputeCommandListDelegate(this.context, commandListId, commandBufferId, label);

            return default(bool);
        }

        private GraphicsService_CommitComputeCommandListDelegate graphicsService_CommitComputeCommandListDelegate { get; }
        public unsafe void CommitComputeCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CommitComputeCommandListDelegate != null)
                this.graphicsService_CommitComputeCommandListDelegate(this.context, commandListId);
        }

        private GraphicsService_DispatchThreadsDelegate graphicsService_DispatchThreadsDelegate { get; }
        public unsafe Vector3 DispatchThreads(uint commandListId, uint threadCountX, uint threadCountY, uint threadCountZ)
        {
            if (this.context != null && this.graphicsService_DispatchThreadsDelegate != null)
                return this.graphicsService_DispatchThreadsDelegate(this.context, commandListId, threadCountX, threadCountY, threadCountZ);

            return default(Vector3);
        }

        private GraphicsService_CreateRenderCommandListDelegate graphicsService_CreateRenderCommandListDelegate { get; }
        public unsafe bool CreateRenderCommandList(uint commandListId, uint commandBufferId, GraphicsRenderPassDescriptor renderDescriptor, string label)
        {
            if (this.context != null && this.graphicsService_CreateRenderCommandListDelegate != null)
                return this.graphicsService_CreateRenderCommandListDelegate(this.context, commandListId, commandBufferId, renderDescriptor, label);

            return default(bool);
        }

        private GraphicsService_CommitRenderCommandListDelegate graphicsService_CommitRenderCommandListDelegate { get; }
        public unsafe void CommitRenderCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CommitRenderCommandListDelegate != null)
                this.graphicsService_CommitRenderCommandListDelegate(this.context, commandListId);
        }

        private GraphicsService_SetPipelineStateDelegate graphicsService_SetPipelineStateDelegate { get; }
        public unsafe void SetPipelineState(uint commandListId, uint pipelineStateId)
        {
            if (this.context != null && this.graphicsService_SetPipelineStateDelegate != null)
                this.graphicsService_SetPipelineStateDelegate(this.context, commandListId, pipelineStateId);
        }

        private GraphicsService_SetShaderDelegate graphicsService_SetShaderDelegate { get; }
        public unsafe void SetShader(uint commandListId, uint shaderId)
        {
            if (this.context != null && this.graphicsService_SetShaderDelegate != null)
                this.graphicsService_SetShaderDelegate(this.context, commandListId, shaderId);
        }

        private GraphicsService_ExecuteIndirectCommandBufferDelegate graphicsService_ExecuteIndirectCommandBufferDelegate { get; }
        public unsafe void ExecuteIndirectCommandBuffer(uint commandListId, uint indirectCommandBufferId, int maxCommandCount)
        {
            if (this.context != null && this.graphicsService_ExecuteIndirectCommandBufferDelegate != null)
                this.graphicsService_ExecuteIndirectCommandBufferDelegate(this.context, commandListId, indirectCommandBufferId, maxCommandCount);
        }

        private GraphicsService_SetIndexBufferDelegate graphicsService_SetIndexBufferDelegate { get; }
        public unsafe void SetIndexBuffer(uint commandListId, uint graphicsBufferId)
        {
            if (this.context != null && this.graphicsService_SetIndexBufferDelegate != null)
                this.graphicsService_SetIndexBufferDelegate(this.context, commandListId, graphicsBufferId);
        }

        private GraphicsService_DrawIndexedPrimitivesDelegate graphicsService_DrawIndexedPrimitivesDelegate { get; }
        public unsafe void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (this.context != null && this.graphicsService_DrawIndexedPrimitivesDelegate != null)
                this.graphicsService_DrawIndexedPrimitivesDelegate(this.context, commandListId, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId);
        }

        private GraphicsService_DrawPrimitivesDelegate graphicsService_DrawPrimitivesDelegate { get; }
        public unsafe void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount)
        {
            if (this.context != null && this.graphicsService_DrawPrimitivesDelegate != null)
                this.graphicsService_DrawPrimitivesDelegate(this.context, commandListId, primitiveType, startVertex, vertexCount);
        }

        private GraphicsService_WaitForCommandListDelegate graphicsService_WaitForCommandListDelegate { get; }
        public unsafe void WaitForCommandList(uint commandListId, uint commandListToWaitId)
        {
            if (this.context != null && this.graphicsService_WaitForCommandListDelegate != null)
                this.graphicsService_WaitForCommandListDelegate(this.context, commandListId, commandListToWaitId);
        }

        private GraphicsService_PresentScreenBufferDelegate graphicsService_PresentScreenBufferDelegate { get; }
        public unsafe void PresentScreenBuffer(uint commandBufferId)
        {
            if (this.context != null && this.graphicsService_PresentScreenBufferDelegate != null)
                this.graphicsService_PresentScreenBufferDelegate(this.context, commandBufferId);
        }

        private GraphicsService_WaitForAvailableScreenBufferDelegate graphicsService_WaitForAvailableScreenBufferDelegate { get; }
        public unsafe void WaitForAvailableScreenBuffer()
        {
            if (this.context != null && this.graphicsService_WaitForAvailableScreenBufferDelegate != null)
                this.graphicsService_WaitForAvailableScreenBufferDelegate(this.context);
        }
    }

    public struct NullableGraphicsCommandBufferStatus
    {
        public bool HasValue { get; }
        public GraphicsCommandBufferStatus Value { get; }
    }
}

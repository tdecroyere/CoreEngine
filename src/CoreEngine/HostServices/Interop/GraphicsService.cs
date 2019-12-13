using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate Vector2 GraphicsService_GetRenderSizeDelegate(IntPtr context);
    internal unsafe delegate bool GraphicsService_CreateGraphicsBufferDelegate(IntPtr context, uint graphicsBufferId, int length);
    internal unsafe delegate bool GraphicsService_CreateTextureDelegate(IntPtr context, uint textureId, int width, int height);
    internal unsafe delegate bool GraphicsService_CreateShaderDelegate(IntPtr context, uint shaderId, byte *shaderByteCode, int shaderByteCodeLength);
    internal unsafe delegate void GraphicsService_RemoveShaderDelegate(IntPtr context, uint shaderId);
    internal unsafe delegate bool GraphicsService_CreateCopyCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void GraphicsService_ExecuteCopyCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void GraphicsService_UploadDataToGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, byte *data, int dataLength);
    internal unsafe delegate void GraphicsService_UploadDataToTextureDelegate(IntPtr context, uint commandListId, uint textureId, int width, int height, byte *data, int dataLength);
    internal unsafe delegate bool GraphicsService_CreateRenderCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void GraphicsService_ExecuteRenderCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void GraphicsService_SetShaderDelegate(IntPtr context, uint commandListId, uint shaderId);
    internal unsafe delegate void GraphicsService_SetShaderBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, int slot, int index);
    internal unsafe delegate void GraphicsService_SetShaderBuffersDelegate(IntPtr context, uint commandListId, uint *graphicsBufferIdList, int graphicsBufferIdListLength, int slot, int index);
    internal unsafe delegate void GraphicsService_SetShaderTextureDelegate(IntPtr context, uint commandListId, uint textureId, int slot, int index);
    internal unsafe delegate void GraphicsService_SetShaderTexturesDelegate(IntPtr context, uint commandListId, uint *textureIdList, int textureIdListLength, int slot, int index);
    internal unsafe delegate void GraphicsService_SetIndexBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId);
    internal unsafe delegate void GraphicsService_DrawIndexedPrimitivesDelegate(IntPtr context, uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
    internal unsafe delegate void GraphicsService_PresentScreenBufferDelegate(IntPtr context);
    public struct GraphicsService : IGraphicsService
    {
        private IntPtr context
        {
            get;
        }

        private GraphicsService_GetRenderSizeDelegate graphicsService_GetRenderSizeDelegate
        {
            get;
        }

        public unsafe Vector2 GetRenderSize()
        {
            if (this.context != null && this.graphicsService_GetRenderSizeDelegate != null)
                return this.graphicsService_GetRenderSizeDelegate(this.context);
            else
                return default(Vector2);
        }

        private GraphicsService_CreateGraphicsBufferDelegate graphicsService_CreateGraphicsBufferDelegate
        {
            get;
        }

        public unsafe bool CreateGraphicsBuffer(uint graphicsBufferId, int length)
        {
            if (this.context != null && this.graphicsService_CreateGraphicsBufferDelegate != null)
                return this.graphicsService_CreateGraphicsBufferDelegate(this.context, graphicsBufferId, length);
            else
                return default(bool);
        }

        private GraphicsService_CreateTextureDelegate graphicsService_CreateTextureDelegate
        {
            get;
        }

        public unsafe bool CreateTexture(uint textureId, int width, int height)
        {
            if (this.context != null && this.graphicsService_CreateTextureDelegate != null)
                return this.graphicsService_CreateTextureDelegate(this.context, textureId, width, height);
            else
                return default(bool);
        }

        private GraphicsService_CreateShaderDelegate graphicsService_CreateShaderDelegate
        {
            get;
        }

        public unsafe bool CreateShader(uint shaderId, ReadOnlySpan<byte> shaderByteCode)
        {
            if (this.context != null && this.graphicsService_CreateShaderDelegate != null)
                fixed (byte *shaderByteCodePinned = shaderByteCode)
                    return this.graphicsService_CreateShaderDelegate(this.context, shaderId, shaderByteCodePinned, shaderByteCode.Length);
            else
                return default(bool);
        }

        private GraphicsService_RemoveShaderDelegate graphicsService_RemoveShaderDelegate
        {
            get;
        }

        public unsafe void RemoveShader(uint shaderId)
        {
            if (this.context != null && this.graphicsService_RemoveShaderDelegate != null)
                this.graphicsService_RemoveShaderDelegate(this.context, shaderId);
        }

        private GraphicsService_CreateCopyCommandListDelegate graphicsService_CreateCopyCommandListDelegate
        {
            get;
        }

        public unsafe bool CreateCopyCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CreateCopyCommandListDelegate != null)
                return this.graphicsService_CreateCopyCommandListDelegate(this.context, commandListId);
            else
                return default(bool);
        }

        private GraphicsService_ExecuteCopyCommandListDelegate graphicsService_ExecuteCopyCommandListDelegate
        {
            get;
        }

        public unsafe void ExecuteCopyCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_ExecuteCopyCommandListDelegate != null)
                this.graphicsService_ExecuteCopyCommandListDelegate(this.context, commandListId);
        }

        private GraphicsService_UploadDataToGraphicsBufferDelegate graphicsService_UploadDataToGraphicsBufferDelegate
        {
            get;
        }

        public unsafe void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.graphicsService_UploadDataToGraphicsBufferDelegate != null)
                fixed (byte *dataPinned = data)
                    this.graphicsService_UploadDataToGraphicsBufferDelegate(this.context, commandListId, graphicsBufferId, dataPinned, data.Length);
        }

        private GraphicsService_UploadDataToTextureDelegate graphicsService_UploadDataToTextureDelegate
        {
            get;
        }

        public unsafe void UploadDataToTexture(uint commandListId, uint textureId, int width, int height, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.graphicsService_UploadDataToTextureDelegate != null)
                fixed (byte *dataPinned = data)
                    this.graphicsService_UploadDataToTextureDelegate(this.context, commandListId, textureId, width, height, dataPinned, data.Length);
        }

        private GraphicsService_CreateRenderCommandListDelegate graphicsService_CreateRenderCommandListDelegate
        {
            get;
        }

        public unsafe bool CreateRenderCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_CreateRenderCommandListDelegate != null)
                return this.graphicsService_CreateRenderCommandListDelegate(this.context, commandListId);
            else
                return default(bool);
        }

        private GraphicsService_ExecuteRenderCommandListDelegate graphicsService_ExecuteRenderCommandListDelegate
        {
            get;
        }

        public unsafe void ExecuteRenderCommandList(uint commandListId)
        {
            if (this.context != null && this.graphicsService_ExecuteRenderCommandListDelegate != null)
                this.graphicsService_ExecuteRenderCommandListDelegate(this.context, commandListId);
        }

        private GraphicsService_SetShaderDelegate graphicsService_SetShaderDelegate
        {
            get;
        }

        public unsafe void SetShader(uint commandListId, uint shaderId)
        {
            if (this.context != null && this.graphicsService_SetShaderDelegate != null)
                this.graphicsService_SetShaderDelegate(this.context, commandListId, shaderId);
        }

        private GraphicsService_SetShaderBufferDelegate graphicsService_SetShaderBufferDelegate
        {
            get;
        }

        public unsafe void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderBufferDelegate != null)
                this.graphicsService_SetShaderBufferDelegate(this.context, commandListId, graphicsBufferId, slot, index);
        }

        private GraphicsService_SetShaderBuffersDelegate graphicsService_SetShaderBuffersDelegate
        {
            get;
        }

        public unsafe void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderBuffersDelegate != null)
                fixed (uint *graphicsBufferIdListPinned = graphicsBufferIdList)
                    this.graphicsService_SetShaderBuffersDelegate(this.context, commandListId, graphicsBufferIdListPinned, graphicsBufferIdList.Length, slot, index);
        }

        private GraphicsService_SetShaderTextureDelegate graphicsService_SetShaderTextureDelegate
        {
            get;
        }

        public unsafe void SetShaderTexture(uint commandListId, uint textureId, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderTextureDelegate != null)
                this.graphicsService_SetShaderTextureDelegate(this.context, commandListId, textureId, slot, index);
        }

        private GraphicsService_SetShaderTexturesDelegate graphicsService_SetShaderTexturesDelegate
        {
            get;
        }

        public unsafe void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index)
        {
            if (this.context != null && this.graphicsService_SetShaderTexturesDelegate != null)
                fixed (uint *textureIdListPinned = textureIdList)
                    this.graphicsService_SetShaderTexturesDelegate(this.context, commandListId, textureIdListPinned, textureIdList.Length, slot, index);
        }

        private GraphicsService_SetIndexBufferDelegate graphicsService_SetIndexBufferDelegate
        {
            get;
        }

        public unsafe void SetIndexBuffer(uint commandListId, uint graphicsBufferId)
        {
            if (this.context != null && this.graphicsService_SetIndexBufferDelegate != null)
                this.graphicsService_SetIndexBufferDelegate(this.context, commandListId, graphicsBufferId);
        }

        private GraphicsService_DrawIndexedPrimitivesDelegate graphicsService_DrawIndexedPrimitivesDelegate
        {
            get;
        }

        public unsafe void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (this.context != null && this.graphicsService_DrawIndexedPrimitivesDelegate != null)
                this.graphicsService_DrawIndexedPrimitivesDelegate(this.context, commandListId, primitiveType, startIndex, indexCount, instanceCount, baseInstanceId);
        }

        private GraphicsService_PresentScreenBufferDelegate graphicsService_PresentScreenBufferDelegate
        {
            get;
        }

        public unsafe void PresentScreenBuffer()
        {
            if (this.context != null && this.graphicsService_PresentScreenBufferDelegate != null)
                this.graphicsService_PresentScreenBufferDelegate(this.context);
        }
    }
}
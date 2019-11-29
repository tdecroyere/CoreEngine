using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate Vector2 GetRenderSizeDelegate(IntPtr context);
    internal unsafe delegate uint CreateShaderDelegate(IntPtr context, byte *shaderByteCode, int shaderByteCodeLength);
    internal unsafe delegate uint CreateShaderParametersDelegate(IntPtr context, uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3);
    internal unsafe delegate uint CreateGraphicsBufferDelegate(IntPtr context, int length);
    internal unsafe delegate uint CreateCopyCommandListDelegate(IntPtr context);
    internal unsafe delegate void ExecuteCopyCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void UploadDataToGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, byte *data, int dataLength);
    internal unsafe delegate uint CreateRenderCommandListDelegate(IntPtr context);
    internal unsafe delegate void ExecuteRenderCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void DrawPrimitivesDelegate(IntPtr context, uint commandListId, GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId);
    public struct GraphicsService : IGraphicsService
    {
        private IntPtr context
        {
            get;
        }

        private GetRenderSizeDelegate getRenderSizeDelegate
        {
            get;
        }

        public unsafe Vector2 GetRenderSize()
        {
            if (this.context != null && this.getRenderSizeDelegate != null)
                return this.getRenderSizeDelegate(this.context);
            else
                return default(Vector2);
        }

        private CreateShaderDelegate createShaderDelegate
        {
            get;
        }

        public unsafe uint CreateShader(ReadOnlySpan<byte> shaderByteCode)
        {
            if (this.context != null && this.createShaderDelegate != null)
                fixed (byte *shaderByteCodePinned = shaderByteCode)
                    return this.createShaderDelegate(this.context, shaderByteCodePinned, shaderByteCode.Length);
            else
                return default(uint);
        }

        private CreateShaderParametersDelegate createShaderParametersDelegate
        {
            get;
        }

        public unsafe uint CreateShaderParameters(uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3)
        {
            if (this.context != null && this.createShaderParametersDelegate != null)
                return this.createShaderParametersDelegate(this.context, graphicsBuffer1, graphicsBuffer2, graphicsBuffer3);
            else
                return default(uint);
        }

        private CreateGraphicsBufferDelegate createGraphicsBufferDelegate
        {
            get;
        }

        public unsafe uint CreateGraphicsBuffer(int length)
        {
            if (this.context != null && this.createGraphicsBufferDelegate != null)
                return this.createGraphicsBufferDelegate(this.context, length);
            else
                return default(uint);
        }

        private CreateCopyCommandListDelegate createCopyCommandListDelegate
        {
            get;
        }

        public unsafe uint CreateCopyCommandList()
        {
            if (this.context != null && this.createCopyCommandListDelegate != null)
                return this.createCopyCommandListDelegate(this.context);
            else
                return default(uint);
        }

        private ExecuteCopyCommandListDelegate executeCopyCommandListDelegate
        {
            get;
        }

        public unsafe void ExecuteCopyCommandList(uint commandListId)
        {
            if (this.context != null && this.executeCopyCommandListDelegate != null)
                this.executeCopyCommandListDelegate(this.context, commandListId);
        }

        private UploadDataToGraphicsBufferDelegate uploadDataToGraphicsBufferDelegate
        {
            get;
        }

        public unsafe void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.uploadDataToGraphicsBufferDelegate != null)
                fixed (byte *dataPinned = data)
                    this.uploadDataToGraphicsBufferDelegate(this.context, commandListId, graphicsBufferId, dataPinned, data.Length);
        }

        private CreateRenderCommandListDelegate createRenderCommandListDelegate
        {
            get;
        }

        public unsafe uint CreateRenderCommandList()
        {
            if (this.context != null && this.createRenderCommandListDelegate != null)
                return this.createRenderCommandListDelegate(this.context);
            else
                return default(uint);
        }

        private ExecuteRenderCommandListDelegate executeRenderCommandListDelegate
        {
            get;
        }

        public unsafe void ExecuteRenderCommandList(uint commandListId)
        {
            if (this.context != null && this.executeRenderCommandListDelegate != null)
                this.executeRenderCommandListDelegate(this.context, commandListId);
        }

        private DrawPrimitivesDelegate drawPrimitivesDelegate
        {
            get;
        }

        public unsafe void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId)
        {
            if (this.context != null && this.drawPrimitivesDelegate != null)
                this.drawPrimitivesDelegate(this.context, commandListId, primitiveType, startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId);
        }
    }
}
using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate Vector2 GetRenderSizeDelegate(IntPtr context);
    internal unsafe delegate uint CreateShaderDelegate(IntPtr context, byte *shaderByteCode, int shaderByteCodeLength);
    internal unsafe delegate uint CreateShaderParametersDelegate(IntPtr context, uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3);
    internal unsafe delegate uint CreateStaticGraphicsBufferDelegate(IntPtr context, byte *data, int dataLength);
    internal unsafe delegate uint CreateDynamicGraphicsBufferDelegate(IntPtr context, int length);
    internal unsafe delegate void UploadDataToGraphicsBufferDelegate(IntPtr context, uint graphicsBufferId, byte *data, int dataLength);
    internal unsafe delegate void BeginCopyGpuDataDelegate(IntPtr context);
    internal unsafe delegate void EndCopyGpuDataDelegate(IntPtr context);
    internal unsafe delegate void BeginRenderDelegate(IntPtr context);
    internal unsafe delegate void EndRenderDelegate(IntPtr context);
    internal unsafe delegate void DrawPrimitivesDelegate(IntPtr context, GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId);
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

        private CreateStaticGraphicsBufferDelegate createStaticGraphicsBufferDelegate
        {
            get;
        }

        public unsafe uint CreateStaticGraphicsBuffer(ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.createStaticGraphicsBufferDelegate != null)
                fixed (byte *dataPinned = data)
                    return this.createStaticGraphicsBufferDelegate(this.context, dataPinned, data.Length);
            else
                return default(uint);
        }

        private CreateDynamicGraphicsBufferDelegate createDynamicGraphicsBufferDelegate
        {
            get;
        }

        public unsafe uint CreateDynamicGraphicsBuffer(int length)
        {
            if (this.context != null && this.createDynamicGraphicsBufferDelegate != null)
                return this.createDynamicGraphicsBufferDelegate(this.context, length);
            else
                return default(uint);
        }

        private UploadDataToGraphicsBufferDelegate uploadDataToGraphicsBufferDelegate
        {
            get;
        }

        public unsafe void UploadDataToGraphicsBuffer(uint graphicsBufferId, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.uploadDataToGraphicsBufferDelegate != null)
                fixed (byte *dataPinned = data)
                    this.uploadDataToGraphicsBufferDelegate(this.context, graphicsBufferId, dataPinned, data.Length);
        }

        private BeginCopyGpuDataDelegate beginCopyGpuDataDelegate
        {
            get;
        }

        public unsafe void BeginCopyGpuData()
        {
            if (this.context != null && this.beginCopyGpuDataDelegate != null)
                this.beginCopyGpuDataDelegate(this.context);
        }

        private EndCopyGpuDataDelegate endCopyGpuDataDelegate
        {
            get;
        }

        public unsafe void EndCopyGpuData()
        {
            if (this.context != null && this.endCopyGpuDataDelegate != null)
                this.endCopyGpuDataDelegate(this.context);
        }

        private BeginRenderDelegate beginRenderDelegate
        {
            get;
        }

        public unsafe void BeginRender()
        {
            if (this.context != null && this.beginRenderDelegate != null)
                this.beginRenderDelegate(this.context);
        }

        private EndRenderDelegate endRenderDelegate
        {
            get;
        }

        public unsafe void EndRender()
        {
            if (this.context != null && this.endRenderDelegate != null)
                this.endRenderDelegate(this.context);
        }

        private DrawPrimitivesDelegate drawPrimitivesDelegate
        {
            get;
        }

        public unsafe void DrawPrimitives(GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId)
        {
            if (this.context != null && this.drawPrimitivesDelegate != null)
                this.drawPrimitivesDelegate(this.context, primitiveType, startIndex, indexCount, vertexBufferId, indexBufferId, baseInstanceId);
        }
    }
}
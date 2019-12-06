using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate Vector2 GetRenderSizeDelegate(IntPtr context);
    internal unsafe delegate uint CreatePipelineStateDelegate(IntPtr context, byte *shaderByteCode, int shaderByteCodeLength);
    internal unsafe delegate void RemovePipelineStateDelegate(IntPtr context, uint pipelineStateId);
    internal unsafe delegate uint CreateShaderParametersDelegate(IntPtr context, uint pipelineStateId, uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3);
    internal unsafe delegate uint CreateGraphicsBufferDelegate(IntPtr context, int length);
    internal unsafe delegate uint CreateCopyCommandListDelegate(IntPtr context);
    internal unsafe delegate void ExecuteCopyCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void UploadDataToGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, byte *data, int dataLength);
    internal unsafe delegate uint CreateRenderCommandListDelegate(IntPtr context);
    internal unsafe delegate void ExecuteRenderCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void SetPipelineStateDelegate(IntPtr context, uint commandListId, uint pipelineStateId);
    internal unsafe delegate void SetGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, GraphicsBindStage graphicsBindStage, uint slot);
    internal unsafe delegate void DrawPrimitivesDelegate(IntPtr context, uint commandListId, GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId);
    internal unsafe delegate void PresentScreenBufferDelegate(IntPtr context);
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

        private CreatePipelineStateDelegate createPipelineStateDelegate
        {
            get;
        }

        public unsafe uint CreatePipelineState(ReadOnlySpan<byte> shaderByteCode)
        {
            if (this.context != null && this.createPipelineStateDelegate != null)
                fixed (byte *shaderByteCodePinned = shaderByteCode)
                    return this.createPipelineStateDelegate(this.context, shaderByteCodePinned, shaderByteCode.Length);
            else
                return default(uint);
        }

        private RemovePipelineStateDelegate removePipelineStateDelegate
        {
            get;
        }

        public unsafe void RemovePipelineState(uint pipelineStateId)
        {
            if (this.context != null && this.removePipelineStateDelegate != null)
                this.removePipelineStateDelegate(this.context, pipelineStateId);
        }

        private CreateShaderParametersDelegate createShaderParametersDelegate
        {
            get;
        }

        public unsafe uint CreateShaderParameters(uint pipelineStateId, uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3)
        {
            if (this.context != null && this.createShaderParametersDelegate != null)
                return this.createShaderParametersDelegate(this.context, pipelineStateId, graphicsBuffer1, graphicsBuffer2, graphicsBuffer3);
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

        private SetPipelineStateDelegate setPipelineStateDelegate
        {
            get;
        }

        public unsafe void SetPipelineState(uint commandListId, uint pipelineStateId)
        {
            if (this.context != null && this.setPipelineStateDelegate != null)
                this.setPipelineStateDelegate(this.context, commandListId, pipelineStateId);
        }

        private SetGraphicsBufferDelegate setGraphicsBufferDelegate
        {
            get;
        }

        public unsafe void SetGraphicsBuffer(uint commandListId, uint graphicsBufferId, GraphicsBindStage graphicsBindStage, uint slot)
        {
            if (this.context != null && this.setGraphicsBufferDelegate != null)
                this.setGraphicsBufferDelegate(this.context, commandListId, graphicsBufferId, graphicsBindStage, slot);
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

        private PresentScreenBufferDelegate presentScreenBufferDelegate
        {
            get;
        }

        public unsafe void PresentScreenBuffer()
        {
            if (this.context != null && this.presentScreenBufferDelegate != null)
                this.presentScreenBufferDelegate(this.context);
        }
    }
}
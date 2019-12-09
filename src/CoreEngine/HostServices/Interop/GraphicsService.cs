using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate Vector2 GetRenderSizeDelegate(IntPtr context);
    internal unsafe delegate bool CreateGraphicsBufferDelegate(IntPtr context, uint graphicsResourceId, int length);
    internal unsafe delegate bool CreateTextureDelegate(IntPtr context, uint graphicsResourceId, int width, int height);
    internal unsafe delegate uint CreatePipelineStateDelegate(IntPtr context, byte *shaderByteCode, int shaderByteCodeLength);
    internal unsafe delegate void RemovePipelineStateDelegate(IntPtr context, uint pipelineStateId);
    internal unsafe delegate bool CreateShaderParametersDelegate(IntPtr context, uint graphicsResourceId, uint pipelineStateId, GraphicsShaderParameterDescriptor*parameters, int parametersLength);
    internal unsafe delegate uint CreateCopyCommandListDelegate(IntPtr context);
    internal unsafe delegate void ExecuteCopyCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void UploadDataToGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, byte *data, int dataLength);
    internal unsafe delegate void UploadDataToTextureDelegate(IntPtr context, uint commandListId, uint textureId, int width, int height, byte *data, int dataLength);
    internal unsafe delegate uint CreateRenderCommandListDelegate(IntPtr context);
    internal unsafe delegate void ExecuteRenderCommandListDelegate(IntPtr context, uint commandListId);
    internal unsafe delegate void SetPipelineStateDelegate(IntPtr context, uint commandListId, uint pipelineStateId);
    internal unsafe delegate void SetGraphicsBufferDelegate(IntPtr context, uint commandListId, uint graphicsBufferId, GraphicsBindStage graphicsBindStage, uint slot);
    internal unsafe delegate void SetTextureDelegate(IntPtr context, uint commandListId, uint textureId, GraphicsBindStage graphicsBindStage, uint slot);
    internal unsafe delegate void DrawPrimitivesDelegate(IntPtr context, uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, uint vertexBufferId, uint indexBufferId, int instanceCount, int baseInstanceId);
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

        private CreateGraphicsBufferDelegate createGraphicsBufferDelegate
        {
            get;
        }

        public unsafe bool CreateGraphicsBuffer(uint graphicsResourceId, int length)
        {
            if (this.context != null && this.createGraphicsBufferDelegate != null)
                return this.createGraphicsBufferDelegate(this.context, graphicsResourceId, length);
            else
                return default(bool);
        }

        private CreateTextureDelegate createTextureDelegate
        {
            get;
        }

        public unsafe bool CreateTexture(uint graphicsResourceId, int width, int height)
        {
            if (this.context != null && this.createTextureDelegate != null)
                return this.createTextureDelegate(this.context, graphicsResourceId, width, height);
            else
                return default(bool);
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

        public unsafe bool CreateShaderParameters(uint graphicsResourceId, uint pipelineStateId, ReadOnlySpan<GraphicsShaderParameterDescriptor> parameters)
        {
            if (this.context != null && this.createShaderParametersDelegate != null)
                fixed (GraphicsShaderParameterDescriptor*parametersPinned = parameters)
                    return this.createShaderParametersDelegate(this.context, graphicsResourceId, pipelineStateId, parametersPinned, parameters.Length);
            else
                return default(bool);
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

        private UploadDataToTextureDelegate uploadDataToTextureDelegate
        {
            get;
        }

        public unsafe void UploadDataToTexture(uint commandListId, uint textureId, int width, int height, ReadOnlySpan<byte> data)
        {
            if (this.context != null && this.uploadDataToTextureDelegate != null)
                fixed (byte *dataPinned = data)
                    this.uploadDataToTextureDelegate(this.context, commandListId, textureId, width, height, dataPinned, data.Length);
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

        private SetTextureDelegate setTextureDelegate
        {
            get;
        }

        public unsafe void SetTexture(uint commandListId, uint textureId, GraphicsBindStage graphicsBindStage, uint slot)
        {
            if (this.context != null && this.setTextureDelegate != null)
                this.setTextureDelegate(this.context, commandListId, textureId, graphicsBindStage, slot);
        }

        private DrawPrimitivesDelegate drawPrimitivesDelegate
        {
            get;
        }

        public unsafe void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, uint vertexBufferId, uint indexBufferId, int instanceCount, int baseInstanceId)
        {
            if (this.context != null && this.drawPrimitivesDelegate != null)
                this.drawPrimitivesDelegate(this.context, commandListId, primitiveType, startIndex, indexCount, vertexBufferId, indexBufferId, instanceCount, baseInstanceId);
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
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.HostServices
{
    public enum GraphicsBindStage
    {
        Vertex,
        Pixel
    }

    public enum GraphicsPrimitiveType
    {
        Triangle,
        Line
    }

    public enum GraphicsShaderParameterType
    {
        Buffer,
        Texture,
        TextureArray
    }

    public readonly struct GraphicsShaderParameterDescriptor
    {
        public GraphicsShaderParameterDescriptor(int resourceIdCount, GraphicsShaderParameterType parameterType, uint slot)
        {
            this.ResourceIdCount = resourceIdCount;
            this.ParameterType = parameterType;
            this.Slot = slot;
        }

        public readonly int ResourceIdCount { get; }
        public readonly GraphicsShaderParameterType ParameterType { get; }
        public readonly uint Slot { get; }
    }

    public interface IGraphicsService
    {
        Vector2 GetRenderSize();
        
        bool CreateGraphicsBuffer(uint graphicsResourceId, int length);
        bool CreateTexture(uint graphicsResourceId, int width, int height);

        uint CreatePipelineState(ReadOnlySpan<byte> shaderByteCode);
        void RemovePipelineState(uint pipelineStateId);
        bool CreateShaderParameters(uint graphicsResourceId, uint pipelineStateId, uint slot, ReadOnlySpan<uint> graphicsResourceIdList, ReadOnlySpan<GraphicsShaderParameterDescriptor> parameters);
        
        uint CreateCopyCommandList();
        void ExecuteCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        void UploadDataToTexture(uint commandListId, uint textureId, int width, int height, ReadOnlySpan<byte> data);
        
        uint CreateRenderCommandList();
        void ExecuteRenderCommandList(uint commandListId);
        void SetPipelineState(uint commandListId, uint pipelineStateId);
        void SetGraphicsBuffer(uint commandListId, uint graphicsBufferId, GraphicsBindStage graphicsBindStage, uint slot);
        void SetTexture(uint commandListId, uint textureId, GraphicsBindStage graphicsBindStage, uint slot);
        void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, uint vertexBufferId, uint indexBufferId, int instanceCount, int baseInstanceId);

        void PresentScreenBuffer();
    }
}
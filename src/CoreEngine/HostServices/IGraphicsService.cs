using System;
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

    public interface IGraphicsService
    {
        Vector2 GetRenderSize();
        
        uint CreatePipelineState(ReadOnlySpan<byte> shaderByteCode);
        void RemovePipelineState(uint pipelineStateId);
        uint CreateShaderParameters(uint pipelineStateId, uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3);
        uint CreateGraphicsBuffer(int length);
        uint CreateTexture(int width, int height);
        
        uint CreateCopyCommandList();
        void ExecuteCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        void UploadDataToTexture(uint commandListId, uint textureId, int width, int height, ReadOnlySpan<byte> data);
        
        uint CreateRenderCommandList();
        void ExecuteRenderCommandList(uint commandListId);
        void SetPipelineState(uint commandListId, uint pipelineStateId);
        void SetGraphicsBuffer(uint commandListId, uint graphicsBufferId, GraphicsBindStage graphicsBindStage, uint slot);
        void SetTexture(uint commandListId, uint textureId, GraphicsBindStage graphicsBindStage, uint slot);
        void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId);

        void PresentScreenBuffer();
    }
}
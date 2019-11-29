using System;
using System.Numerics;

namespace CoreEngine.HostServices
{
    public enum GraphicsPrimitiveType
    {
        Triangle,
        Line
    }

    public interface IGraphicsService
    {
        Vector2 GetRenderSize();
        
        uint CreateShader(ReadOnlySpan<byte> shaderByteCode);
        uint CreateShaderParameters(uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3);
        uint CreateGraphicsBuffer(int length);
        
        uint CreateCopyCommandList();
        void ExecuteCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        
        uint CreateRenderCommandList();
        void ExecuteRenderCommandList(uint commandListId);
        void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId);
    }
}
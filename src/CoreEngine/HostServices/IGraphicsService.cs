using System;
using System.Numerics;

namespace CoreEngine.HostServices
{
    public interface IGraphicsService
    {
        Vector2 GetRenderSize();
        uint CreateShader(ReadOnlySpan<byte> shaderByteCode);
        uint CreateShaderParameters(uint graphicsBuffer1, uint graphicsBuffer2, uint graphicsBuffer3);
        uint CreateStaticGraphicsBuffer(ReadOnlySpan<byte> data);
        uint CreateDynamicGraphicsBuffer(int length);

        void UploadDataToGraphicsBuffer(uint graphicsBufferId, ReadOnlySpan<byte> data);
        void BeginCopyGpuData();
        void EndCopyGpuData();
        void BeginRender();
        void EndRender();
        void DrawPrimitives(uint startIndex, uint indexCount, uint vertexBufferId, uint indexBufferId, uint baseInstanceId);
    }
}
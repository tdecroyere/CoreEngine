using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.HostServices
{
    // TODO: Avoid the duplication of structs and enums

    public enum GraphicsPrimitiveType
    {
        Triangle,
        Line
    }

    public interface IGraphicsService
    {
        Vector2 GetRenderSize();
        
        bool CreateGraphicsBuffer(uint graphicsBufferId, int length, string? debugName);
        bool CreateTexture(uint textureId, int width, int height, string? debugName);

        bool CreateShader(uint shaderId, ReadOnlySpan<byte> shaderByteCode, string? debugName);
        void RemoveShader(uint shaderId);
        
        bool CreateCopyCommandList(uint commandListId, string? debugName, bool createNewCommandBuffer);
        void ExecuteCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        void UploadDataToTexture(uint commandListId, uint textureId, int width, int height, ReadOnlySpan<byte> data);
        
        bool CreateRenderCommandList(uint commandListId, string? debugName, bool createNewCommandBuffer);
        void ExecuteRenderCommandList(uint commandListId);

        void SetShader(uint commandListId, uint shaderId);
        void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, int index);
        void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index);
        void SetShaderTexture(uint commandListId, uint textureId, int slot, int index);
        void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index);

        void SetIndexBuffer(uint commandListId, uint graphicsBufferId);
        void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
        void PresentScreenBuffer();
    }
}
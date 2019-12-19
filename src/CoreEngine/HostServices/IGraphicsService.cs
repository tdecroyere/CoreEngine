using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Graphics;

namespace CoreEngine.HostServices
{
    // TODO: Avoid the duplication of structs and enums

    public enum GraphicsPrimitiveType
    {
        Triangle,
        Line
    }

    public enum GraphicsTextureFormat
    {
        Rgba8UnormSrgb,
        Bgra8UnormSrgb,
        Depth32Float
    }

    public readonly struct GraphicsRenderPassDescriptor
    {
        public GraphicsRenderPassDescriptor(RenderPassDescriptor renderPassDescriptor)
        {
            this.ColorTextureId = renderPassDescriptor.ColorTexture.GraphicsResourceId;
            this.ClearColor = renderPassDescriptor.ClearColor;
            this.DepthTextureId = renderPassDescriptor.DepthTexture?.GraphicsResourceId;
            this.DepthCompare = renderPassDescriptor.DepthCompare;
            this.DepthWrite = renderPassDescriptor.DepthWrite;
            this.BackfaceCulling = renderPassDescriptor.BackfaceCulling;
        }
        
        public GraphicsRenderPassDescriptor(Texture? colorTexture, Vector4? clearColor, Texture? depthTexture, bool depthCompare, bool depthWrite, bool backfaceCulling)
        {
            this.ColorTextureId = colorTexture?.GraphicsResourceId;
            this.ClearColor = clearColor;
            this.DepthTextureId = depthTexture?.GraphicsResourceId;
            this.DepthCompare = depthCompare;
            this.DepthWrite = depthWrite;
            this.BackfaceCulling = backfaceCulling;
        }

        public readonly uint? ColorTextureId { get; }
        public readonly Vector4? ClearColor { get; }
        public readonly uint? DepthTextureId { get; }
        public readonly bool DepthCompare { get; }
        public readonly bool DepthWrite { get; }
        public readonly bool BackfaceCulling { get; }
    }

    public interface IGraphicsService
    {
        Vector2 GetRenderSize();
        
        bool CreateGraphicsBuffer(uint graphicsBufferId, int length, string? debugName);

        bool CreateTexture(uint textureId, GraphicsTextureFormat textureFormat, int width, int height, bool isRenderTarget, string? debugName);
        void RemoveTexture(uint textureId);

        bool CreateShader(uint shaderId, string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, bool useDepthBuffer, string? debugName);
        void RemoveShader(uint shaderId);
        
        bool CreateCopyCommandList(uint commandListId, string? debugName, bool createNewCommandBuffer);
        void ExecuteCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        void UploadDataToTexture(uint commandListId, uint textureId, int width, int height, ReadOnlySpan<byte> data);
        void ResetIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);
        void OptimizeIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);

        bool CreateComputeCommandList(uint commandListId, string? debugName, bool createNewCommandBuffer);
        void ExecuteComputeCommandList(uint commandListId);
        void DispatchThreadGroups(uint commandListId, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);
        
        bool CreateRenderCommandList(uint commandListId, GraphicsRenderPassDescriptor renderDescriptor, string? debugName, bool createNewCommandBuffer);
        void ExecuteRenderCommandList(uint commandListId);

        bool CreateIndirectCommandList(uint commandListId, int maxCommandCount, string? debugName);

        void SetShader(uint commandListId, uint shaderId);
        void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, int index);
        void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index);
        void SetShaderTexture(uint commandListId, uint textureId, int slot, int index);
        void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index);
        void SetShaderIndirectCommandList(uint commandListId, uint indirectCommandListId, int slot, int index);

        void ExecuteIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);

        void SetIndexBuffer(uint commandListId, uint graphicsBufferId);
        void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
        void PresentScreenBuffer();
    }
}
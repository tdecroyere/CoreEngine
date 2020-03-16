using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CoreEngine.Graphics;

namespace CoreEngine.HostServices
{
    // TODO: Avoid the duplication of structs and enums

    public enum GraphicsPrimitiveType
    {
        Triangle,
        TriangleStrip,
        Line,
    }

    public enum GraphicsTextureFormat
    {
        Rgba8UnormSrgb,
        Bgra8UnormSrgb,
        Depth32Float,
        Rgba16Float,
        R16Float,
        BC1Srgb,
        BC2Srgb,
        BC3Srgb,
        BC4,
        BC5,
        BC6,
        BC7Srgb,
        Rgba32Float,
        Rgba16Unorm
    }

    public enum GraphicsDepthBufferOperation
    {
        DepthNone,
        CompareEqual,
        CompareLess,
        Write,
        ClearWrite
    }


    public enum GraphicsBlendOperation
    {
        None,
        AlphaBlending,
        AddOneOne,
        AddOneMinusSourceColor
    }

    public readonly struct GraphicsRenderPassDescriptor : IEquatable<GraphicsRenderPassDescriptor>
    {
        public GraphicsRenderPassDescriptor(RenderPassDescriptor renderPassDescriptor)
        {
            this.IsRenderShader = true;

            if (renderPassDescriptor.RenderTarget1 != null)
            {
                this.MultiSampleCount = renderPassDescriptor.RenderTarget1.Value.ColorTexture.MultiSampleCount;
                this.RenderTarget1TextureId = renderPassDescriptor.RenderTarget1.Value.ColorTexture.GraphicsResourceId;
                this.RenderTarget1TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget1.Value.ColorTexture.TextureFormat;
                this.RenderTarget1ClearColor = renderPassDescriptor.RenderTarget1.Value.ClearColor;
                this.RenderTarget1BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget1.Value.BlendOperation;
            }

            else
            {
                if (renderPassDescriptor.DepthTexture != null)
                {
                    this.MultiSampleCount = renderPassDescriptor.DepthTexture.MultiSampleCount;
                }

                else
                {
                    this.MultiSampleCount = 1;
                }

                this.RenderTarget1TextureId = null;
                this.RenderTarget1TextureFormat = null;
                this.RenderTarget1ClearColor = null;
                this.RenderTarget1BlendOperation = null;
            }

            if (renderPassDescriptor.RenderTarget2 != null)
            {
                this.RenderTarget2TextureId = renderPassDescriptor.RenderTarget2.Value.ColorTexture.GraphicsResourceId;
                this.RenderTarget2TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget2.Value.ColorTexture.TextureFormat;
                this.RenderTarget2ClearColor = renderPassDescriptor.RenderTarget2.Value.ClearColor;
                this.RenderTarget2BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget2.Value.BlendOperation;
            }

            else
            {
                this.RenderTarget2TextureId = null;
                this.RenderTarget2TextureFormat = null;
                this.RenderTarget2ClearColor = null;
                this.RenderTarget2BlendOperation = null;
            }

            if (renderPassDescriptor.RenderTarget3 != null)
            {
                this.RenderTarget3TextureId = renderPassDescriptor.RenderTarget3.Value.ColorTexture.GraphicsResourceId;
                this.RenderTarget3TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget3.Value.ColorTexture.TextureFormat;
                this.RenderTarget3ClearColor = renderPassDescriptor.RenderTarget3.Value.ClearColor;
                this.RenderTarget3BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget3.Value.BlendOperation;
            }

            else
            {
                this.RenderTarget3TextureId = null;
                this.RenderTarget3TextureFormat = null;
                this.RenderTarget3ClearColor = null;
                this.RenderTarget3BlendOperation = null;
            }

            if (renderPassDescriptor.RenderTarget4 != null)
            {
                this.RenderTarget4TextureId = (uint?)renderPassDescriptor.RenderTarget4.Value.ColorTexture.GraphicsResourceId;
                this.RenderTarget4TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget4.Value.ColorTexture.TextureFormat;
                this.RenderTarget4ClearColor = renderPassDescriptor.RenderTarget4.Value.ClearColor;
                this.RenderTarget4BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget4.Value.BlendOperation;
            }

            else
            {
                this.RenderTarget4TextureId = null;
                this.RenderTarget4TextureFormat = null;
                this.RenderTarget4ClearColor = null;
                this.RenderTarget4BlendOperation = null;
            }

            this.DepthTextureId = renderPassDescriptor.DepthTexture?.GraphicsResourceId;
            this.DepthBufferOperation = (GraphicsDepthBufferOperation)renderPassDescriptor.DepthBufferOperation;
            this.BackfaceCulling = renderPassDescriptor.BackfaceCulling;
        }

        public readonly bool IsRenderShader { get; }
        public readonly int? MultiSampleCount { get; }
        public readonly uint? RenderTarget1TextureId { get; }
        public readonly GraphicsTextureFormat? RenderTarget1TextureFormat { get; }
        public readonly Vector4? RenderTarget1ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget1BlendOperation { get; }
        public readonly uint? RenderTarget2TextureId { get; }
        public readonly GraphicsTextureFormat? RenderTarget2TextureFormat { get; }
        public readonly Vector4? RenderTarget2ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget2BlendOperation { get; }
        public readonly uint? RenderTarget3TextureId { get; }
        public readonly GraphicsTextureFormat? RenderTarget3TextureFormat { get; }
        public readonly Vector4? RenderTarget3ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget3BlendOperation { get; }
        public readonly uint? RenderTarget4TextureId { get; }
        public readonly GraphicsTextureFormat? RenderTarget4TextureFormat { get; }
        public readonly Vector4? RenderTarget4ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget4BlendOperation { get; }
        public readonly uint? DepthTextureId { get; }
        public readonly GraphicsDepthBufferOperation DepthBufferOperation { get; }
        public readonly bool BackfaceCulling { get; }

        public override int GetHashCode() 
        {
            return this.RenderTarget1TextureFormat.GetHashCode() ^ 
                   this.RenderTarget1BlendOperation.GetHashCode() ^ 
                   this.RenderTarget2TextureFormat.GetHashCode() ^ 
                   this.RenderTarget2BlendOperation.GetHashCode() ^ 
                   this.RenderTarget3TextureFormat.GetHashCode() ^ 
                   this.RenderTarget3BlendOperation.GetHashCode() ^ 
                   this.RenderTarget4TextureFormat.GetHashCode() ^ 
                   this.RenderTarget4BlendOperation.GetHashCode() ^ 
                   this.MultiSampleCount.GetHashCode() ^ 
                   this.DepthBufferOperation.GetHashCode() ^ 
                   this.BackfaceCulling.GetHashCode();
        }

        public override bool Equals(Object? obj) 
        {
            return obj is GraphicsRenderPassDescriptor && this == (GraphicsRenderPassDescriptor)obj;
        }

        public bool Equals(GraphicsRenderPassDescriptor other)
        {
            return this == other;
        }

        public static bool operator ==(GraphicsRenderPassDescriptor layout1, GraphicsRenderPassDescriptor layout2) 
        {
            return layout1.GetHashCode() == layout2.GetHashCode();
        }

        public static bool operator !=(GraphicsRenderPassDescriptor layout1, GraphicsRenderPassDescriptor layout2) 
        {
            return !(layout1 == layout2);
        }
    }

    public interface IGraphicsService
    {
        bool GetGpuError();
        Vector2 GetRenderSize();
        string? GetGraphicsAdapterName();
        float GetGpuExecutionTime(uint commandListId);
        
        bool CreateGraphicsBuffer(uint graphicsBufferId, int length, bool isWriteOnly, string? debugName);

        bool CreateTexture(uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isRenderTarget, string? debugName);
        void RemoveTexture(uint textureId);

        bool CreateIndirectCommandBuffer(uint indirectCommandBufferId, int maxCommandCount, string? debugName);

        bool CreateShader(uint shaderId, string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string? debugName);
        void RemoveShader(uint shaderId);

        bool CreatePipelineState(uint pipelineStateId, uint shaderId, GraphicsRenderPassDescriptor renderPassDescriptor, string? debugName);
        void RemovePipelineState(uint pipelineStateId);
        
        bool CreateCopyCommandList(uint commandListId, string? debugName);
        void ExecuteCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        void CopyGraphicsBufferDataToCpu(uint commandListId, uint graphicsBufferId, int length);
        void ReadGraphicsBufferData(uint graphicsBufferId, ReadOnlySpan<byte> data);
        void UploadDataToTexture(uint commandListId, uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, ReadOnlySpan<byte> data);
        void ResetIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);
        void OptimizeIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);

        bool CreateComputeCommandList(uint commandListId, string? debugName);
        void ExecuteComputeCommandList(uint commandListId);
        Vector3 DispatchThreads(uint commandListId, uint threadCountX, uint threadCountY, uint threadCountZ);
        
        bool CreateRenderCommandList(uint commandListId, GraphicsRenderPassDescriptor renderDescriptor, string? debugName);
        void ExecuteRenderCommandList(uint commandListId);

        void SetPipelineState(uint commandListId, uint pipelineStateId);

        void SetShader(uint commandListId, uint shaderId);
        void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, bool isReadOnly, int index);
        void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index);
        void SetShaderTexture(uint commandListId, uint textureId, int slot, bool isReadOnly, int index);
        void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index);
        void SetShaderIndirectCommandList(uint commandListId, uint indirectCommandListId, int slot, int index);
        void SetShaderIndirectCommandLists(uint commandListId, ReadOnlySpan<uint> indirectCommandListIdList, int slot, int index);

        void ExecuteIndirectCommandBuffer(uint commandListId, uint indirectCommandBufferId, int maxCommandCount);
        void SetIndexBuffer(uint commandListId, uint graphicsBufferId);
        void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);
        void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);
        
        void WaitForCommandList(uint commandListId, uint commandListToWaitId);
        void PresentScreenBuffer();
    }
}
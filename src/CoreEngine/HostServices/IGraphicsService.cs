using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CoreEngine.Graphics;

namespace CoreEngine.HostServices
{
    // TODO: Avoid the duplication of structs and enums

    public enum GraphicsCommandBufferType
    {
        Render,
        Copy,
        Compute
    }

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

    public enum GraphicsCommandBufferState
    {
        Created,
        Committed,
        Scheduled,
        Completed,
        Error
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

    public readonly struct GraphicsCommandBufferStatus
    {
        public readonly GraphicsCommandBufferState State { get; }
        public double ScheduledStartTime { get; }
        public double ScheduledEndTime { get; }
        public double ExecutionStartTime { get; }
        public double ExecutionEndTime { get; }
        public readonly int? ErrorCode { get; }
        public readonly string? ErrorMessage { get; }
    }

    // TODO: Make all method thread safe!
    [HostService]
    public interface IGraphicsService
    {
        // bool CreateSwapChain(uint swapChainId, int width, int height, GraphicsTextureFormat textureFormat);
        // void DeleteSwapChain(uint swapChainId);
        // void ResizeSwapChain(uint swapChainId, int with, int height);
        // Vector2 GetSwapChainSize();
        // uint GetNextSwapChainTexture(uint swapChainId);

        // TODO: Rename commandListId parameter to specialized name when there is a restriction
        // TODO: Add functions to manage resource transitions

        // TODO: This function should be merged into a GetAdapterInfos that will returns also the alignments and system support flags
        Vector2 GetRenderSize();
        string GetGraphicsAdapterName();
        
        // TODO: Create a heap resource so that the engine can apply multiple allocation strategies (buddy system, transient/aliases, etc.)
        // TODO: All create/remove for resources should take as parameter an heap and an offset
        // TODO: Remove isWriteOnly?
        // TODO: Pass the stride of the data type
        bool CreateGraphicsBuffer(uint graphicsBufferId, int length, bool isWriteOnly, string label);

        // TODO: Find a way to specify if the texture will only be used as RenderTarget or UAV
        bool CreateTexture(uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isRenderTarget, string label);
        void DeleteTexture(uint textureId);

        // TODO: Pass a shader Id so that we can create an indirect argument buffer from the shader definition
        bool CreateIndirectCommandBuffer(uint indirectCommandBufferId, int maxCommandCount, string label);

        bool CreateShader(uint shaderId, string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string label);
        void DeleteShader(uint shaderId);

        bool CreatePipelineState(uint pipelineStateId, uint shaderId, GraphicsRenderPassDescriptor renderPassDescriptor, string label);
        void DeletePipelineState(uint pipelineStateId);

        // TODO: Implement a barrier ressource

        // TODO: Refactor the command buffer/command list to map it better to have fewer Allocators in DirectX12
        // (One Command Allocator per queue type and per frame and per threads)
        bool CreateCommandBuffer(uint commandBufferId, GraphicsCommandBufferType commandBufferType, string label);
        void DeleteCommandBuffer(uint commandBufferId);
        void ResetCommandBuffer(uint commandBufferId);
        void ExecuteCommandBuffer(uint commandBufferId);
        GraphicsCommandBufferStatus? GetCommandBufferStatus(uint commandBufferId);

        // TODO: Shader parameters is a separate resource that we can bind it is allocated in a heap and can be dynamic and is set in one call in a command list
        // TODO: Each shader parameter set correspond in DX12 to a descriptorTable and to an argument buffer in Metal
        void SetShaderBuffer(uint commandListId, uint graphicsBufferId, int slot, bool isReadOnly, int index);
        void SetShaderBuffers(uint commandListId, ReadOnlySpan<uint> graphicsBufferIdList, int slot, int index);
        void SetShaderTexture(uint commandListId, uint textureId, int slot, bool isReadOnly, int index);
        void SetShaderTextures(uint commandListId, ReadOnlySpan<uint> textureIdList, int slot, int index);
        void SetShaderIndirectCommandList(uint commandListId, uint indirectCommandListId, int slot, int index);
        void SetShaderIndirectCommandLists(uint commandListId, ReadOnlySpan<uint> indirectCommandListIdList, int slot, int index);

        bool CreateCopyCommandList(uint commandListId, uint commandBufferId, string label);
        void CommitCopyCommandList(uint commandListId);
        void UploadDataToGraphicsBuffer(uint commandListId, uint graphicsBufferId, ReadOnlySpan<byte> data);
        void CopyGraphicsBufferDataToCpu(uint commandListId, uint graphicsBufferId, int length);
        void ReadGraphicsBufferData(uint graphicsBufferId, ReadOnlySpan<byte> data);
        void UploadDataToTexture(uint commandListId, uint textureId, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel, ReadOnlySpan<byte> data);

        // TODO: Rename that to IndirectCommandBuffer
        void ResetIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);
        void OptimizeIndirectCommandList(uint commandListId, uint indirectCommandListId, int maxCommandCount);

        bool CreateComputeCommandList(uint commandListId, uint commandBufferId, string label);
        void CommitComputeCommandList(uint commandListId);
        Vector3 DispatchThreads(uint commandListId, uint threadCountX, uint threadCountY, uint threadCountZ);
        
        bool CreateRenderCommandList(uint commandListId, uint commandBufferId, GraphicsRenderPassDescriptor renderDescriptor, string label);
        void CommitRenderCommandList(uint commandListId);

        void SetPipelineState(uint commandListId, uint pipelineStateId);

        // TODO: Add a raytrace command list

        // TODO: This function should be removed. Only pipeline states can be set 
        void SetShader(uint commandListId, uint shaderId);

        void ExecuteIndirectCommandBuffer(uint commandListId, uint indirectCommandBufferId, int maxCommandCount);

        // TODO: Merge SetIndexBuffer to DrawIndexedPrimitives
        void SetIndexBuffer(uint commandListId, uint graphicsBufferId);
        void DrawIndexedPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId);

        // TODO: Change that to take instances params
        void DrawPrimitives(uint commandListId, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount);
        
        void WaitForCommandList(uint commandListId, uint commandListToWaitId);

        // TODO: Add a parameter to specify which drawable we should update. Usefull for editor or multiple windows management
        // TODO: Rename that to PresentSwapChain and add swap chain id parameter
        void PresentScreenBuffer(uint commandBufferId);

        // TODO: Rename that to WaitForVSync()
        void WaitForAvailableScreenBuffer();
    }
}
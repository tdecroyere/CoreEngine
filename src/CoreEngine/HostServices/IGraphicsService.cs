using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CoreEngine.Graphics;

namespace CoreEngine.HostServices
{
    // TODO: Avoid the duplication of structs and enums

    public enum GraphicsServiceHeapType
    {
        Gpu,
        Upload,
        ReadBack
    }

    public enum GraphicsServiceCommandType
    {
        Render,
        Copy,
        Compute,
        Present
    }

    public enum GraphicsBufferUsage
    {
        Storage,
        IndirectCommands
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

    public enum GraphicsTextureUsage
    {
        ShaderRead,
        ShaderWrite,
        RenderTarget
    }

    public enum GraphicsDepthBufferOperation
    {
        DepthNone,
        CompareEqual,
        CompareGreater,
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

    public enum GraphicsQueryBufferType
    {
        Timestamp,
        CopyTimestamp,
        GraphicsPipelineStats
    }

    public enum GraphicsPrimitiveType
    {
        Triangle,
        Line
    }

    public enum GraphicsResourceState
    {
        StateDestinationCopy,
        StateShaderRead,
        StateCommon
    }

    public readonly struct GraphicsAllocationInfos
    {
        public GraphicsAllocationInfos(int sizeInBytes, int alignment)
        {
            this.SizeInBytes = sizeInBytes;
            this.Alignment = alignment;
        }

        public int SizeInBytes { get; }
        public int Alignment { get; }
    }

    public readonly struct GraphicsFence
    {
        public GraphicsFence(Fence fence)
        {
            this.CommandQueuePointer = fence.CommandQueue.NativePointer;
            this.Value = fence.Value;
        }

        public IntPtr CommandQueuePointer { get; }
        public ulong Value { get; }
    }

    public readonly struct GraphicsRenderPassDescriptor : IEquatable<GraphicsRenderPassDescriptor>
    {
        public GraphicsRenderPassDescriptor(RenderPassDescriptor renderPassDescriptor)
        {
            this.IsRenderShader = true;

            if (renderPassDescriptor.RenderTarget1 != null)
            {
                this.MultiSampleCount = renderPassDescriptor.RenderTarget1.Value.ColorTexture.MultiSampleCount;
                this.RenderTarget1TexturePointer = renderPassDescriptor.RenderTarget1.Value.ColorTexture.NativePointer;
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

                this.RenderTarget1TexturePointer = null;
                this.RenderTarget1TextureFormat = null;
                this.RenderTarget1ClearColor = null;
                this.RenderTarget1BlendOperation = null;
            }

            if (renderPassDescriptor.RenderTarget2 != null)
            {
                this.RenderTarget2TexturePointer = renderPassDescriptor.RenderTarget2.Value.ColorTexture.NativePointer;
                this.RenderTarget2TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget2.Value.ColorTexture.TextureFormat;
                this.RenderTarget2ClearColor = renderPassDescriptor.RenderTarget2.Value.ClearColor;
                this.RenderTarget2BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget2.Value.BlendOperation;
            }

            else
            {
                this.RenderTarget2TexturePointer = null;
                this.RenderTarget2TextureFormat = null;
                this.RenderTarget2ClearColor = null;
                this.RenderTarget2BlendOperation = null;
            }

            if (renderPassDescriptor.RenderTarget3 != null)
            {
                this.RenderTarget3TexturePointer = renderPassDescriptor.RenderTarget3.Value.ColorTexture.NativePointer;
                this.RenderTarget3TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget3.Value.ColorTexture.TextureFormat;
                this.RenderTarget3ClearColor = renderPassDescriptor.RenderTarget3.Value.ClearColor;
                this.RenderTarget3BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget3.Value.BlendOperation;
            }

            else
            {
                this.RenderTarget3TexturePointer = null;
                this.RenderTarget3TextureFormat = null;
                this.RenderTarget3ClearColor = null;
                this.RenderTarget3BlendOperation = null;
            }

            if (renderPassDescriptor.RenderTarget4 != null)
            {
                this.RenderTarget4TexturePointer = renderPassDescriptor.RenderTarget4.Value.ColorTexture.NativePointer;
                this.RenderTarget4TextureFormat = (GraphicsTextureFormat?)renderPassDescriptor.RenderTarget4.Value.ColorTexture.TextureFormat;
                this.RenderTarget4ClearColor = renderPassDescriptor.RenderTarget4.Value.ClearColor;
                this.RenderTarget4BlendOperation = (GraphicsBlendOperation?)renderPassDescriptor.RenderTarget4.Value.BlendOperation;
            }

            else
            {
                this.RenderTarget4TexturePointer = null;
                this.RenderTarget4TextureFormat = null;
                this.RenderTarget4ClearColor = null;
                this.RenderTarget4BlendOperation = null;
            }

            this.DepthTexturePointer = renderPassDescriptor.DepthTexture?.NativePointer;
            this.DepthBufferOperation = (GraphicsDepthBufferOperation)renderPassDescriptor.DepthBufferOperation;
            this.BackfaceCulling = renderPassDescriptor.BackfaceCulling;
            this.PrimitiveType = (GraphicsPrimitiveType)renderPassDescriptor.PrimitiveType;
        }

        public readonly bool IsRenderShader { get; }
        public readonly int? MultiSampleCount { get; }
        public readonly IntPtr? RenderTarget1TexturePointer { get; }
        public readonly GraphicsTextureFormat? RenderTarget1TextureFormat { get; }
        public readonly Vector4? RenderTarget1ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget1BlendOperation { get; }
        public readonly IntPtr? RenderTarget2TexturePointer { get; }
        public readonly GraphicsTextureFormat? RenderTarget2TextureFormat { get; }
        public readonly Vector4? RenderTarget2ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget2BlendOperation { get; }
        public readonly IntPtr? RenderTarget3TexturePointer { get; }
        public readonly GraphicsTextureFormat? RenderTarget3TextureFormat { get; }
        public readonly Vector4? RenderTarget3ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget3BlendOperation { get; }
        public readonly IntPtr? RenderTarget4TexturePointer { get; }
        public readonly GraphicsTextureFormat? RenderTarget4TextureFormat { get; }
        public readonly Vector4? RenderTarget4ClearColor { get; }
        public readonly GraphicsBlendOperation? RenderTarget4BlendOperation { get; }
        public readonly IntPtr? DepthTexturePointer { get; }
        public readonly GraphicsDepthBufferOperation DepthBufferOperation { get; }
        public readonly bool BackfaceCulling { get; }
        public readonly GraphicsPrimitiveType PrimitiveType { get; }

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
                   this.BackfaceCulling.GetHashCode() ^
                   this.PrimitiveType.GetHashCode();
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

    // TODO: Make all method thread safe!
    // TODO: Can we pass readonly structs as references or pointers with the in keyword?
    // TODO: Review int parameters (can they be converted to uint?)
    [HostService]
    public interface IGraphicsService
    {
        // TODO: Add an adapter object and list method that can be passed to other methods
        // TODO: For the moment we always use the best GPU available in the system

        // GraphicsAdapterInfos GetGraphicsAdapterInfos();
        string GetGraphicsAdapterName();

        GraphicsAllocationInfos GetBufferAllocationInfos(int sizeInBytes);
        GraphicsAllocationInfos GetTextureAllocationInfos(GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);

        IntPtr CreateCommandQueue(GraphicsServiceCommandType commandQueueType);
        void SetCommandQueueLabel(IntPtr commandQueuePointer, string label);
        void DeleteCommandQueue(IntPtr commandQueuePointer);
        void ResetCommandQueue(IntPtr commandQueuePointer);
        ulong GetCommandQueueTimestampFrequency(IntPtr commandQueuePointer);
        ulong ExecuteCommandLists(IntPtr commandQueuePointer, ReadOnlySpan<IntPtr> commandLists, ReadOnlySpan<GraphicsFence> fencesToWait);
        void WaitForCommandQueueOnCpu(GraphicsFence fenceToWait);
 
        IntPtr CreateCommandList(IntPtr commandQueuePointer);
        void SetCommandListLabel(IntPtr commandListPointer, string label);
        void DeleteCommandList(IntPtr commandListPointer);
        void ResetCommandList(IntPtr commandListPointer);
        void CommitCommandList(IntPtr commandListPointer);
        
        IntPtr CreateGraphicsHeap(GraphicsServiceHeapType type, ulong sizeInBytes);
        void SetGraphicsHeapLabel(IntPtr graphicsHeapPointer, string label);
        void DeleteGraphicsHeap(IntPtr graphicsHeapPointer);

        // TODO: Try to make a cache system for transient resources that are always created with the same descriptors
        IntPtr CreateShaderResourceHeap(ulong length);
        void SetShaderResourceHeapLabel(IntPtr shaderResourceHeapPointer, string label);
        void DeleteShaderResourceHeap(IntPtr shaderResourceHeapPointer);
        void CreateShaderResourceTexture(IntPtr shaderResourceHeapPointer, uint index, IntPtr texturePointer);
        void DeleteShaderResourceTexture(IntPtr shaderResourceHeapPointer, uint index);
        void CreateShaderResourceBuffer(IntPtr shaderResourceHeapPointer, uint index, IntPtr bufferPointer);
        void DeleteShaderResourceBuffer(IntPtr shaderResourceHeapPointer, uint index);
        // TODO: UAV

        // TODO: Move make aliasable into a separate method
        IntPtr CreateGraphicsBuffer(IntPtr graphicsHeapPointer, ulong heapOffset, GraphicsBufferUsage graphicsBufferUsage, int sizeInBytes);
        void SetGraphicsBufferLabel(IntPtr graphicsBufferPointer, string label);
        void DeleteGraphicsBuffer(IntPtr graphicsBufferPointer);
        IntPtr GetGraphicsBufferCpuPointer(IntPtr graphicsBufferPointer);
        void ReleaseGraphicsBufferCpuPointer(IntPtr graphicsBufferPointer);

        // TODO: Move make aliasable into a separate method
        IntPtr CreateTexture(IntPtr graphicsHeapPointer, ulong heapOffset, bool isAliasable, GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
        void SetTextureLabel(IntPtr texturePointer, string label);
        void DeleteTexture(IntPtr texturePointer);

        IntPtr CreateSwapChain(IntPtr windowPointer, IntPtr commandQueuePointer, int width, int height, GraphicsTextureFormat textureFormat);
        void DeleteSwapChain(IntPtr swapChainPointer);
        void ResizeSwapChain(IntPtr swapChainPointer, int width, int height);
        IntPtr GetSwapChainBackBufferTexture(IntPtr swapChainPointer);
        ulong PresentSwapChain(IntPtr swapChainPointer);
        void WaitForSwapChainOnCpu(IntPtr swapChainPointer);

        // TODO: Do we need a reset function?
        IntPtr CreateQueryBuffer(GraphicsQueryBufferType queryBufferType, int length);
        void ResetQueryBuffer(IntPtr queryBufferPointer);
        void SetQueryBufferLabel(IntPtr queryBufferPointer, string label);
        void DeleteQueryBuffer(IntPtr queryBufferPointer);

        IntPtr CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode);
        void SetShaderLabel(IntPtr shaderPointer, string label);
        void DeleteShader(IntPtr shaderPointer);

        IntPtr CreatePipelineState(IntPtr shaderPointer, GraphicsRenderPassDescriptor renderPassDescriptor);
        void SetPipelineStateLabel(IntPtr pipelineStatePointer, string label);
        void DeletePipelineState(IntPtr pipelineStatePointer);

        void CopyDataToGraphicsBuffer(IntPtr commandListPointer, IntPtr destinationGraphicsBufferPointer, IntPtr sourceGraphicsBufferPointer, int length);
        void CopyDataToTexture(IntPtr commandListPointer, IntPtr destinationTexturePointer, IntPtr sourceGraphicsBufferPointer, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
        void CopyTexture(IntPtr commandListPointer, IntPtr destinationTexturePointer, IntPtr sourceTexturePointer);

        // TODO: Only allow passing an array of buffers or resources
        void TransitionGraphicsBufferToState(IntPtr commandListPointer, IntPtr graphicsBufferPointer, GraphicsResourceState resourceState);

        // TODO: Rename that to DispatchCompute
        void DispatchThreads(IntPtr commandListPointer, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);

        // Add a CreateRenderPass method and a RenderPassObject
        // The render pass object will also be used for the pipeline state creation

        // TODO: Split render pass definition and render pass resource assign?
        void BeginRenderPass(IntPtr commandListPointer, GraphicsRenderPassDescriptor renderPassDescriptor);
        void EndRenderPass(IntPtr commandListPointer);

        void SetPipelineState(IntPtr commandListPointer, IntPtr pipelineStatePointer);

        // TODO: Add resource barrier management
        
        // TODO: Add a raytrace command list

        // TODO: This function should be removed. Only pipeline states can be set 
        void SetShaderResourceHeap(IntPtr commandListPointer, IntPtr shaderResourceHeapPointer);
        void SetShader(IntPtr commandListPointer, IntPtr shaderPointer);
        void SetShaderParameterValues(IntPtr commandListPointer, uint slot, ReadOnlySpan<uint> values);

        // TODO: Suppress threadGroupCountY and threadGroupCountZ for mesh shader
        void DispatchMesh(IntPtr commandListPointer, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);
        void DispatchMeshIndirect(IntPtr commandListPointer, uint maxCommandCount, IntPtr commandGraphicsBufferPointer, uint commandBufferOffset, uint commandSizeInBytes);

        // TODO: Find a way to map opaque datastructures like the pipeline stats to a generic one to make this portable
        // TODO: IS the pipeline state usefull?
        void BeginQuery(IntPtr commandListPointer, IntPtr queryBufferPointer, int index);
        void EndQuery(IntPtr commandListPointer, IntPtr queryBufferPointer, int index);
        void ResolveQueryData(IntPtr commandListPointer, IntPtr queryBufferPointer, IntPtr destinationBufferPointer, int startIndex, int endIndex);
    }
}
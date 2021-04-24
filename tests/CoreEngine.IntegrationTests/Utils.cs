using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.HostServices;
using Moq;

namespace CoreEngine.IntegrationTests
{
    public class TestGraphicsService : IGraphicsService
    {
        public string GetGraphicsAdapterName() { return "TestAdapter"; }
        
        public GraphicsAllocationInfos GetTextureAllocationInfos(GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
        {
            return new GraphicsAllocationInfos(1024, 64);
        }

        public IntPtr CreateCommandQueue(GraphicsServiceCommandType commandQueueType)
        {
            return new IntPtr(1);
        }

        public void SetCommandQueueLabel(IntPtr commandQueuePointer, string label) {}
        public void DeleteCommandQueue(IntPtr commandQueuePointer) {}
        public void ResetCommandQueue(IntPtr commandQueuePointer) {}
        public ulong GetCommandQueueTimestampFrequency(IntPtr commandQueuePointer)
        {
            return 1;
        }

        public ulong ExecuteCommandLists(IntPtr commandQueuePointer, ReadOnlySpan<IntPtr> commandLists, bool isAwaitable) 
        {
            return 1;
        }

        public void WaitForCommandQueue(IntPtr commandQueuePointer, IntPtr commandQueueToWaitPointer, ulong fenceValue) {}
        public void WaitForCommandQueueOnCpu(IntPtr commandQueueToWaitPointer, ulong fenceValue) {}
 
        public IntPtr CreateCommandList(IntPtr commandQueuePointer) 
        {
            return new IntPtr(1);
        }

        public void SetCommandListLabel(IntPtr commandListPointer, string label) {}
        public void DeleteCommandList(IntPtr commandListPointer) {}
        public void ResetCommandList(IntPtr commandListPointer) {}
        public void CommitCommandList(IntPtr commandListPointer) {}
        
        public IntPtr CreateGraphicsHeap(GraphicsServiceHeapType type, ulong length) 
        {
            return new IntPtr(1);
        }

        public void SetGraphicsHeapLabel(IntPtr graphicsHeapPointer, string label) {}
        public void DeleteGraphicsHeap(IntPtr graphicsHeapPointer) {}

        public IntPtr CreateGraphicsBuffer(IntPtr graphicsHeapPointer, ulong heapOffset, bool isAliasable, int sizeInBytes) 
        {
            return new IntPtr(1);
        }

        public void SetGraphicsBufferLabel(IntPtr graphicsBufferPointer, string label) {}
        public void DeleteGraphicsBuffer(IntPtr graphicsBufferPointer) {}
        public IntPtr GetGraphicsBufferCpuPointer(IntPtr graphicsBufferPointer) {}

        public IntPtr CreateTexture(IntPtr graphicsHeapPointer, ulong heapOffset, bool isAliasable, GraphicsTextureFormat textureFormat, GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount) 
        {
            return new IntPtr(1);
        }

        public void SetTextureLabel(IntPtr texturePointer, string label) {}
        public void DeleteTexture(IntPtr texturePointer) {}

        public IntPtr CreateSwapChain(IntPtr windowPointer, IntPtr commandQueuePointer, int width, int height, GraphicsTextureFormat textureFormat) 
        {
            return new IntPtr(1);
        }
        public void ResizeSwapChain(IntPtr swapChainPointer, int width, int height) {}
        public IntPtr GetSwapChainBackBufferTexture(IntPtr swapChainPointer) {}
        public ulong PresentSwapChain(IntPtr swapChainPointer) {}

        public IntPtr CreateIndirectCommandBuffer(int maxCommandCount) 
        {
            return new IntPtr(1);
        }
        public void SetIndirectCommandBufferLabel(IntPtr indirectCommandBufferPointer, string label) {}
        public void DeleteIndirectCommandBuffer(IntPtr indirectCommandBufferPointer) {}

        public IntPtr CreateQueryBuffer(GraphicsQueryBufferType queryBufferType, int length) 
        {
            return new IntPtr(1);
        }
        public void SetQueryBufferLabel(IntPtr queryBufferPointer, string label) {}
        public void DeleteQueryBuffer(IntPtr queryBufferPointer) {}

        public IntPtr CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode) 
        {
            return new IntPtr(1);
        }
        public void SetShaderLabel(IntPtr shaderPointer, string label) {}
        public void DeleteShader(IntPtr shaderPointer) {}

        public IntPtr CreatePipelineState(IntPtr shaderPointer, GraphicsRenderPassDescriptor renderPassDescriptor) 
        {
            return new IntPtr(1);
        }
        public void SetPipelineStateLabel(IntPtr pipelineStatePointer, string label) {}
        public void DeletePipelineState(IntPtr pipelineStatePointer) {}

        public void SetShaderBuffer(IntPtr commandListPointer, IntPtr graphicsBufferPointer, int slot, bool isReadOnly, int index) {}
        public void SetShaderBuffers(IntPtr commandListPointer, ReadOnlySpan<IntPtr> graphicsBufferPointerList, int slot, int index) {}
        public void SetShaderTexture(IntPtr commandListPointer, IntPtr texturePointer, int slot, bool isReadOnly, int index) {}
        public void SetShaderTextures(IntPtr commandListPointer, ReadOnlySpan<IntPtr> texturePointerList, int slot, int index) {}
        public void SetShaderIndirectCommandList(IntPtr commandListPointer, IntPtr indirectCommandListPointer, int slot, int index) {}
        public void SetShaderIndirectCommandLists(IntPtr commandListPointer, ReadOnlySpan<IntPtr> indirectCommandListPointerList, int slot, int index) {}

        public void CopyDataToGraphicsBuffer(IntPtr commandListPointer, IntPtr destinationGraphicsBufferPointer, IntPtr sourceGraphicsBufferPointer, int length) {}
        public void CopyDataToTexture(IntPtr commandListPointer, IntPtr destinationTexturePointer, IntPtr sourceGraphicsBufferPointer, GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel) {}
        public void CopyTexture(IntPtr commandListPointer, IntPtr destinationTexturePointer, IntPtr sourceTexturePointer) {}

        public void ResetIndirectCommandList(IntPtr commandListPointer, IntPtr indirectCommandListPointer, int maxCommandCount) {}
        public void OptimizeIndirectCommandList(IntPtr commandListPointer, IntPtr indirectCommandListPointer, int maxCommandCount) {}

        public Vector3 DispatchThreads(IntPtr commandListPointer, uint threadCountX, uint threadCountY, uint threadCountZ) 
        {
            return new Vector3(1, 1, 1);
        }

        public void BeginRenderPass(IntPtr commandListPointer, GraphicsRenderPassDescriptor renderPassDescriptor) {}
        public void EndRenderPass(IntPtr commandListPointer) {}

        public void SetPipelineState(IntPtr commandListPointer, IntPtr pipelineStatePointer) {}

        public void SetShader(IntPtr commandListPointer, IntPtr shaderPointer) {}
        public void ExecuteIndirectCommandBuffer(IntPtr commandListPointer, IntPtr indirectCommandBufferPointer, int maxCommandCount) {}

        public void SetIndexBuffer(IntPtr commandListPointer, IntPtr graphicsBufferPointer) {}
        public void DrawIndexedPrimitives(IntPtr commandListPointer, GraphicsPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId) {}

        public void DrawPrimitives(IntPtr commandListPointer, GraphicsPrimitiveType primitiveType, int startVertex, int vertexCount) {}

        public void QueryTimestamp(IntPtr commandListPointer, IntPtr queryBufferPointer, int index) {}
        public void ResolveQueryData(IntPtr commandListPointer, IntPtr queryBufferPointer, IntPtr destinationBufferPointer, int startIndex, int endIndex) {}
    }

    public static class Utils
    {
        public static IGraphicsService SetupGraphicsService()
        {
            return new TestGraphicsService();
        }
    }
}
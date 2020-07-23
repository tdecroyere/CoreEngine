using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This class is not thread safe for now

    public class GraphicsMemoryAllocator
    {
        private readonly IGraphicsService graphicsService;

        private uint currentGraphicsResourceId;

        // TODO: Manage heaps differently
        private GraphicsHeap globalGpuHeap;
        private ulong currentGlobalGpuHeapOffset;
        private GraphicsHeap globalUploadHeap;
        private ulong currentGlobalUploadHeapOffset;
        private GraphicsHeap readBackHeap;

        public GraphicsMemoryAllocator(IGraphicsService graphicsService)
        {
            this.graphicsService = graphicsService;
            this.currentGraphicsResourceId = 0;

            var globalGpuHeapId = currentGraphicsResourceId++;
            var globalGpuHeapSize = Utils.GigaBytesToBytes(1); // Allocate 1GB for now
            this.currentGlobalGpuHeapOffset = 0;
            
            if(!this.graphicsService.CreateGraphicsHeap(globalGpuHeapId, (GraphicsServiceHeapType)GraphicsHeapType.Gpu, globalGpuHeapSize, "GlobalGpuHeap"))
            {
                throw new InvalidOperationException("Cannot create global GPU heap");
            }

            this.globalGpuHeap = new GraphicsHeap(globalGpuHeapId, GraphicsHeapType.Gpu, globalGpuHeapSize);

            var globalUploadHeapId = currentGraphicsResourceId++;
            var globalUploadHeapSize = Utils.GigaBytesToBytes(1); // Allocate 1GB for now
            this.currentGlobalUploadHeapOffset = 0;
            
            if(!this.graphicsService.CreateGraphicsHeap(globalUploadHeapId, (GraphicsServiceHeapType)GraphicsHeapType.Upload, globalUploadHeapSize, "GlobalUploadHeap"))
            {
                throw new InvalidOperationException("Cannot create global Upload heap");
            }

            this.globalUploadHeap = new GraphicsHeap(globalUploadHeapId, GraphicsHeapType.Upload, globalUploadHeapSize);
        }

        public ulong AllocatedGpuMemory { get; private set; }
        public ulong AllocatedCpuMemory { get; private set; }

        // TODO: Change that, currently we are just blindly sequentially allocate memory without freeing it
        public GraphicsMemoryAllocation AllocateBuffer(int length, GraphicsHeapType heapType)
        {
            if (heapType == GraphicsHeapType.Upload)
            {
                // TODO: Get the alignment from the device

                var alignment = (ulong)64 * 1024;
                this.currentGlobalUploadHeapOffset = Utils.AlignValue(this.currentGlobalUploadHeapOffset, alignment);

                var allocation = new GraphicsMemoryAllocation(this.globalUploadHeap, this.currentGlobalUploadHeapOffset);
                this.currentGlobalUploadHeapOffset += (ulong)length;
                this.AllocatedCpuMemory += (ulong)length;
                
                return allocation;
            }

            else
            {
                // TODO: Get the alignment from the device
                var alignment = (ulong)64 * 1024;
                this.currentGlobalGpuHeapOffset = Utils.AlignValue(this.currentGlobalGpuHeapOffset, alignment);

                var allocation = new GraphicsMemoryAllocation(this.globalGpuHeap, this.currentGlobalGpuHeapOffset);
                this.currentGlobalGpuHeapOffset += (ulong)length;
                this.AllocatedGpuMemory += (ulong)length;

                return allocation;
            }
        }

        public GraphicsMemoryAllocation AllocateTexture(TextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isRenderTarget)
        {
            // TODO: Implement render target allocation with transiant space and aliasable resources
            if (isRenderTarget)
            {
                return new GraphicsMemoryAllocation();
            }

            var allocationInfos = this.graphicsService.GetTextureAllocationInfos((GraphicsTextureFormat)textureFormat, width, height, faceCount, mipLevels, multisampleCount);

            this.currentGlobalGpuHeapOffset = Utils.AlignValue(this.currentGlobalGpuHeapOffset, (ulong)allocationInfos.Alignment);

            var allocation = new GraphicsMemoryAllocation(this.globalGpuHeap, this.currentGlobalGpuHeapOffset);
            this.currentGlobalGpuHeapOffset += (ulong)allocationInfos.Length;
            this.AllocatedGpuMemory += (ulong)allocationInfos.Length;

            return allocation;
        }

        public void FreeAllocation(GraphicsMemoryAllocation allocation)
        {
            // TODO: Free allocation
            // TODO: Don't delete graphics buffer until next frame to not overwrite data
        }

        public void BindGpuHeaps(CommandList commandList)
        {
            this.graphicsService.BindGraphicsHeap(commandList.Id, this.globalGpuHeap.Id);
        }
    }
}
using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    public class GraphicsMemoryManager
    {
        private readonly IGraphicsService graphicsService;

        private IGraphicsMemoryAllocator globalGpuMemoryAllocator;
        private IGraphicsMemoryAllocator globalTransientGpuMemoryAllocator;
        private IGraphicsMemoryAllocator globalUploadMemoryAllocator;
        private IGraphicsMemoryAllocator globalReadBackMemoryAllocator;

        public GraphicsMemoryManager(IGraphicsService graphicsService)
        {
            this.graphicsService = graphicsService;

            this.globalGpuMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.Gpu, Utils.GigaBytesToBytes(1), "GlobalGpuHeap");
            this.globalTransientGpuMemoryAllocator = new TransientGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.Gpu, Utils.GigaBytesToBytes(1), "GlobalTransientGpuHeap");
            this.globalUploadMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.Upload, Utils.GigaBytesToBytes(1), "GlobalUploadHeap");
            this.globalReadBackMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.ReadBack, Utils.MegaBytesToBytes(32), "GlobalReadBackHeap");
        }

        public ulong AllocatedGpuMemory => this.globalGpuMemoryAllocator.AllocatedMemory;
        public ulong AllocatedTransientGpuMemory => this.globalTransientGpuMemoryAllocator.AllocatedMemory;
        public ulong AllocatedCpuMemory => (this.globalUploadMemoryAllocator.AllocatedMemory + this.globalReadBackMemoryAllocator.AllocatedMemory);

        // TODO: Change that, currently we are just blindly sequentially allocate memory without freeing it
        public GraphicsMemoryAllocation AllocateBuffer(GraphicsHeapType heapType, int sizeInBytes)
        {
            var memoryAllocator = this.globalGpuMemoryAllocator;

            if (heapType == GraphicsHeapType.Upload)
            {
                memoryAllocator = this.globalUploadMemoryAllocator;
            }

            else if (heapType == GraphicsHeapType.ReadBack)
            {
                memoryAllocator = this.globalReadBackMemoryAllocator;
            }

            // TODO: Get the alignment from the device
            var alignment = (ulong)64 * 1024;
            return memoryAllocator.AllocateMemory(sizeInBytes, alignment);
        }

        public GraphicsMemoryAllocation AllocateTexture(GraphicsHeapType heapType, TextureFormat textureFormat, TextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount)
        {
            var allocationInfos = this.graphicsService.GetTextureAllocationInfos((GraphicsTextureFormat)textureFormat, (GraphicsTextureUsage)usage, width, height, faceCount, mipLevels, multisampleCount);

            if (heapType == GraphicsHeapType.TransientGpu)
            {
                return this.globalTransientGpuMemoryAllocator.AllocateMemory(allocationInfos.SizeInBytes, (ulong)allocationInfos.Alignment);
            }

            return this.globalGpuMemoryAllocator.AllocateMemory(allocationInfos.SizeInBytes, (ulong)allocationInfos.Alignment);
        }

        public void FreeAllocation(GraphicsMemoryAllocation allocation)
        {
            var memoryAllocator = this.globalGpuMemoryAllocator;

            if (allocation.GraphicsHeap.Type == GraphicsHeapType.Upload)
            {
                memoryAllocator = this.globalUploadMemoryAllocator;
            }

            else if (allocation.GraphicsHeap.Type == GraphicsHeapType.ReadBack)
            {
                memoryAllocator = this.globalReadBackMemoryAllocator;
            }

            else if (allocation.GraphicsHeap.Type == GraphicsHeapType.TransientGpu)
            {
                memoryAllocator = this.globalTransientGpuMemoryAllocator;
            }

            memoryAllocator.FreeMemory(allocation);

            //this.AllocatedGpuMemory -= (ulong)allocation.SizeInBytes;

            // TODO: Free allocation
            // TODO: Don't delete graphics buffer until next frame to not overwrite data
        }

        public void Reset(uint currentFrameNumber)
        {
            this.globalGpuMemoryAllocator.Reset(currentFrameNumber);
            this.globalTransientGpuMemoryAllocator.Reset(currentFrameNumber);
            this.globalUploadMemoryAllocator.Reset(currentFrameNumber);
            this.globalReadBackMemoryAllocator.Reset(currentFrameNumber);
        }
    }
}
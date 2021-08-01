using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    public class GraphicsMemoryManager : IDisposable
    {
        private readonly IGraphicsService graphicsService;

        private bool isDisposed;

        private readonly IGraphicsMemoryAllocator globalGpuMemoryAllocator;
        private readonly IGraphicsMemoryAllocator globalTransientGpuMemoryAllocator;
        private readonly IGraphicsMemoryAllocator globalUploadMemoryAllocator;
        private readonly IGraphicsMemoryAllocator globalReadBackMemoryAllocator;

        public GraphicsMemoryManager(GraphicsManager graphicsManager, IGraphicsService graphicsService)
        {
            this.graphicsService = graphicsService;

            // TODO: Convert upload/readback heap to Transient

            this.globalGpuMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsManager, GraphicsHeapType.Gpu, Utils.MegaBytesToBytes(64), "GlobalGpuHeap");
            this.globalTransientGpuMemoryAllocator = new TransientGraphicsMemoryAllocator(graphicsManager, GraphicsHeapType.Gpu, Utils.MegaBytesToBytes(128), "GlobalTransientGpuHeap");
            this.globalUploadMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsManager, GraphicsHeapType.Upload, Utils.MegaBytesToBytes(512), "GlobalUploadHeap");
            this.globalReadBackMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsManager, GraphicsHeapType.ReadBack, Utils.MegaBytesToBytes(32), "GlobalReadBackHeap");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing && !this.isDisposed)
            {
                this.globalGpuMemoryAllocator.Dispose();
                this.globalTransientGpuMemoryAllocator.Dispose();
                this.globalUploadMemoryAllocator.Dispose();
                this.globalReadBackMemoryAllocator.Dispose();

                this.isDisposed = true;
            }
        }

        public ulong AllocatedGpuMemory => this.globalGpuMemoryAllocator.AllocatedMemory;
        public ulong TotalGpuMemory => this.globalGpuMemoryAllocator.TotalMemory;
        public ulong AllocatedTransientGpuMemory => this.globalTransientGpuMemoryAllocator.AllocatedMemory;
        public ulong TotalTransientGpuMemory => this.globalTransientGpuMemoryAllocator.TotalMemory;
        public ulong AllocatedCpuMemory => this.globalUploadMemoryAllocator.AllocatedMemory + this.globalReadBackMemoryAllocator.AllocatedMemory;

        // TODO: Change that, currently we are just blindly sequentially allocate memory without freeing it
        public GraphicsMemoryAllocation AllocateBuffer(GraphicsHeapType heapType, int sizeInBytes)
        {
            var allocationInfos = this.graphicsService.GetBufferAllocationInfos(sizeInBytes);
            var memoryAllocator = this.globalGpuMemoryAllocator;

            if (heapType == GraphicsHeapType.Upload)
            {
                memoryAllocator = this.globalUploadMemoryAllocator;
            }

            else if (heapType == GraphicsHeapType.ReadBack)
            {
                memoryAllocator = this.globalReadBackMemoryAllocator;
            }

            return memoryAllocator.AllocateMemory(allocationInfos.SizeInBytes, (ulong)allocationInfos.Alignment);
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

        public void FreeAllocation(in GraphicsMemoryAllocation allocation)
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

            memoryAllocator.FreeMemory(in allocation);

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
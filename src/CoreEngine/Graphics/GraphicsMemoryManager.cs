using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    public class GraphicsMemoryManager : IDisposable
    {
        private readonly IGraphicsService graphicsService;
        private readonly GraphicsManager graphicsManager;

        private bool isDisposed;

        private IGraphicsMemoryAllocator globalGpuMemoryAllocator;
        private IGraphicsMemoryAllocator globalTransientGpuMemoryAllocator;
        private IGraphicsMemoryAllocator globalUploadMemoryAllocator;
        private IGraphicsMemoryAllocator globalReadBackMemoryAllocator;

        public GraphicsMemoryManager(GraphicsManager graphicsManager, IGraphicsService graphicsService)
        {
            this.graphicsManager = graphicsManager;
            this.graphicsService = graphicsService;

            // TODO: Write a paging system with pages of 256MB? (see DirectX12Allocator)

            this.globalGpuMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsManager, graphicsService, GraphicsHeapType.Gpu, Utils.MegaBytesToBytes(256), "GlobalGpuHeap");
            this.globalTransientGpuMemoryAllocator = new TransientGraphicsMemoryAllocator(graphicsManager, graphicsService, GraphicsHeapType.Gpu, Utils.MegaBytesToBytes(100), "GlobalTransientGpuHeap");
            this.globalUploadMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsManager, graphicsService, GraphicsHeapType.Upload, Utils.MegaBytesToBytes(175), "GlobalUploadHeap");
            this.globalReadBackMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsManager, graphicsService, GraphicsHeapType.ReadBack, Utils.MegaBytesToBytes(32), "GlobalReadBackHeap");

            // this.globalGpuMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.Gpu, Utils.GigaBytesToBytes(1), "GlobalGpuHeap");
            // this.globalTransientGpuMemoryAllocator = new TransientGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.Gpu, Utils.GigaBytesToBytes(1), "GlobalTransientGpuHeap");
            // this.globalUploadMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.Upload, Utils.GigaBytesToBytes(1), "GlobalUploadHeap");
            // this.globalReadBackMemoryAllocator = new BlockGraphicsMemoryAllocator(graphicsService, GraphicsHeapType.ReadBack, Utils.MegaBytesToBytes(32), "GlobalReadBackHeap");
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
        public ulong AllocatedTransientGpuMemory => this.globalTransientGpuMemoryAllocator.AllocatedMemory;
        public ulong AllocatedCpuMemory => (this.globalUploadMemoryAllocator.AllocatedMemory + this.globalReadBackMemoryAllocator.AllocatedMemory);

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
using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    class BlockGraphicsMemoryAllocator : IGraphicsMemoryAllocator, IDisposable
    {
        private readonly IGraphicsService graphicsService;
        private readonly GraphicsManager graphicsManager;

        private bool isDisposed;

        public BlockGraphicsMemoryAllocator(GraphicsManager graphicsManager, IGraphicsService graphicsService, GraphicsHeapType heapType, ulong sizeInBytes, string label)
        {
            this.graphicsManager = graphicsManager;
            this.graphicsService = graphicsService;
            this.CurrentOffset = 0;

            this.GraphicsHeap = this.graphicsManager.CreateGraphicsHeap(heapType, sizeInBytes, label);
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
                this.graphicsManager.ScheduleDeleteGraphicsHeap(this.GraphicsHeap);
                this.isDisposed = true;
            }
        }

        public GraphicsMemoryAllocation AllocateMemory(int sizeInBytes, ulong alignment)
        {
            // TODO: Find free block
            var alignedHeapOffset = Utils.AlignValue(this.CurrentOffset, alignment);

            if ((alignedHeapOffset + (ulong)sizeInBytes) > this.GraphicsHeap.SizeInBytes)
            {
                throw new InvalidOperationException($"The are not enough free memory on graphics heap '{this.GraphicsHeap.Label}'.");
            }

            var allocation = new GraphicsMemoryAllocation(this, this.GraphicsHeap, alignedHeapOffset, sizeInBytes, isAliasable: false);

            this.AllocatedMemory += (ulong)sizeInBytes;
            this.CurrentOffset = alignedHeapOffset + (ulong)sizeInBytes;

            return allocation;
        }

        public void FreeMemory(GraphicsMemoryAllocation allocation)
        {
            //this.AllocatedMemory -= (ulong)allocation.SizeInBytes;
        }

        public void Reset(uint frameNumber)
        {

        }

        public GraphicsHeap GraphicsHeap { get; }
        public ulong CurrentOffset { get; private set; }
        public ulong AllocatedMemory { get; private set; }
    }
}
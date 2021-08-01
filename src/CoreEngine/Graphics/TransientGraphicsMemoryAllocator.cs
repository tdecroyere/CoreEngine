using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    class TransientGraphicsMemoryAllocator : IGraphicsMemoryAllocator, IDisposable
    {
        private readonly GraphicsManager graphicsManager;

        private bool isDisposed;

        public TransientGraphicsMemoryAllocator(GraphicsManager graphicsManager, GraphicsHeapType heapType, ulong sizeInBytes, string label)
        {
            this.graphicsManager = graphicsManager;
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
                this.CurrentOffset = 0;
                alignedHeapOffset = Utils.AlignValue(this.CurrentOffset, alignment);
                // throw new InvalidOperationException($"The are not enough free memory on graphics heap '{this.GraphicsHeap.Label}'.");
            }

            var allocation = new GraphicsMemoryAllocation(this, this.GraphicsHeap, alignedHeapOffset, sizeInBytes, isAliasable: true);

            this.AllocatedMemory += (ulong)sizeInBytes;
            this.CurrentOffset = alignedHeapOffset + (ulong)sizeInBytes;

            return allocation;
        }

        public void FreeMemory(in GraphicsMemoryAllocation allocation)
        {

        }

        public void Reset(uint frameNumber)
        {
            this.AllocatedMemory = 0;
            this.StartOffset = this.CurrentOffset;

            // this.GraphicsHeap = ((frameNumber % 2) == 1) ? this.GraphicsHeap1 : this.GraphicsHeap0;
        }

        public GraphicsHeap GraphicsHeap { get; private set; }
        public ulong CurrentOffset { get; private set; }
        public ulong StartOffset { get; private set; }

        public ulong AllocatedMemory { get; private set; }
        public ulong TotalMemory { get; private set; }
    }
}
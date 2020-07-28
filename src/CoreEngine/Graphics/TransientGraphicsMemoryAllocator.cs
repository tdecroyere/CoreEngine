using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    class TransientGraphicsMemoryAllocator : IGraphicsMemoryAllocator
    {
        private readonly IGraphicsService graphicsService;

        public TransientGraphicsMemoryAllocator(IGraphicsService graphicsService, uint graphicsResourceId, GraphicsHeapType heapType, ulong sizeInBytes, string label)
        {
            this.graphicsService = graphicsService;

            this.CurrentOffset = 0;
            
            if(!this.graphicsService.CreateGraphicsHeap(graphicsResourceId, (GraphicsServiceHeapType)heapType, sizeInBytes))
            {
                throw new InvalidOperationException($"Cannot create {label}.");
            }

            this.graphicsService.SetGraphicsHeapLabel(graphicsResourceId, label);

            // TODO: Do something better here
            this.GraphicsHeap0 = new GraphicsHeap(graphicsResourceId, heapType, sizeInBytes, $"{label}0");
            // this.GraphicsHeap1 = new GraphicsHeap(graphicsResourceId, heapType, sizeInBytes, $"{label}1");

            this.GraphicsHeap = this.GraphicsHeap0;
        }

        public GraphicsMemoryAllocation AllocateMemory(int sizeInBytes, ulong alignment)
        {
            // TODO: Find free block
            var alignedHeapOffset = Utils.AlignValue(this.CurrentOffset, alignment);

            if ((alignedHeapOffset + (ulong)sizeInBytes) > this.GraphicsHeap.SizeInBytes)
            {
                throw new InvalidOperationException($"The are not enough free memory on graphics heap '{this.GraphicsHeap.Label}'.");
            }

            var allocation = new GraphicsMemoryAllocation(this, this.GraphicsHeap, alignedHeapOffset, sizeInBytes, isAliasable: true);

            this.AllocatedMemory += (alignedHeapOffset - this.AllocatedMemory) + (ulong)sizeInBytes;
            this.CurrentOffset = alignedHeapOffset + (ulong)sizeInBytes;

            return allocation;
        }

        public void Reset(uint frameNumber)
        {
            this.CurrentOffset = 0;
            this.AllocatedMemory = 0;

            // this.GraphicsHeap = ((frameNumber % 2) == 1) ? this.GraphicsHeap1 : this.GraphicsHeap0;

        }

        public GraphicsHeap GraphicsHeap { get; private set; }
        public GraphicsHeap GraphicsHeap0 { get; }
        public GraphicsHeap GraphicsHeap1 { get; }
        public ulong CurrentOffset { get; private set; }
        public ulong AllocatedMemory { get; private set; }
    }
}
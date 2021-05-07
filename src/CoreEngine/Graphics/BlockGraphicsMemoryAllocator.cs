using System;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    class BlockGraphicsMemoryAllocator : IGraphicsMemoryAllocator
    {
        private readonly IGraphicsService graphicsService;

        public BlockGraphicsMemoryAllocator(IGraphicsService graphicsService, GraphicsHeapType heapType, ulong sizeInBytes, string label)
        {
            this.graphicsService = graphicsService;
            this.CurrentOffset = 0;

            var nativePointer = this.graphicsService.CreateGraphicsHeap((GraphicsServiceHeapType)heapType, sizeInBytes);
            
            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Cannot create {label}.");
            }

            this.graphicsService.SetGraphicsHeapLabel(nativePointer, label);
            this.GraphicsHeap = new GraphicsHeap(nativePointer, heapType, sizeInBytes, label);
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
using System;
using System.Collections.Generic;
using CoreEngine.Diagnostics;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now
    class GraphicsHeapAllocationInfo
    {
        public GraphicsHeapAllocationInfo(GraphicsHeap graphicsHeap, ulong sizeInBytes)
        {
            this.GraphicsHeap = graphicsHeap;
            this.SizeInBytes = sizeInBytes;
        }

        public GraphicsHeap GraphicsHeap { get; }
        public ulong SizeInBytes { get;}
        public ulong CurrentOffset { get; set; }
        
        public ulong AvailableMemory 
        { 
            get
            {
                return this.SizeInBytes - this.CurrentOffset;
            }
        }
    }

    class BlockGraphicsMemoryAllocator : IGraphicsMemoryAllocator, IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private readonly GraphicsHeapType heapType;
        private readonly string label;
        private readonly ulong minimumBlockSizeInBytes;
        private readonly IList<GraphicsHeapAllocationInfo> graphicsHeaps;

        private bool isDisposed;

        public BlockGraphicsMemoryAllocator(GraphicsManager graphicsManager, GraphicsHeapType heapType, ulong minimumBlockSizeInBytes, string label)
        {
            this.graphicsManager = graphicsManager;
            this.heapType = heapType;
            this.label = label;
            this.minimumBlockSizeInBytes = minimumBlockSizeInBytes;
            this.graphicsHeaps = new List<GraphicsHeapAllocationInfo>();

            AllocateGraphicsHeap(minimumBlockSizeInBytes);
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
                foreach (var graphicsHeap in this.graphicsHeaps)
                {
                    this.graphicsManager.ScheduleDeleteGraphicsHeap(graphicsHeap.GraphicsHeap);
                }

                this.isDisposed = true;
            }
        }

        public ulong AllocatedMemory { get; private set; }
        public ulong TotalMemory { get; private set; }

        public GraphicsMemoryAllocation AllocateMemory(int sizeInBytes, ulong alignment)
        {
            var allocation = AllocateHeap(sizeInBytes, alignment);

            if (allocation != null)
            {
                return allocation.Value;
            }

            var heapSize = (ulong)MathF.Max(ComputeBlockSize((ulong)sizeInBytes), this.minimumBlockSizeInBytes);

            // TODO: Hack to try to get the driver to put the heap to discrete videomem :)
            if (heapSize >= Utils.MegaBytesToBytes(512))
            {
                heapSize = (ulong)sizeInBytes;
            }

            Logger.WriteMessage($"Allocating new heap: {Utils.BytesToMegaBytes(heapSize)} MB (Resource: {Utils.BytesToMegaBytes((ulong)sizeInBytes)} MB)");

            AllocateGraphicsHeap(heapSize);
            allocation = AllocateHeap(sizeInBytes, alignment);

            if (allocation != null)
            {
                return allocation.Value;
            }

            throw new InvalidOperationException("Not enough memory");
        }

        public void FreeMemory(in GraphicsMemoryAllocation allocation)
        {
            //this.AllocatedMemory -= (ulong)allocation.SizeInBytes;
        }

        public void Reset(uint frameNumber)
        {

        }

        private void AllocateGraphicsHeap(ulong sizeInBytes)
        {
            var graphicsHeap = this.graphicsManager.CreateGraphicsHeap(this.heapType, sizeInBytes, this.label);
            this.graphicsHeaps.Add(new GraphicsHeapAllocationInfo(graphicsHeap, sizeInBytes));

            this.TotalMemory += sizeInBytes;
        }

        private GraphicsMemoryAllocation? AllocateHeap(int sizeInBytes, ulong alignment)
        {
            foreach (var graphicsHeap in this.graphicsHeaps)
            {
                // TODO: Find free block
                var alignedHeapOffset = Utils.AlignValue(graphicsHeap.CurrentOffset, alignment);

                if ((alignedHeapOffset + (ulong)sizeInBytes) > graphicsHeap.GraphicsHeap.SizeInBytes)
                {
                    continue;
                    // throw new InvalidOperationException($"The are not enough free memory on graphics heap '{graphicsHeap.GraphicsHeap.Label}'.");
                }

                var allocation = new GraphicsMemoryAllocation(this, graphicsHeap.GraphicsHeap, alignedHeapOffset, sizeInBytes, isAliasable: false);

                this.AllocatedMemory += (ulong)sizeInBytes;
                graphicsHeap.CurrentOffset = alignedHeapOffset + (ulong)sizeInBytes;

                return allocation;
            }

            return null;
        }

        private static ulong ComputeBlockSize(ulong sizeInBytes)
        {
            // decrement `n` (to handle the case when `n` itself is a power of 2)
            sizeInBytes--;
        
            // set all bits after the last set bit
            sizeInBytes |= sizeInBytes >> 1;
            sizeInBytes |= sizeInBytes >> 2;
            sizeInBytes |= sizeInBytes >> 4;
            sizeInBytes |= sizeInBytes >> 8;
            sizeInBytes |= sizeInBytes >> 16;
        
            // increment `n` and return
            return ++sizeInBytes;
        }
    }
}
using System;

namespace CoreEngine.Graphics
{
    public interface IGraphicsMemoryAllocator : IDisposable
    {
        ulong AllocatedMemory { get; }
        ulong TotalMemory { get; }

        GraphicsMemoryAllocation AllocateMemory(int sizeInBytes, ulong alignment);
        void FreeMemory(GraphicsMemoryAllocation allocation);
        void Reset(uint frameNumber);
    }
}
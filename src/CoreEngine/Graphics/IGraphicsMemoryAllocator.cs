namespace CoreEngine.Graphics
{
    public interface IGraphicsMemoryAllocator
    {
        ulong AllocatedMemory { get; }

        GraphicsMemoryAllocation AllocateMemory(int sizeInBytes, ulong alignment);
        void FreeMemory(GraphicsMemoryAllocation allocation);
        void Reset(uint frameNumber);
    }
}
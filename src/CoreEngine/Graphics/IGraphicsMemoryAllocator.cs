namespace CoreEngine.Graphics
{
    public interface IGraphicsMemoryAllocator
    {
        ulong AllocatedMemory { get; }

        GraphicsMemoryAllocation AllocateMemory(int sizeInBytes, ulong alignment);
        void Reset(uint frameNumber);
    }
}
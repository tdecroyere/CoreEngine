namespace CoreEngine.Graphics
{
    public readonly struct GraphicsMemoryAllocation
    {
        public GraphicsMemoryAllocation(IGraphicsMemoryAllocator memoryAllocator, GraphicsHeap graphicsHeap, ulong offset, int sizeInBytes, bool isAliasable)
        {
            this.MemoryAllocator = memoryAllocator;
            this.GraphicsHeap = graphicsHeap;
            this.Offset = offset;
            this.SizeInBytes = sizeInBytes;
            this.IsAliasable = isAliasable;
        }

        public IGraphicsMemoryAllocator MemoryAllocator { get; }
        public GraphicsHeap GraphicsHeap { get; }
        public ulong Offset { get; }
        public int SizeInBytes { get; }
        public bool IsAliasable { get; }
    }
}
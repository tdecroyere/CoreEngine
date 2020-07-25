namespace CoreEngine.Graphics
{
    public readonly struct GraphicsMemoryAllocation
    {
        public GraphicsMemoryAllocation(GraphicsHeap graphicsHeap, ulong offset, int sizeInBytes)
        {
            this.GraphicsHeap = graphicsHeap;
            this.Offset = offset;
            this.SizeInBytes = sizeInBytes;
        }

        public GraphicsHeap GraphicsHeap { get; }
        public ulong Offset { get; }
        public int SizeInBytes { get; }
    }
}
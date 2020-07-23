namespace CoreEngine.Graphics
{
    public readonly struct GraphicsMemoryAllocation
    {
        public GraphicsMemoryAllocation(GraphicsHeap graphicsHeap, ulong offset)
        {
            this.GraphicsHeap = graphicsHeap;
            this.Offset = offset;
        }

        public GraphicsHeap GraphicsHeap { get; }
        public ulong Offset { get; }
    }
}
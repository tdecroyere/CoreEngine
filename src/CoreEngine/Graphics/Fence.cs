namespace CoreEngine.Graphics
{
    public readonly struct Fence
    {
        public Fence(CommandQueue commandQueue, ulong value)
        {
            this.CommandQueue = commandQueue;
            this.Value = value;
        }

        public CommandQueue CommandQueue { get; }
        public ulong Value { get; }
    }
}
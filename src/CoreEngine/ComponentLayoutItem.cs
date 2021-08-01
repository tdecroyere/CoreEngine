namespace CoreEngine
{
    public readonly record struct ComponentLayoutItem
    {
        internal ComponentLayoutItem(ComponentHash hash, int offset, int sizeInBytes, ReadOnlyMemory<byte>? defaultData)
        {
            this.Hash = hash;
            this.Offset = offset;
            this.SizeInBytes = sizeInBytes;
            this.DefaultData = defaultData;
        }

        public ComponentHash Hash { get; }
        public int Offset { get; }
        public int SizeInBytes { get; }
        public ReadOnlyMemory<byte>? DefaultData { get; }
    }
}
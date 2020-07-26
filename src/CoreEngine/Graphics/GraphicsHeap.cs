using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsHeap
    {
        internal GraphicsHeap(uint id, GraphicsHeapType type, ulong sizeInBytes, string label)
        {
            this.Id = id;
            this.Type = type;
            this.SizeInBytes = sizeInBytes;
            this.Label = label;
        }

        public readonly uint Id { get; }
        public readonly GraphicsHeapType Type { get; }
        public readonly ulong SizeInBytes { get; }
        public readonly string Label { get; }
    }
}
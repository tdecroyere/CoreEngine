using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsHeap
    {
        internal GraphicsHeap(uint id, GraphicsHeapType type, ulong length)
        {
            this.Id = id;
            this.Type = type;
            this.Length = length;
        }

        public readonly uint Id { get; }
        public readonly GraphicsHeapType Type { get; }
        public readonly ulong Length { get; }
    }
}
using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsHeap
    {
        internal GraphicsHeap(IntPtr nativePointer, GraphicsHeapType type, ulong sizeInBytes, string label)
        {
            this.NativePointer = nativePointer;
            this.Type = type;
            this.SizeInBytes = sizeInBytes;
            this.Label = label;
        }

        public readonly IntPtr NativePointer { get; }
        public readonly GraphicsHeapType Type { get; }
        public readonly ulong SizeInBytes { get; }
        public readonly string Label { get; }
    }
}
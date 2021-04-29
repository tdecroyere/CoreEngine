using System;

namespace CoreEngine.Graphics
{
    public readonly struct ShaderResourceHeap
    {
        internal ShaderResourceHeap(IntPtr nativePointer, ulong length, string label)
        {
            this.NativePointer = nativePointer;
            this.Length = length;
            this.Label = label;
        }

        public readonly IntPtr NativePointer { get; }
        public readonly ulong Length { get; }
        public readonly string Label { get; }
    }
}
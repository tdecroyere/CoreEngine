using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandQueue
    {
        internal CommandQueue(IntPtr nativePointer, CommandType type, string label)
        {
            this.NativePointer = nativePointer;
            this.Type = type;
            this.Label = label;
        }

        public readonly IntPtr NativePointer { get; }
        public readonly CommandType Type { get; }
        public readonly string Label { get; }
    }
}
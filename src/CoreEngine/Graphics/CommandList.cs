using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandList
    {
        internal CommandList(IntPtr nativePointer, CommandType type, CommandQueue commandQueue, string label)
        {
            this.NativePointer = nativePointer;
            this.Type = type;
            this.CommandQueue = commandQueue;
            this.Label = label;
        }

        public IntPtr NativePointer { get; }
        public CommandType Type { get; }
        public CommandQueue CommandQueue { get; }
        public string Label { get; }
    }
}
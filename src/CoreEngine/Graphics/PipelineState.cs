using System;

namespace CoreEngine.Graphics
{
    public readonly struct PipelineState
    {
        public PipelineState(IntPtr nativePointer)
        {
            this.NativePointer = nativePointer;
        }

        public readonly IntPtr NativePointer { get; }
    }
}
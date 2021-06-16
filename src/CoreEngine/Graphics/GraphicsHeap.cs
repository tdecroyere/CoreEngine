using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsHeap : IDisposable
    {
        private readonly GraphicsManager graphicsManager;

        internal GraphicsHeap(GraphicsManager graphicsManager, IntPtr nativePointer, GraphicsHeapType type, ulong sizeInBytes, string label)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer = nativePointer;
            this.Type = type;
            this.SizeInBytes = sizeInBytes;
            this.Label = label;
        }

        public void Dispose()
        {
            this.graphicsManager.ScheduleDeleteGraphicsHeap(this);
            GC.SuppressFinalize(this);
        }

        public readonly IntPtr NativePointer { get; }
        public readonly GraphicsHeapType Type { get; }
        public readonly ulong SizeInBytes { get; }
        public readonly string Label { get; }
    }
}
using System;

namespace CoreEngine.Graphics
{
    public readonly struct PipelineState : IDisposable
    {
        private readonly GraphicsManager graphicsManager;

        public PipelineState(GraphicsManager graphicsManager, IntPtr nativePointer, string label)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer = nativePointer;
            this.Label = label;
        }

        public void Dispose()
        {
            this.graphicsManager.ScheduleDeletePipelineState(this);
            GC.SuppressFinalize(this);
        }

        public readonly IntPtr NativePointer { get; }
        public readonly string Label { get; }
    }
}
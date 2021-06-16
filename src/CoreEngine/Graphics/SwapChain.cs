using System;

namespace CoreEngine.Graphics
{
    public class SwapChain : IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private bool isDisposed;

        internal SwapChain(GraphicsManager graphicsManager, IntPtr nativePointer, CommandQueue commandQueue, int width, int height, TextureFormat textureFormat)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer = nativePointer;
            this.CommandQueue = commandQueue;
            this.Width = width;
            this.Height = height;
            this.TextureFormat = textureFormat;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing && !this.isDisposed)
            {
                this.graphicsManager.ScheduleDeleteSwapChain(this);
                this.isDisposed = true;
            }
        }

        public IntPtr NativePointer { get; }
        public CommandQueue CommandQueue { get; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public TextureFormat TextureFormat { get; }
    }
}
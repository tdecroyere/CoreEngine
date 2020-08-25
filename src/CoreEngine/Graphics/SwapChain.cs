using System;

namespace CoreEngine.Graphics
{
    public readonly struct SwapChain
    {
        internal SwapChain(IntPtr nativePointer, CommandQueue commandQueue, int width, int height, TextureFormat textureFormat)
        {
            this.NativePointer = nativePointer;
            this.CommandQueue = commandQueue;
            this.Width = width;
            this.Height = height;
            this.TextureFormat = textureFormat;
        }

        public IntPtr NativePointer { get; }
        public CommandQueue CommandQueue { get; }
        public int Width { get; }
        public int Height { get; }
        public TextureFormat TextureFormat { get; }
    }
}
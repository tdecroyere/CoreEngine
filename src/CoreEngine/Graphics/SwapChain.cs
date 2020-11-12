using System;

namespace CoreEngine.Graphics
{
    public class SwapChain
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
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public TextureFormat TextureFormat { get; }
    }
}
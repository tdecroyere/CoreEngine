using System;

namespace CoreEngine.UI.Native
{
    public readonly struct Window
    {
        public Window(IntPtr nativePointer, string title, int width, int height)
        {
            this.NativePointer = nativePointer;
            this.Title = title;
            this.Width = width;
            this.Height = height;
        }

        public IntPtr NativePointer { get; }
        public string Title { get; }
        public int Width { get; }
        public int Height { get; }
    }
}
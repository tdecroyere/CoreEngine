using System;
using System.Numerics;

namespace CoreEngine.HostServices
{
    public enum NativeWindowState
    {
        Normal,
        Maximized
    }

    public readonly struct NativeAppStatus
    {
        public bool IsRunning { get; }
        public bool IsActive { get; }
    }

    public interface INativeUIService
    {
        IntPtr CreateWindow(string title, int width, int height, NativeWindowState windowState);
        Vector2 GetWindowRenderSize(IntPtr windowPointer);
        NativeAppStatus ProcessSystemMessages();
    }
}
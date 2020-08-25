using System;
using System.Numerics;

namespace CoreEngine.HostServices
{
    public interface INativeUIService
    {
        IntPtr CreateWindow(string title, int width, int height);
        Vector2 GetWindowRenderSize(IntPtr windowPointer);
    }
}
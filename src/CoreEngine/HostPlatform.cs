using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    // TODO: Find a way to hide that to external assemblies

    public struct ByteSpan
    {
        public IntPtr Pointer;
        public int Length;

        public unsafe static implicit operator Span<byte>(ByteSpan value)
        {
            return new Span<byte>(value.Pointer.ToPointer(), value.Length);
        }
    }
    public delegate int AddTestHostMethodDelegate(int a, int b);
    public delegate ByteSpan GetTestBufferDelegate();

    public struct HostPlatform
    {
        public int TestParameter;
        public AddTestHostMethodDelegate AddTestHostMethod;
        public GetTestBufferDelegate GetTestBuffer;
        public GraphicsService GraphicsService;
    }

    public delegate void DebugDrawTriangleDelegate(Vector4 color1, Vector4 color2, Vector4 color3, Matrix4x4 worldMatrix);

    public struct GraphicsService
    {
        public DebugDrawTriangleDelegate DebugDrawTriange;
    }
}
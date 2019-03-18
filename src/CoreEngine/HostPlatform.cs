using System;
using System.Runtime.InteropServices;

namespace CoreEngine
{
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
    }
}
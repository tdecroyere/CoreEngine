using System;
using System.Numerics;

namespace CoreEngine
{
    public static class Utils
    {
        public static ulong AlignValue(ulong value, ulong alignment)
        {
            return (value + (alignment - (value % alignment)) % alignment);
        }

        public static ulong GigaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024 * 1024;
        }
    }
}
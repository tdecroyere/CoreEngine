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

        public static float BytesToMegaBytes(ulong value)
        {
            return (float)value / 1024.0f / 1024.0f;
        }

        public static ulong MegaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024;
        }

        public static ulong GigaBytesToBytes(ulong value)
        {
            return value * 1024 * 1024 * 1024;
        }
    }
}
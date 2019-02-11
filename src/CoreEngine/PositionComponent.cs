using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PositionComponent : IComponentData
    {
        public Vector3 Position;
    }
}
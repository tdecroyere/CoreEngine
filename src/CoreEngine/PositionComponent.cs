using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public struct PositionComponent : IComponentData
    {
        public Vector3 Position;
    }
}
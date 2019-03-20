using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public struct TransformComponent : IComponentData
    {
        public Vector3 Position;
        public Matrix4x4 WorldMatrix;
    }
}
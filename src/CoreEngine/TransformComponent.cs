using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public struct TransformComponent : IComponentData
    {
        public Vector3 Position;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public Matrix4x4 WorldMatrix;
    }
}
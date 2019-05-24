using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public struct TransformComponent : IComponentData
    {
        public Vector3 Position;
        public Vector3 Scale;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public Matrix4x4 WorldMatrix;

        public void SetDefaultValues()
        {
            this.Scale = Vector3.One;
            this.WorldMatrix = Matrix4x4.Identity;
        }
    }
}
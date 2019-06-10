using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public struct TransformComponent : IComponentData
    {
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public Quaternion RotationQuaternion { get; set; }
        public Matrix4x4 WorldMatrix { get; set; }

        public void SetDefaultValues()
        {
            this.Scale = Vector3.One;
            this.WorldMatrix = Matrix4x4.Identity;
        }
    }
}
using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class MeshInstance
    {
        public MeshInstance(Mesh mesh, Matrix4x4 worldMatrix, uint objectPropertiesIndex)
        {
            this.Mesh = mesh;
            this.WorldMatrix = worldMatrix;
            this.IsAlive = true;
            this.IsDirty = true;
            this.ObjectPropertiesIndex = objectPropertiesIndex;
        }

        public Mesh Mesh { get; internal set; }
        public Matrix4x4 WorldMatrix { get; internal set; }
        public bool IsAlive { get; internal set; }
        public bool IsDirty { get; internal set; }
        public uint ObjectPropertiesIndex { get; }
    }
}
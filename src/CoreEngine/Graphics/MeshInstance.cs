using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class MeshInstance
    {
        public MeshInstance(Entity entity, Mesh mesh, Matrix4x4 worldMatrix, int objectPropertiesIndex)
        {
            this.Entity = entity;
            this.Mesh = mesh;
            this.WorldMatrix = worldMatrix;
            this.IsAlive = true;
            this.IsDirty = true;
            this.ObjectPropertiesIndex = objectPropertiesIndex;
        }

        public Entity Entity { get; }
        public Mesh Mesh { get; internal set; }
        public Matrix4x4 WorldMatrix { get; internal set; }
        public bool IsAlive { get; internal set; }
        public bool IsDirty { get; internal set; }
        public int ObjectPropertiesIndex { get; }
    }
}
using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class MeshInstance
    {
        public MeshInstance(Entity entity, Mesh mesh, Matrix4x4 worldMatrix)
        {
            this.Entity = entity;
            this.Mesh = mesh;
            this.WorldMatrix = worldMatrix;
            this.IsAlive = true;
        }

        public Entity Entity { get; }
        public Mesh Mesh { get; }
        public Matrix4x4 WorldMatrix { get; internal set; }
        public bool IsAlive { get; internal set; }
    }
}
using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics
{
    public class MeshInstance : TrackedItem
    {
        private Matrix4x4 worldMatrix;

        public MeshInstance(Mesh mesh, Matrix4x4 worldMatrix, uint objectPropertiesIndex)
        {
            this.Mesh = mesh;
            this.ObjectPropertiesIndex = objectPropertiesIndex;
            this.worldMatrix = worldMatrix;
        }

        public Mesh Mesh { get; }
        public uint ObjectPropertiesIndex { get; }

        public Matrix4x4 WorldMatrix 
        { 
            get
            {
                return this.worldMatrix;
            }

            set
            {
                UpdateField(ref this.worldMatrix, value);
            }
        }
    }
}
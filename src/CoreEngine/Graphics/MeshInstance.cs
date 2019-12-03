using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics
{
    public class MeshInstance : TrackedItem
    {
        private Matrix4x4 worldMatrix;

        public MeshInstance(Mesh mesh, Matrix4x4 worldMatrix, bool alwaysAlive = true)
        {
            this.Mesh = mesh;
            this.worldMatrix = worldMatrix;
            this.AlwaysAlive = alwaysAlive;
            this.WorldBoundingBoxList = new List<BoundingBox>();
        }

        public Mesh Mesh { get; }

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
        
        internal IList<BoundingBox> WorldBoundingBoxList { get; }
    }
}
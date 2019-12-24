using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Collections;
using CoreEngine.Diagnostics;

namespace CoreEngine.Graphics
{
    public class MeshInstance : TrackedItem
    {
        private bool isMeshLoaded;
        private Matrix4x4 worldMatrix;

        public MeshInstance(Mesh mesh, Material? material, Matrix4x4 worldMatrix, bool alwaysAlive = true)
        {
            this.Mesh = mesh;
            this.Material = material;
            this.worldMatrix = worldMatrix;
            this.AlwaysAlive = alwaysAlive;
            this.WorldBoundingBoxList = new List<BoundingBox>();
        }

        public Mesh Mesh { get; }
        public Material? Material { get; }

        public Matrix4x4 WorldMatrix 
        { 
            get
            {
                return this.worldMatrix;
            }

            set
            {
                UpdateField(ref this.worldMatrix, value);
                UpdateField(ref this.isMeshLoaded, this.Mesh.IsLoaded);
            }
        }
        
        internal IList<BoundingBox> WorldBoundingBoxList { get; }
    }
}
using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Collections;
using CoreEngine.Diagnostics;

namespace CoreEngine.Rendering
{
    public class MeshInstance : TrackedItem
    {
        private bool isMeshLoaded;
        private Matrix4x4 worldMatrix;
        private Matrix4x4 worldInvTransposeMatrix;

        public MeshInstance(Mesh mesh, Material? material, Matrix4x4 worldMatrix, bool alwaysAlive = true)
        {
            this.Mesh = mesh;
            this.Material = material;
            this.worldMatrix = worldMatrix;
            this.worldInvTransposeMatrix = ComputeInverseTransposeMatrix(worldMatrix);
            this.AlwaysAlive = alwaysAlive;
            this.WorldBoundingBoxList = new List<BoundingBox>();
        }

        public Mesh Mesh { get; }
        public Material? Material { get; }
        public float Scale { get; set; }

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
                UpdateField(ref this.worldInvTransposeMatrix, ComputeInverseTransposeMatrix(this.worldMatrix));
            }
        }

        public Matrix4x4 WorldInvTransposeMatrix 
        { 
            get
            {
                return this.worldInvTransposeMatrix;
            }
        }
        
        internal BoundingBox WorldBoundingBox { get; set; }
        internal IList<BoundingBox> WorldBoundingBoxList { get; }

        private static Matrix4x4 ComputeInverseTransposeMatrix(Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var inverseMatrix);
            return Matrix4x4.Transpose(inverseMatrix);
        }
    }
}
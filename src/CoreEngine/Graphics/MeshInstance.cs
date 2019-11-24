using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics
{
    public class MeshInstance : TrackedItem
    {
        private Matrix4x4 worldMatrix;

        public MeshInstance(Mesh mesh, Matrix4x4 worldMatrix, uint objectPropertiesIndex, bool alwaysAlive = true)
        {
            this.Mesh = mesh;
            this.ObjectPropertiesIndex = objectPropertiesIndex;
            this.worldMatrix = worldMatrix;
            this.AlwaysAlive = alwaysAlive;
            this.WorldBoundingBoxList = new List<BoundingBox>();
            this.BoundingBoxMeshList = new List<ItemIdentifier>();
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
        
        internal IList<BoundingBox> WorldBoundingBoxList { get; }
        internal IList<ItemIdentifier> BoundingBoxMeshList { get; }
    }
}
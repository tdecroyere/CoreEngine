using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics
{
    public class Camera : TrackedItem
    {
        private Vector3 worldPosition;
        private Matrix4x4 viewMatrix;
        private Matrix4x4 projectionMatrix;

        public Camera(Vector3 worldPosition, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            this.worldPosition = worldPosition;
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
            this.BoundingFrustum = new BoundingFrustum(viewMatrix * projectionMatrix);
        }

        public Vector3 WorldPosition 
        { 
            get
            {
                return this.worldPosition;
            } 
            
            set
            {
                UpdateField(ref this.worldPosition, value);
            } 
        }

        public Matrix4x4 ViewMatrix 
        { 
            get
            {
                return this.viewMatrix;
            } 
            
            set
            {
                UpdateField(ref this.viewMatrix, value);
            } 
        }

        public Matrix4x4 ProjectionMatrix 
        { 
            get
            {
                return this.projectionMatrix;
            } 
            
            set
            {
                UpdateField(ref this.projectionMatrix, value);
            } 
        }

        public BoundingFrustum BoundingFrustum { get; set; }
    }
}
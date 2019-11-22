using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics
{
    public class Camera : TrackedItem
    {
        private Matrix4x4 viewMatrix;
        private Matrix4x4 projectionMatrix;

        public Camera(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
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
    }
}
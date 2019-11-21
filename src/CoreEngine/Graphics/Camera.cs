using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class Camera
    {
        private Matrix4x4 viewMatrix;
        private Matrix4x4 projectionMatrix;

        public Camera(Entity entity, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            this.Entity = entity;
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
        }

        public Entity Entity { get; }

        public Matrix4x4 ViewMatrix 
        { 
            get
            {
                return this.viewMatrix;
            } 
            
            set
            {
                if (this.viewMatrix != value)
                {
                    this.viewMatrix = value;
                    this.IsDirty = true;
                }
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
                if (this.projectionMatrix != value)
                {
                    this.projectionMatrix = value;
                    this.IsDirty = true;
                }
            } 
        }

        public bool IsAlive { get; internal set; }
        public bool IsDirty { get; internal set; }
    }
}
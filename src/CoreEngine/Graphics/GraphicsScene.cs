using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics
{
    public class GraphicsScene
    {
        private Camera? activeCamera;

        public GraphicsScene()
        {
            this.activeCamera = null;
            this.Cameras = new ItemCollection<Camera>();
            this.MeshInstances = new ItemCollection<MeshInstance>();
        }

        // TODO: Always auto-delete cameras that are not alive anymore
        public ItemCollection<Camera> Cameras { get; }
        public ItemCollection<MeshInstance> MeshInstances { get; }

        public Camera? ActiveCamera 
        { 
            get
            {
                if (this.activeCamera == null && this.Cameras.Count > 0)
                {
                    return this.Cameras[0];
                }

                return this.activeCamera;
            }

            set
            {
                this.activeCamera = value;
            } 
        }

        public void CleanItems()
        {
            this.Cameras.CleanItems();
            this.MeshInstances.CleanItems();
        }

        public void ResetItemsStatus()
        {
            this.Cameras.ResetItemsStatus();
            this.MeshInstances.ResetItemsStatus();
        }
    }
}
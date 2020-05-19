using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering
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

        public Camera? DebugCamera { get; set; }

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

        // TODO: Optimize this! Don't use a deep copy
        // With this code, all properties will be dirty be default 
        // so there will always be a gpu copy
        public GraphicsScene Copy()
        {
            var result = new GraphicsScene();
            
            for (var i = 0; i < this.Cameras.Count; i++)
            {
                result.Cameras.Add(new Camera(this.Cameras[i].WorldPosition, this.Cameras[i].TargetPosition, this.Cameras[i].NearPlaneDistance, this.Cameras[i].FarPlaneDistance, this.Cameras[i].ViewMatrix, this.Cameras[i].ProjectionMatrix, this.Cameras[i].ViewProjectionMatrix));
            }

            for (var i = 0; i < this.MeshInstances.Count; i++)
            {
                var meshInstanceCopy = new MeshInstance(this.MeshInstances[i].Mesh, this.MeshInstances[i].Material, this.MeshInstances[i].WorldMatrix);
                
                for (var j = 0; j < this.MeshInstances[i].WorldBoundingBoxList.Count; j++)
                {
                    meshInstanceCopy.WorldBoundingBoxList.Add(this.MeshInstances[i].WorldBoundingBoxList[j]);
                }

                result.MeshInstances.Add(meshInstanceCopy);
            }

            if (this.activeCamera != null)
            {
                result.ActiveCamera = result.Cameras[this.Cameras.IndexOf(this.activeCamera)];
            }

            if (this.DebugCamera != null)
            {
                result.DebugCamera = result.Cameras[this.Cameras.IndexOf(this.DebugCamera)];
            }

            return result;
        }
    }
}
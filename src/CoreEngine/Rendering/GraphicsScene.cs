using CoreEngine.Collections;
using System.Numerics;

namespace CoreEngine.Rendering
{
    public class GraphicsScene
    {
        public GraphicsScene()
        {
            // TODO: Create a valid default camera
            this.ActiveCamera = new Camera(Vector3.Zero, new Vector3(0, 0, 1), 0, Matrix4x4.Identity, Matrix4x4.Identity, Matrix4x4.Identity);

            this.Cameras = new ItemCollection<Camera>();
            this.Lights = new ItemCollection<Light>();
            this.MeshInstances = new ItemCollection<MeshInstance>();
        }

        public ItemCollection<Camera> Cameras { get; }
        public ItemCollection<Light> Lights { get; }
        public ItemCollection<MeshInstance> MeshInstances { get; }

        public Camera ActiveCamera { get; set; }
        public Camera? DebugCamera { get; set; }
        public uint ShowMeshlets { get; set; }

        public void CleanItems()
        {
            this.Cameras.CleanItems();
            this.Lights.CleanItems();
            this.MeshInstances.CleanItems();
        }

        public void ResetItemsStatus()
        {
            this.Cameras.ResetItemsStatus();
            this.Lights.ResetItemsStatus();
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
                result.Cameras.Add(new Camera(this.Cameras[i].WorldPosition, this.Cameras[i].TargetPosition, this.Cameras[i].NearPlaneDistance, this.Cameras[i].ViewMatrix, this.Cameras[i].ProjectionMatrix, this.Cameras[i].ViewProjectionMatrix));
            }

            for (var i = 0; i < this.Lights.Count; i++)
            {
                result.Lights.Add(new Light(this.Lights[i].WorldPosition, this.Lights[i].Color, this.Lights[i].LightType));
            }

            for (var i = 0; i < this.MeshInstances.Count; i++)
            {
                var meshInstanceCopy = new MeshInstance(this.MeshInstances[i].Mesh, this.MeshInstances[i].Material, this.MeshInstances[i].WorldMatrix);
                meshInstanceCopy.Scale = this.MeshInstances[i].Scale;
                
                for (var j = 0; j < this.MeshInstances[i].WorldBoundingBoxList.Count; j++)
                {
                    meshInstanceCopy.WorldBoundingBoxList.Add(this.MeshInstances[i].WorldBoundingBoxList[j]);
                }

                result.MeshInstances.Add(meshInstanceCopy);
            }

            result.ActiveCamera = result.Cameras[this.Cameras.IndexOf(this.ActiveCamera)];

            if (this.DebugCamera != null)
            {
                result.DebugCamera = result.Cameras[this.Cameras.IndexOf(this.DebugCamera)];
            }

            result.ShowMeshlets = this.ShowMeshlets;

            return result;
        }
    }
}
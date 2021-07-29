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
        }

        public ItemCollection<Camera> Cameras { get; }
        public ItemCollection<Light> Lights { get; }

        public Camera ActiveCamera { get; set; }
        public Camera? DebugCamera { get; set; }
        public uint ShowMeshlets { get; set; }
        public uint IsOcclusionCullingEnabled { get; set; }

        public void CleanItems()
        {
            this.Cameras.CleanItems();
            this.Lights.CleanItems();
        }

        public void ResetItemsStatus()
        {
            this.Cameras.ResetItemsStatus();
            this.Lights.ResetItemsStatus();
        }
    }
}
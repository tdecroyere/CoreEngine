using CoreEngine.Diagnostics;

namespace CoreEngine.Rendering
{
    public class GraphicsSceneManager : SystemManager
    {
        private readonly GraphicsSceneQueue sceneQueue;

        // Dissociate this?
        public GraphicsScene CurrentScene { get; }

        public GraphicsSceneManager(GraphicsSceneQueue sceneQueue)
        {
            this.sceneQueue = sceneQueue;
            this.CurrentScene = new GraphicsScene();
        }

        public override void PostUpdate(CoreEngineContext context)
        {
            this.CurrentScene.CleanItems();

            UpdateCameraBoundingFrustum();
            UpdateMeshWorldBoundingBox();

            this.sceneQueue.EnqueueScene(this.CurrentScene);
            this.CurrentScene.ResetItemsStatus();
        }

        private void UpdateCameraBoundingFrustum()
        {
            for (var i = 0; i < this.CurrentScene.Cameras.Count; i++)
            {
                var camera = this.CurrentScene.Cameras[i];

                if (camera.IsDirty)
                {
                    camera.BoundingFrustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
                }
            }
        }

        private void UpdateMeshWorldBoundingBox()
        {
            for (var i = 0; i < this.CurrentScene.MeshInstances.Count; i++)
            {
                var meshInstance = this.CurrentScene.MeshInstances[i];

                if (meshInstance.IsDirty)
                {
                    meshInstance.WorldBoundingBox = BoundingBox.CreateTransformed(meshInstance.Mesh.BoundingBox, meshInstance.WorldMatrix);

                    meshInstance.WorldBoundingBoxList.Clear();

                    for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
                    {
                        var geometryInstance = meshInstance.Mesh.GeometryInstances[j];

                        var boundingBox = BoundingBox.CreateTransformed(geometryInstance.BoundingBox, meshInstance.WorldMatrix);
                        meshInstance.WorldBoundingBoxList.Add(boundingBox);
                    }
                }
            }
        }
    }
}
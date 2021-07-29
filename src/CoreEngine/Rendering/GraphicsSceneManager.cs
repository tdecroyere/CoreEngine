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
    }
}
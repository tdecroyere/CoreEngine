using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Graphics.Components;
using CoreEngine.Resources;

namespace CoreEngine.Graphics.EntitySystems
{
    public class UpdateGraphicsSceneSystem : EntitySystem
    {
        private readonly GraphicsSceneRenderer sceneRenderer;

        public UpdateGraphicsSceneSystem(GraphicsSceneRenderer sceneRenderer)
        {
            this.sceneRenderer = sceneRenderer;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Graphics Scene System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(SceneComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }
            
            var sceneArray = this.GetComponentDataArray<SceneComponent>();
           
            sceneRenderer.CurrentScene.ActiveCamera = null;

            for (var i = 0; i < sceneArray.Length; i++)
            {
                var sceneComponent = sceneArray[i];

                if (sceneComponent.ActiveCamera != null)
                {
                    var cameraComponent = entityManager.GetComponentData<CameraComponent>(sceneComponent.ActiveCamera.Value);
                    var camera = sceneRenderer.CurrentScene.Cameras[cameraComponent.Camera];
                    
                    sceneRenderer.CurrentScene.ActiveCamera = camera;
                }

                if (sceneComponent.DebugCamera != null)
                {
                    var cameraComponent = entityManager.GetComponentData<CameraComponent>(sceneComponent.DebugCamera.Value);
                    var camera = sceneRenderer.CurrentScene.Cameras[cameraComponent.Camera];
                    
                    sceneRenderer.CurrentScene.DebugCamera = camera;
                }

                else
                {
                    sceneRenderer.CurrentScene.DebugCamera = null;
                }                    
            }
        }
    }
}
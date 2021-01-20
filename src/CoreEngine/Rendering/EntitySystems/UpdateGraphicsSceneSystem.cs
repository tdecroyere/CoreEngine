using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Rendering.Components;
using CoreEngine.Resources;

namespace CoreEngine.Rendering.EntitySystems
{
    public class UpdateGraphicsSceneSystem : EntitySystem
    {
        private readonly GraphicsSceneManager sceneManager;

        public UpdateGraphicsSceneSystem(GraphicsSceneManager sceneManager)
        {
            this.sceneManager = sceneManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Graphics Scene System");

            definition.Parameters.Add(new EntitySystemParameter<SceneComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }
            
            var sceneArray = this.GetComponentDataArray<SceneComponent>();
           
            sceneManager.CurrentScene.ActiveCamera = null;

            for (var i = 0; i < sceneArray.Length; i++)
            {
                var sceneComponent = sceneArray[i];

                if (sceneComponent.ActiveCamera != null)
                {
                    var cameraComponent = entityManager.GetComponentData<CameraComponent>(sceneComponent.ActiveCamera.Value);
                    var camera = sceneManager.CurrentScene.Cameras[cameraComponent.Camera];
                    
                    sceneManager.CurrentScene.ActiveCamera = camera;
                }

                if (sceneComponent.DebugCamera != null)
                {
                    var cameraComponent = entityManager.GetComponentData<CameraComponent>(sceneComponent.DebugCamera.Value);
                    var camera = sceneManager.CurrentScene.Cameras[cameraComponent.Camera];
                    
                    sceneManager.CurrentScene.DebugCamera = camera;
                }

                else
                {
                    sceneManager.CurrentScene.DebugCamera = null;
                }                    
            }
        }
    }
}
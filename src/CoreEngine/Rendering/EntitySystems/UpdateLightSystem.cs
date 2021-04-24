using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Rendering.Components;

namespace CoreEngine.Rendering.EntitySystems
{
    public class UpdateLightSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;
        private readonly GraphicsSceneManager sceneManager;

        public UpdateLightSystem(GraphicsManager graphicsManager, GraphicsSceneManager sceneManager)
        {
            this.graphicsManager = graphicsManager;
            this.sceneManager = sceneManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Light System");

            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>(isReadOnly: true));
            definition.Parameters.Add(new EntitySystemParameter<LightComponent>(isReadOnly: true));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }
            
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var lightArray = this.GetComponentDataArray<LightComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                ref var transformComponent = ref transformArray[i];
                ref var lightComponent = ref lightArray[i];

                if (!sceneManager.CurrentScene.Lights.Contains(lightComponent.Light))
                {
                    var light = new Light(transformComponent.Position, lightComponent.Color, (LightType)lightComponent.LightType);
                    lightComponent.Light = sceneManager.CurrentScene.Lights.Add(light);
                }

                else
                {
                    var light = sceneManager.CurrentScene.Lights[lightComponent.Light];

                    light.WorldPosition = transformComponent.Position;
                    light.Color = lightComponent.Color;
                    //light.LightType = (LightType)lightComponent.LightType;
                }
            }
        }
    }
}
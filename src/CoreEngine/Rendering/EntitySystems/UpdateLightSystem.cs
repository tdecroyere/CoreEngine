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
        private readonly GraphicsSceneManager sceneManager;

        public UpdateLightSystem(GraphicsSceneManager sceneManager)
        {
            this.sceneManager = sceneManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Update Light System");

            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>(isReadOnly: true));
            definition.Parameters.Add(new EntitySystemParameter<LightComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }

            var memoryChunks = this.GetMemoryChunks();

            for (var i = 0; i < memoryChunks.Length; i++)
            {
                var memoryChunk = memoryChunks.Span[i];

                var transformArray = GetComponentArray<TransformComponent>(memoryChunk);
                var lightArray = GetComponentArray<LightComponent>(memoryChunk); 

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var transformComponent = transformArray[j];
                    ref var lightComponent = ref lightArray[j];

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
}
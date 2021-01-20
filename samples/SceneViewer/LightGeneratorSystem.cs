using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Rendering.Components;
using CoreEngine.Inputs;

namespace CoreEngine.Samples.SceneViewer
{
    public class LightGeneratorSystem : EntitySystem
    {
        private bool isFirstTimeRun = true;

        public LightGeneratorSystem()
        {
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Light Generator System");

            definition.Parameters.Add(new EntitySystemParameter<LightGeneratorComponent>(isReadOnly: true));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }

            if (this.isFirstTimeRun)
            {
                this.isFirstTimeRun = false;

                var entityArray = this.GetEntityArray();
                var lightGeneratorArray = this.GetComponentDataArray<LightGeneratorComponent>();

                for (var i = 0; i < entityArray.Length; i++)
                {
                    ref var lightGeneratorComponent = ref lightGeneratorArray[i];

                    var random = new Random();
                    var componentLayout = entityManager.CreateComponentLayout<LightComponent, TransformComponent, AutomaticMovementComponent>();

                    for (var j = 0; j < lightGeneratorComponent.LightCount; j++)
                    {
                        var offsetX = (float)random.NextDouble() * lightGeneratorComponent.Dimensions.X - lightGeneratorComponent.Dimensions.X * 0.5f;
                        var offsetY = (float)random.NextDouble() * lightGeneratorComponent.Dimensions.Y;
                        var offsetZ = (float)random.NextDouble() * lightGeneratorComponent.Dimensions.Z - lightGeneratorComponent.Dimensions.Z * 0.5f;

                        var speed = (float)random.NextDouble() * 1.3f;

                        var entity = entityManager.CreateEntity(componentLayout);
                        entityManager.SetComponentData(entity, new LightComponent { Color = new Vector3((j % 3 == 0) ? 1: 0, (j % 3 == 1) ? 1: 0, (j % 3 == 2) ? 1: 0) });
                        entityManager.SetComponentData(entity, new TransformComponent{ Position = new Vector3(offsetX, offsetY, offsetZ), Scale = Vector3.One, WorldMatrix = Matrix4x4.Identity });
                        entityManager.SetComponentData(entity, new AutomaticMovementComponent{ Radius = 1.0f, Speed = speed });
                    }
                }
            }
        }
    }
}
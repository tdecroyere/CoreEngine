using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;

namespace CoreEngine.Samples.SceneViewer
{
    public class AutomaticMovementSystem : EntitySystem
    {
        public AutomaticMovementSystem()
        {
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Automatic Movement System");

            definition.Parameters.Add(new EntitySystemParameter<AutomaticMovementComponent>());
            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>());

            return definition;
        }

        // TODO: Remove that hack and have an init system for each entityManagers
        float absoluteTime = 0.0f;

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }

            absoluteTime += deltaTime;

            var memoryChunks = this.GetMemoryChunks();

            Parallel.For(0, memoryChunks.Length, (i) =>
            //for (var i = 0; i < memoryChunks.Length; i++)
            {
                var memoryChunk = memoryChunks.Span[i];
                var random = new Random();

                var transformArray = GetComponentArray<TransformComponent>(memoryChunk);
                var autoMovementArray = GetComponentArray<AutomaticMovementComponent>(memoryChunk); 

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    ref var transformComponent = ref transformArray[j];
                    ref var autoMovementComponent = ref autoMovementArray[j];

                    if (autoMovementComponent.OriginalPosition == Vector3.Zero)
                    {
                        autoMovementComponent.OriginalPosition = transformComponent.Position;
                        autoMovementComponent.RandomValues = new Vector3((float)random.NextDouble() * 2.0f - 1.0f, (float)random.NextDouble() * 2.0f - 1.0f, (float)random.NextDouble() * 2.0f - 1.0f);
                    }

                    var sin = MathF.Sin(absoluteTime * autoMovementComponent.Speed);
                    var halfSin = MathF.Sin(absoluteTime * autoMovementComponent.Speed * 0.5f);
                    var cos = MathF.Cos(absoluteTime * autoMovementComponent.Speed);

                    transformComponent.Position = autoMovementComponent.OriginalPosition + autoMovementComponent.Radius * new Vector3(sin, cos, halfSin) * autoMovementComponent.RandomValues;
                }
            });
        }
    }
}
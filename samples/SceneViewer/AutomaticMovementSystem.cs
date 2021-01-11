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

            definition.Parameters.Add(new EntitySystemParameter(typeof(AutomaticMovementComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));

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

            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var autoMovementArray = this.GetComponentDataArray<AutomaticMovementComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                ref var transformComponent = ref transformArray[i];
                ref var autoMovementComponent = ref autoMovementArray[i];

                if (autoMovementComponent.OriginalPosition == Vector3.Zero)
                {
                    autoMovementComponent.OriginalPosition = transformComponent.Position;
                }

                var sin = MathF.Sin(absoluteTime * autoMovementComponent.Speed);
                var halfSin = MathF.Sin(absoluteTime * autoMovementComponent.Speed * 0.5f);
                var cos = MathF.Cos(absoluteTime * autoMovementComponent.Speed);

                transformComponent.Position = autoMovementComponent.OriginalPosition + autoMovementComponent.Radius * new Vector3(sin, cos, halfSin);
            }
        }
    }
}
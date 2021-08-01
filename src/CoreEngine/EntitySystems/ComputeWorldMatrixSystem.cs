using CoreEngine.Components;

namespace CoreEngine.EntitySystems
{
    public class ComputeWorldMatrixSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Compute World Matrix System");

            // TODO: Add parameters that receive only changed components
            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var memoryChunks = this.GetMemoryChunks();

            // TODO: Do a custom job scheduler?
            Parallel.For(0, memoryChunks.Length, (i) =>
            // for (var i = 0; i < memoryChunks.Length; i++)
            {
                var memoryChunk = memoryChunks.Span[i];

                var transformArray = GetComponentArray<TransformComponent>(memoryChunk);

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    ref var transformComponent = ref transformArray[j];

                    // TODO: This is a hack
                    if (transformComponent.HasChanged == 0)
                    {
                        // TODO: Only update world matrix when transform component has been changed
                        var scale = Matrix4x4.CreateScale(transformComponent.Scale);
                        var rotationX = MathUtils.DegreesToRad(transformComponent.RotationX);
                        var rotationY = MathUtils.DegreesToRad(transformComponent.RotationY);
                        var rotationZ = MathUtils.DegreesToRad(transformComponent.RotationZ);
                        var translation = MathUtils.CreateTranslation(transformComponent.Position);

                        var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);

                        // TODO: Split the transform componenent in 2 to separate the world matrix
                        transformComponent.RotationQuaternion = rotationQuaternion;
                        transformComponent.WorldMatrix = Matrix4x4.Transform(scale, transformComponent.RotationQuaternion) * translation;
                    }
                }
            });
        }
    }
}
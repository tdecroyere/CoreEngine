using System;
using System.Numerics;
using CoreEngine.Components;

namespace CoreEngine.EntitySystems
{
    public class ComputeWorldMatrixSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Compute World Matrix System");

            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                ref var tranformComponent = ref transformArray[i];

                var scale = Matrix4x4.CreateScale(tranformComponent.Scale);
                var rotationX = MathUtils .DegreesToRad(tranformComponent.RotationX);
                var rotationY = MathUtils.DegreesToRad(tranformComponent.RotationY);
                var rotationZ = MathUtils.DegreesToRad(tranformComponent.RotationZ);
                var translation = MathUtils.CreateTranslation(tranformComponent.Position);

                var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);

                tranformComponent.RotationQuaternion = rotationQuaternion;
                tranformComponent.WorldMatrix = Matrix4x4.Transform(scale, tranformComponent.RotationQuaternion) * translation;
            }
        }
    }
}
using System;
using System.Numerics;
using CoreEngine;

namespace CoreEngine
{
    public class ComputeWorldMatrixSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Compute World Matrix System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                ref var tranform = ref transformArray[i];

                var scale = Matrix4x4.CreateScale(tranform.Scale);
                var rotationX = MathUtils .DegreesToRad(tranform.RotationX);
                var rotationY = MathUtils.DegreesToRad(tranform.RotationY);
                var rotationZ = MathUtils.DegreesToRad(tranform.RotationZ);
                var translation = MathUtils.CreateTranslation(tranform.Position);

                var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);

                tranform.RotationQuaternion = rotationQuaternion;
                tranform.WorldMatrix = Matrix4x4.Transform(scale, tranform.RotationQuaternion) * translation;
            }
        }
    }
}
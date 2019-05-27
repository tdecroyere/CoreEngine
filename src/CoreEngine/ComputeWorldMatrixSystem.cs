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
                var scale = Matrix4x4.CreateScale(transformArray[i].Scale);
                var rotationX = MathUtils.DegreesToRad(transformArray[i].RotationX);
                var rotationY = MathUtils.DegreesToRad(transformArray[i].RotationY);
                var rotationZ = MathUtils.DegreesToRad(transformArray[i].RotationZ);
                var translation = Matrix4x4.CreateTranslation(transformArray[i].Position);

                var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(rotationY, rotationX, rotationZ);

                transformArray[i].RotationQuaternion = rotationQuaternion;
                transformArray[i].WorldMatrix = Matrix4x4.Transform(scale, transformArray[i].RotationQuaternion) * translation;
            }
        }
    }
}
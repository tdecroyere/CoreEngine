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
                var rotationX = Matrix4x4.CreateRotationX((transformArray[i].RotationX * MathF.PI) / 180.0f);
                var rotationY = Matrix4x4.CreateRotationY((transformArray[i].RotationY * MathF.PI) / 180.0f);
                var translation = Matrix4x4.CreateTranslation(transformArray[i].Position);

                transformArray[i].WorldMatrix = scale * rotationX * rotationY * translation;
            }
        }
    }
}
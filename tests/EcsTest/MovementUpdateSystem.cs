using System;
using System.Numerics;
using CoreEngine;

namespace CoreEngine.Tests.EcsTest
{
    public class MovementUpdateSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Movement Update System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(PlayerComponent), true));
            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                if (playerArray[i].RotationVector.LengthSquared() > 0.0f)
                {
                    transformArray[i].RotationY += playerArray[i].RotationVector.X * deltaTime * playerArray[i].RotationSpeed;
                    transformArray[i].RotationX += playerArray[i].RotationVector.Y * deltaTime * playerArray[i].RotationSpeed;
                }

                if (playerArray[i].TranslationVector.LengthSquared() > 0.0f)
                {
                    var positionDeltaX = playerArray[i].TranslationVector.X * deltaTime * playerArray[i].MovementSpeed;
                    var positionDeltaZ = playerArray[i].TranslationVector.Y * deltaTime * playerArray[i].MovementSpeed;

                    var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(transformArray[i].RotationY, -transformArray[i].RotationX, 0.0f);
                    transformArray[i].Position += Vector3.Transform(new Vector3(positionDeltaX, 0.0f, positionDeltaZ), -rotationQuaternion);
                }
            }
        }
    }
}
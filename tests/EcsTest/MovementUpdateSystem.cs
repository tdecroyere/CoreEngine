using System;
using System.Numerics;
using CoreEngine.Components;

namespace CoreEngine.Tests.EcsTest
{
    public class MovementUpdateSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Movement Update System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(PlayerComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var friction = -80.5f;
            var deltaTimePow2 = deltaTime * deltaTime;

            var entityArray = this.GetEntityArray();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var player = playerArray[i];

	            var rotationAcceleration = player.RotationVector * player.RotationAcceleration;

                // Add friction to the player
                // TODO: It seems we need to implement ODE equations here
                rotationAcceleration += friction * player.RotationVelocity;

                var movementAcceleration = player.MovementVector * player.MovementAcceleration;

                // Add friction to the player
                // TODO: It seems we need to implement ODE equations here
                movementAcceleration += friction * player.MovementVelocity;

	            var rotationDelta = 0.5f * rotationAcceleration * deltaTimePow2 + player.RotationVelocity * deltaTime;
	            playerArray[i].RotationVelocity = rotationAcceleration * deltaTime + player.RotationVelocity;

                var movementDelta = 0.5f * movementAcceleration * deltaTimePow2 + player.MovementVelocity * deltaTime;
	            playerArray[i].MovementVelocity = movementAcceleration * deltaTime + player.MovementVelocity;

                if (rotationDelta.LengthSquared() > 0)
                {
                    transformArray[i].RotationX += rotationDelta.X;
                    transformArray[i].RotationY += rotationDelta.Y;
                }

                if (movementDelta.LengthSquared() > 0.0f)
                {
                    var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(MathUtils.DegreesToRad(transformArray[i].RotationY), MathUtils.DegreesToRad(transformArray[i].RotationX), 0.0f);
                    transformArray[i].Position += Vector3.Transform(new Vector3(movementDelta.X, 0.0f, movementDelta.Y), rotationQuaternion);
                }
            }
        }
    }
}
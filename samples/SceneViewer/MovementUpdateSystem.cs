using System;
using System.Numerics;
using CoreEngine.Components;

namespace CoreEngine.Samples.SceneViewer
{
    public class MovementUpdateSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Movement Update System");

            definition.Parameters.Add(new EntitySystemParameter<PlayerComponent>());
            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var friction = -75.0f;
            var deltaTimePow2 = deltaTime * deltaTime;

            var memoryChunks = this.GetMemoryChunks();

            for (var i = 0; i < memoryChunks.Length; i++)
            {
                var memoryChunk = memoryChunks.Span[i];

                var transformArray = GetComponentArray<TransformComponent>(memoryChunk);
                var playerArray = GetComponentArray<PlayerComponent>(memoryChunk); 

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var playerComponent = playerArray[j];

                    var rotationAcceleration = playerComponent.RotationVector * playerComponent.RotationAcceleration;

                    // Add friction to the player
                    // TODO: It seems we need to implement ODE equations here
                    rotationAcceleration += friction * playerComponent.RotationVelocity;

                    var movementAcceleration = playerComponent.MovementVector * playerComponent.MovementAcceleration;

                    // Add friction to the player
                    // TODO: It seems we need to implement ODE equations here
                    movementAcceleration += friction * playerComponent.MovementVelocity;

                    var rotationDelta = 0.5f * rotationAcceleration * deltaTimePow2 + playerComponent.RotationVelocity * deltaTime;
                    playerArray[j].RotationVelocity = rotationAcceleration * deltaTime + playerComponent.RotationVelocity;

                    var movementDelta = 0.5f * movementAcceleration * deltaTimePow2 + playerComponent.MovementVelocity * deltaTime;
                    playerArray[j].MovementVelocity = movementAcceleration * deltaTime + playerComponent.MovementVelocity;

                    if (rotationDelta.LengthSquared() > 0)
                    {
                        transformArray[j].RotationX += rotationDelta.X;
                        transformArray[j].RotationY += rotationDelta.Y;
                    }

                    if (movementDelta.LengthSquared() > 0.0f)
                    {
                        var rotationQuaternion = Quaternion.CreateFromYawPitchRoll(MathUtils.DegreesToRad(transformArray[j].RotationY), MathUtils.DegreesToRad(transformArray[j].RotationX), 0.0f);
                        transformArray[j].Position += Vector3.Transform(new Vector3(movementDelta.X, 0.0f, movementDelta.Y), rotationQuaternion);
                    }
                }
            }
        }
    }
}
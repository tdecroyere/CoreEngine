using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Samples.SuperMario.Components;

namespace CoreEngine.Samples.SuperMario.EntitySystems
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

            var entityArray = this.GetEntityArray();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var playerComponent = playerArray[i];
                var movementAcceleration = playerComponent.MovementVector * playerComponent.MovementAcceleration;

                // Add friction to the player
                // TODO: It seems we need to implement ODE equations here
                movementAcceleration += friction * playerComponent.MovementVelocity;

                var movementDelta = 0.5f * movementAcceleration * deltaTimePow2 + playerComponent.MovementVelocity * deltaTime;
	            playerArray[i].MovementVelocity = movementAcceleration * deltaTime + playerComponent.MovementVelocity;

	            transformArray[i].Position += new Vector3(100 * deltaTime, playerComponent.MovementVector.Y, 0.0f);
              
                if (movementDelta.LengthSquared() > 0.0f)
                {
                    // transformArray[i].Position += new Vector3(movementDelta.X, movementDelta.Y, 0.0f);
                }
            }
        }
    }
}
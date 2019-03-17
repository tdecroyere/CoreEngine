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

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var velocity = new Vector3(20.0f, 50.0f, 100.0f);
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                transformArray[i].Position += velocity * deltaTime;
            }
        }
    }
}
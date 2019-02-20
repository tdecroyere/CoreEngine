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
            Console.WriteLine("Begin Movement Update System");

            var velocity = new Vector3(20.0f, 50.0f, 100.0f);
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();

            for (var i = 0; i < transformArray.Length; i++)
            {
                Console.WriteLine($"Processing entity: {entityArray[i]}");
                transformArray[i].Position += velocity * deltaTime;
            }

            Console.WriteLine("End Movement Update System");
        }
    }
}
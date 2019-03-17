using System;
using System.Numerics;
using CoreEngine;

namespace CoreEngine.Tests.EcsTest
{
    public class BlockUpdateSystem : EntitySystem
    {
        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Block Update System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(BlockComponent), true));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var blockArray = this.GetComponentDataArray<BlockComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                if (blockArray[i].IsWall == 1)
                {
                    transformArray[i].Position.Y += 0.25f * deltaTime;
                }

                else if (blockArray[i].IsWater == 1)
                {
                    transformArray[i].Position.Z += 0.25f * deltaTime;
                }
            }
        }
    }
}
using System;
using BenchmarkDotNet.Attributes;

namespace CoreEngine.Tests.EcsTest
{
    public class Benchmark
    {
        private EcsTestApp testApp;

        public Benchmark()
        {
            this.testApp = new EcsTestApp();
            this.testApp.Init();
        }

        [Benchmark]
        public void TestEntityCreation()
        {
            var entityManager = new EntityManager();
            var blockLayout = entityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(BlockComponent));
  
            for (int i = 0; i < 10000; i++)
            {
                var wallEntity = entityManager.CreateEntity(blockLayout);

                TransformComponent wallPositionComponent;
                wallPositionComponent.Position.X = (float)i;
                wallPositionComponent.Position.Y = (float)i + 54.0f;
                wallPositionComponent.Position.Z = (float)i + 22.0f;
                entityManager.SetComponentData(wallEntity, wallPositionComponent);

                BlockComponent wallBlockComponent;
                wallBlockComponent.IsWall = (i % 2);
                wallBlockComponent.IsWater = ((i + 1) % 2);
                entityManager.SetComponentData(wallEntity, wallBlockComponent);
            }
        }

        [Benchmark]
        public void TestSystemUpdates()
        {
            for (var i = 0; i < 10000; i++)
            {
                this.testApp.Update(i);
            }
        }
    }
}
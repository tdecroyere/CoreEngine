using System;
using CoreEngine;

namespace CoreEngine.Tests.EcsTest
{
    public class EcsTestApp : CoreEngineApp
    {
        public override string Name => "EcsTest App";

        public override void Init()
        {
            Console.WriteLine("Init Ecs Test App...");

            // Test EntityManager basic functions
            var entityManager = new EntityManager();
            var playerLayout = entityManager.CreateEntityComponentLayout(typeof(TransformComponent));
            var wallLayout = entityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(BlockComponent));

            var playerEntity = entityManager.CreateEntity(playerLayout);

            TransformComponent playerPositionComponent;
            playerPositionComponent.Position.X = 12.0f;
            playerPositionComponent.Position.Y = 20.0f;
            playerPositionComponent.Position.Z = 45.0f;
            entityManager.SetComponentData(playerEntity, playerPositionComponent);

            for (int i = 0; i < 10; i++)
            {
                var wallEntity = entityManager.CreateEntity(wallLayout);

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

            DisplayEntities(entityManager);

            var entitySystemManager = new EntitySystemManager(entityManager);
            entitySystemManager.RegisterSystem(new MovementUpdateSystem());
            entitySystemManager.RegisterSystem(new BlockUpdateSystem());

            entitySystemManager.Process(2);
            entitySystemManager.Process(1);

            DisplayEntities(entityManager);
        }

        private static void DisplayEntities(EntityManager entityManager)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Display entities");
            Console.WriteLine("----------------------------------------");

            var entities = entityManager.GetEntities();

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                Console.WriteLine($"Entity: {entity.EntityId}");

                var position = entityManager.GetComponentData<TransformComponent>(entity);
                Console.WriteLine($"Position (X: {position.Position.X}, Y: {position.Position.Y}, Z: {position.Position.Z})");

                if (entityManager.HasComponent<BlockComponent>(entity))
                {
                    var blockComponent = entityManager.GetComponentData<BlockComponent>(entity);
                    Console.WriteLine($"Block (IsWall: {blockComponent.IsWall}, IsWater: {blockComponent.IsWater})");
                }

                Console.WriteLine("----------------------------------------");
            }
        }

        public override void Update()
        {

        }
    }
}
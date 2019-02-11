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
            var playerLayout = entityManager.CreateEntityComponentLayout(typeof(PositionComponent));
            var wallLayout = entityManager.CreateEntityComponentLayout(typeof(PositionComponent), typeof(BlockComponent));

            var playerEntity = entityManager.CreateEntity(playerLayout);

            PositionComponent playerPositionComponent;
            playerPositionComponent.Position.X = 12.0f;
            playerPositionComponent.Position.Y = 20.0f;
            playerPositionComponent.Position.Z = 45.0f;
            entityManager.SetComponentData(playerEntity, playerPositionComponent);

            for (int i = 0; i < 10; i++)
            {
                var wallEntity = entityManager.CreateEntity(wallLayout);

                PositionComponent wallPositionComponent;
                wallPositionComponent.Position.X = (float)i;
                wallPositionComponent.Position.Y = (float)i + 54.0f;
                wallPositionComponent.Position.Z = (float)i + 22.0f;
                entityManager.SetComponentData(wallEntity, wallPositionComponent);

                BlockComponent wallBlockComponent;
                wallBlockComponent.IsWall = true;
                wallBlockComponent.IsWater = false;
                entityManager.SetComponentData(wallEntity, wallBlockComponent);
            }

            var entities = entityManager.GetEntities();

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                Console.WriteLine($"Entity: {entity.EntityId}");
                
                var position = entityManager.GetComponentData<PositionComponent>(entity);
                Console.WriteLine($"Position (X: {position.Position.X}, Y: {position.Position.Y}, Z: {position.Position.Z})");
            
                if (entityManager.HasComponent<BlockComponent>(entity))
                {
                    var blockComponent = entityManager.GetComponentData<BlockComponent>(entity);
                    Console.WriteLine($"Block (IsWall: {blockComponent.IsWall}, IsWater: {blockComponent.IsWater})");
                }

                Console.WriteLine("----------------------------------------");
            }

            // Test Entity Layout compatibility

            // PositionUpdateSystem positionUpdateSystem = {};
            // positionUpdateSystem.OnUpdate(nullptr);
        }

        public override void Update()
        {

        }
    }
}
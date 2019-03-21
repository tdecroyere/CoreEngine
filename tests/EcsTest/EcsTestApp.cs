using System;
using System.Numerics;
using CoreEngine;

namespace CoreEngine.Tests.EcsTest
{
    public class EcsTestApp : CoreEngineApp
    {
        private EntityManager? entityManager;
        private EntitySystemManager? entitySystemManager;

        public override string Name => "EcsTest App";

        public override void Init()
        {
            Console.WriteLine("Init Ecs Test App...");

            // Test EntityManager basic functions
            this.entityManager = new EntityManager();
            var playerLayout = this.entityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(DebugTriangleComponent));
            var blockLayout = this.entityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(BlockComponent));

            var playerEntity = this.entityManager.CreateEntity(playerLayout);

            // TODO: Find a way to have default values for components
            TransformComponent playerPositionComponent = new TransformComponent();
            playerPositionComponent.Position.X = 12.0f;
            playerPositionComponent.Position.Y = 20.0f;
            playerPositionComponent.Position.Z = 45.0f;
            playerPositionComponent.WorldMatrix = Matrix4x4.Identity;
            this.entityManager.SetComponentData(playerEntity, playerPositionComponent);

            DebugTriangleComponent playerDebugTriangleComponent;
            playerDebugTriangleComponent.Color1 = new Vector4(1, 0, 0, 1);
            playerDebugTriangleComponent.Color2 = new Vector4(0, 1, 0, 1);
            playerDebugTriangleComponent.Color3 = new Vector4(0, 0, 1, 1);
            this.entityManager.SetComponentData(playerEntity, playerDebugTriangleComponent);

            for (int i = 0; i < 10; i++)
            {
                var wallEntity = entityManager.CreateEntity(blockLayout);

                TransformComponent wallPositionComponent = new TransformComponent();
                wallPositionComponent.Position.X = (float)i;
                wallPositionComponent.Position.Y = (float)i + 54.0f;
                wallPositionComponent.Position.Z = (float)i + 22.0f;
                wallPositionComponent.WorldMatrix = Matrix4x4.Identity; 
                this.entityManager.SetComponentData(wallEntity, wallPositionComponent);

                BlockComponent wallBlockComponent;
                wallBlockComponent.IsWall = (i % 2);
                wallBlockComponent.IsWater = ((i + 1) % 2);
                this.entityManager.SetComponentData(wallEntity, wallBlockComponent);
            }

            DisplayEntities(this.entityManager);

            this.entitySystemManager = new EntitySystemManager(entityManager);
            this.entitySystemManager.RegisterSystem<MovementUpdateSystem>();
            this.entitySystemManager.RegisterSystem<BlockUpdateSystem>();
            this.entitySystemManager.RegisterSystem<DebugTriangleSystem>();
        }

        public override void Update(float deltaTime)
        {
            if (this.entitySystemManager != null && this.entityManager != null)
            {
                this.entitySystemManager.Process(deltaTime);
                //DisplayEntities(this.entityManager);
            }
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
    }
}
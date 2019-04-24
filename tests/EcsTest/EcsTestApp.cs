using System;
using System.Numerics;
using System.IO;
using CoreEngine;
using CoreEngine.Resources;

namespace CoreEngine.Tests.EcsTest
{
    public class EcsTestApp : CoreEngineApp
    {
        private EntityManager? entityManager;
        private EntitySystemManager? entitySystemManager;
        private TestResource? testResource;

        public override string Name => "EcsTest App";

        public override void Init()
        {
            Console.WriteLine("Init Ecs Test App...");

            var resourceManager = this.SystemManagerContainer.GetSystemManager<ResourcesManager>();
            resourceManager.AddResourceStorage(new FileSystemResourceStorage("../Resources"));
            resourceManager.AddResourceLoader(new TestResourceLoader());

            this.testResource = resourceManager.LoadResourceAsync<TestResource>("/Test.tst");
            this.testResource = resourceManager.LoadResourceAsync<TestResource>("/Test.tst");

            // Test EntityManager basic functions
            this.entityManager = new EntityManager();
            var playerLayout = this.entityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(PlayerComponent), typeof(DebugTriangleComponent));
            var blockLayout = this.entityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(BlockComponent));

            var playerEntity = this.entityManager.CreateEntity(playerLayout);

            // TODO: Find a way to have default values for components
            var playerPositionComponent = new TransformComponent();
            playerPositionComponent.Position.X = 12.0f;
            playerPositionComponent.Position.Y = 20.0f;
            playerPositionComponent.Position.Z = 45.0f;
            playerPositionComponent.WorldMatrix = Matrix4x4.Identity;
            this.entityManager.SetComponentData(playerEntity, playerPositionComponent);

            var playerComponent = new PlayerComponent();
            playerComponent.InputVector = Vector3.Zero;
            playerComponent.ChangeColorAction = 0;
            this.entityManager.SetComponentData(playerEntity, playerComponent);

            var playerDebugTriangleComponent = new DebugTriangleComponent();
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

            //DisplayEntities(this.entityManager);

            this.entitySystemManager = new EntitySystemManager(entityManager, this.SystemManagerContainer);
            this.entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<BlockUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<DebugTriangleSystem>();
        }

        public override void Update(float deltaTime)
        {
            if (this.entitySystemManager != null && this.entityManager != null)
            {
                this.entitySystemManager.Process(deltaTime);

                if (this.testResource != null)
                {
                    Console.WriteLine($"Test Resource: {this.testResource.Text}");
                }

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
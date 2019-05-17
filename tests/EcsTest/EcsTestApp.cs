using System;
using System.Numerics;
using System.IO;
using CoreEngine;
using CoreEngine.Resources;
using CoreEngine.Graphics;
using CoreEngine.Diagnostics;

namespace CoreEngine.Tests.EcsTest
{
    public class EcsTestApp : CoreEngineApp
    {
        private EntityManager? entityManager;
        private EntitySystemManager? entitySystemManager;
        private Shader? testResource;
        private Mesh? testMesh;

        public override string Name => "EcsTest App";

        public override void Init()
        {
            Logger.WriteMessage("Init Ecs Test App...");

            var resourceManager = this.SystemManagerContainer.GetSystemManager<ResourcesManager>();
            resourceManager.AddResourceStorage(new FileSystemResourceStorage("/Users/tdecroyere/Projects/CoreEngine/build/MacOS/CoreEngine.app/Contents/Resources"));
            resourceManager.AddResourceLoader(new TestResourceLoader());

            this.testResource = resourceManager.LoadResourceAsync<Shader>("/TestShader.shader");

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

                //DisplayEntities(this.entityManager);
            }
        }

        private static void DisplayEntities(EntityManager entityManager)
        {
            Logger.WriteMessage("----------------------------------------");
            Logger.WriteMessage("Display entities");
            Logger.WriteMessage("----------------------------------------");

            var entities = entityManager.GetEntities();

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                Logger.WriteMessage($"Entity: {entity.EntityId}");

                var position = entityManager.GetComponentData<TransformComponent>(entity);
                Logger.WriteMessage($"Position (X: {position.Position.X}, Y: {position.Position.Y}, Z: {position.Position.Z})");

                if (entityManager.HasComponent<BlockComponent>(entity))
                {
                    var blockComponent = entityManager.GetComponentData<BlockComponent>(entity);
                    Logger.WriteMessage($"Block (IsWall: {blockComponent.IsWall}, IsWater: {blockComponent.IsWater})");
                }

                Logger.WriteMessage("----------------------------------------");
            }
        }
    }
}
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
        private Scene? currentScene;
        private EntitySystemManager? entitySystemManager;
        private Shader? testShader;

        public override string Name => "EcsTest App";

        public override void Init()
        {
            Logger.WriteMessage("Init Ecs Test App...");

            var resourcesManager = this.SystemManagerContainer.GetSystemManager<ResourcesManager>();
            resourcesManager.AddResourceStorage(new FileSystemResourceStorage("/Users/tdecroyere/Projects/CoreEngine/build/MacOS/CoreEngine.app/Contents/Resources"));
            resourcesManager.AddResourceStorage(new FileSystemResourceStorage(@"C:\Projects\perso\CoreEngine\build\MacOS\CoreEngine.app\Contents\Resources"));

            this.testShader = resourcesManager.LoadResourceAsync<Shader>("/TestShader.shader");
            
            var testLoadedScene = resourcesManager.LoadResourceAsync<Scene>("/TestScene.scene");

            //var testMesh = resourcesManager.LoadResourceAsync<Mesh>("/teapot.mesh");
            //var sponzaMesh = resourcesManager.LoadResourceAsync<Mesh>("/sponza.mesh");

            // Test EntityManager basic functions
            //var testScene = new Scene();
            // var playerLayout = testScene.EntityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(PlayerComponent), typeof(MeshComponent));
            // var blockLayout = testScene.EntityManager.CreateEntityComponentLayout(typeof(TransformComponent), typeof(BlockComponent), typeof(MeshComponent));

            // var playerEntity = testScene.EntityManager.CreateEntity(playerLayout);

            // // TODO: Find a way to have default values for components
            // var playerPositionComponent = new TransformComponent();
            // playerPositionComponent.Position = new Vector3(0, -15.0f, 0);
            // playerPositionComponent.Scale = new Vector3(0.05f, 0.05f, 0.05f);
            // playerPositionComponent.WorldMatrix = Matrix4x4.Identity;
            // testScene.EntityManager.SetComponentData(playerEntity, playerPositionComponent);

            // var playerComponent = new PlayerComponent();
            // playerComponent.InputVector = Vector3.Zero;
            // playerComponent.ChangeColorAction = 0;
            // testScene.EntityManager.SetComponentData(playerEntity, playerComponent);

            // var playerDebugTriangleComponent = new MeshComponent();
            // playerDebugTriangleComponent.MeshResourceId = sponzaMesh.ResourceId;
            // testScene.EntityManager.SetComponentData(playerEntity, playerDebugTriangleComponent);

            // for (int i = 0; i < 10; i++)
            // {
            //     var wallEntity = testScene.EntityManager.CreateEntity(blockLayout);

            //     var wallPositionComponent = new TransformComponent();
            //     wallPositionComponent.Position.X = -140.0f + i * 30.0f;
            //     wallPositionComponent.Position.Y = 0.0f;
            //     wallPositionComponent.Position.Z = 200.0f;
            //     wallPositionComponent.Scale = new Vector3(1.0f, 1.0f, 1.0f);
            //     wallPositionComponent.WorldMatrix = Matrix4x4.Identity; 
            //     testScene.EntityManager.SetComponentData(wallEntity, wallPositionComponent);

            //     var wallBlockComponent = new BlockComponent();
            //     wallBlockComponent.IsWall = (i % 2);
            //     wallBlockComponent.IsWater = ((i + 1) % 2);
            //     testScene.EntityManager.SetComponentData(wallEntity, wallBlockComponent);

            //     var wallMesh = new MeshComponent();
            //     wallMesh.MeshResourceId = testMesh.ResourceId;
            //     testScene.EntityManager.SetComponentData(wallEntity, wallMesh);
            // }

            this.currentScene = testLoadedScene;

            this.entitySystemManager = new EntitySystemManager(this.SystemManagerContainer);
            this.entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<BlockUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<ComputeWorldMatrixSystem>();
            this.entitySystemManager.RegisterEntitySystem<RenderMeshSystem>();
        }

        public override void Update(float deltaTime)
        {
            if (this.entitySystemManager != null && this.currentScene != null)
            {
                this.entitySystemManager.Process(this.currentScene.EntityManager, deltaTime);
            }
        }
    }
}
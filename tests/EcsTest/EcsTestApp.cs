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

            this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/TestScene.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Moana/island.scene");
            this.testShader = resourcesManager.LoadResourceAsync<Shader>("/TestShader.shader");

            this.entitySystemManager = new EntitySystemManager(this.SystemManagerContainer);
            this.entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<ComputeWorldMatrixSystem>();
            this.entitySystemManager.RegisterEntitySystem<UpdateActiveCameraSystem>();
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
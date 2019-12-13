using System;
using System.Numerics;
using System.IO;
using CoreEngine.EntitySystems;
using CoreEngine.Resources;
using CoreEngine.Graphics;
using CoreEngine.Graphics.EntitySystems;
using CoreEngine.Diagnostics;

namespace CoreEngine.Tests.EcsTest
{
    public class EcsTestApp : CoreEngineApp
    {
        private Scene? currentScene;
        private EntitySystemManager? entitySystemManager;
        private Texture? testTexture;
        private Texture? testTexture2;
        private Graphics2DRenderer? graphics2DRenderer;

        public override string Name => "EcsTest App";

        public override void Init()
        {
            var resourcesManager = this.SystemManagerContainer.GetSystemManager<ResourcesManager>();
            this.graphics2DRenderer = this.SystemManagerContainer.GetSystemManager<Graphics2DRenderer>();
            // resourcesManager.AddResourceStorage(new FileSystemResourceStorage("/Users/tdecroyere/Projects/CoreEngine/build/MacOS/CoreEngine.app/Contents/Resources"));
            // resourcesManager.AddResourceStorage(new FileSystemResourceStorage(@"C:\Projects\perso\CoreEngine\build\Windows\Resources"));

            this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/TestScene.scene");
            this.testTexture = resourcesManager.LoadResourceAsync<Texture>("/pokemon.texture");
            this.testTexture2 = resourcesManager.LoadResourceAsync<Texture>("/pokemon2.texture");
            // this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Moana/island.scene");

            this.entitySystemManager = new EntitySystemManager(this.SystemManagerContainer);
            this.entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<ManageActiveCameraSystem>();
            this.entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<ComputeWorldMatrixSystem>();
            this.entitySystemManager.RegisterEntitySystem<UpdateCameraSystem>();
            this.entitySystemManager.RegisterEntitySystem<UpdateGraphicsSceneSystem>();
            this.entitySystemManager.RegisterEntitySystem<RenderMeshSystem>();
        }

        public override void Update(float deltaTime)
        {
            if (this.entitySystemManager != null && this.currentScene != null)
            {
                this.entitySystemManager.Process(this.currentScene.EntityManager, deltaTime);
            }

            if (this.graphics2DRenderer != null)
            {
                if (this.testTexture != null)
                {
                    this.graphics2DRenderer.DrawRectangleTexture(new Vector2(0, 0), testTexture);
                }

                if (this.testTexture2 != null)
                {
                    this.graphics2DRenderer.DrawRectangleTexture(new Vector2(2000, 100), testTexture2);
                    this.graphics2DRenderer.DrawRectangleTexture(new Vector2(1000, 1000), testTexture2);
                }
            }
        }
    }
}
using CoreEngine.EntitySystems;
using CoreEngine.Resources;
using CoreEngine.Rendering.EntitySystems;

namespace CoreEngine.Tests.EcsTest
{
    public class EcsTestApp : CoreEngineApp
    {
        private Scene currentScene;
        private EntitySystemManager entitySystemManager;

        public override string Name => "EcsTest App";

        public EcsTestApp(SystemManagerContainer systemManagerContainer) : base(systemManagerContainer)
        {
            var resourcesManager = this.SystemManagerContainer.GetSystemManager<ResourcesManager>();
            // resourcesManager.AddResourceStorage(new FileSystemResourceStorage("/Users/tdecroyere/Projects/CoreEngine/build/MacOS/CoreEngine.app/Contents/Resources"));
            // resourcesManager.AddResourceStorage(new FileSystemResourceStorage(@"C:\Projects\perso\CoreEngine\build\Windows\Resources"));

            this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/TestScene.scene");
            // this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Bistro/Bistro.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/BistroV4/Bistro.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Moana/island.scene");

            this.entitySystemManager = new EntitySystemManager(this.SystemManagerContainer);
            this.entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<ManageActiveCameraSystem>();
            this.entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
            this.entitySystemManager.RegisterEntitySystem<LightGeneratorSystem>();
            this.entitySystemManager.RegisterEntitySystem<AutomaticMovementSystem>();
            this.entitySystemManager.RegisterEntitySystem<ComputeWorldMatrixSystem>();
            this.entitySystemManager.RegisterEntitySystem<UpdateCameraSystem>();
            this.entitySystemManager.RegisterEntitySystem<UpdateLightSystem>();
            this.entitySystemManager.RegisterEntitySystem<UpdateGraphicsSceneSystem>();
            this.entitySystemManager.RegisterEntitySystem<RenderMeshSystem>();
        }

        public override void Update(float deltaTime)
        {
            this.entitySystemManager.Process(this.currentScene.EntityManager, deltaTime);
        }
    }
}
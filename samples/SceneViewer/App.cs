using CoreEngine.EntitySystems;
using CoreEngine.Resources;
using CoreEngine.Rendering.EntitySystems;

namespace CoreEngine.Samples.SceneViewer
{
    public class App : CoreEngineApp
    {
        private Scene currentScene;
        private EntitySystemManager entitySystemManager;

        public override string Name => "Scene Viewer";

        public App(SystemManagerContainer systemManagerContainer) : base(systemManagerContainer)
        {
            var resourcesManager = this.SystemManagerContainer.GetSystemManager<ResourcesManager>();

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
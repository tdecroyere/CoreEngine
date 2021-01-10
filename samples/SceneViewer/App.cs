using CoreEngine.EntitySystems;
using CoreEngine.Resources;
using CoreEngine.Rendering.EntitySystems;

namespace CoreEngine.Samples.SceneViewer
{
    public class App : CoreEngineApp
    {
        private EntitySystemManager entitySystemManager;

        public override string Name => "Scene Viewer";

        public override void OnInit(CoreEngineContext context)
        {
            var resourcesManager = context.SystemManagerContainer.GetSystemManager<ResourcesManager>();

            context.CurrentScene = resourcesManager.LoadResourceAsync<Scene>("/TestScene.scene");
            // this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Bistro/Bistro.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/BistroV4/Bistro.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Moana/island.scene");

            this.entitySystemManager = new EntitySystemManager(context.SystemManagerContainer);
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

        public override void OnUpdate(CoreEngineContext context, float deltaTime)
        {
            this.entitySystemManager.Process(context.CurrentScene.EntityManager, deltaTime);
        }
    }
}
using CoreEngine.EntitySystems;
using CoreEngine.Resources;
using CoreEngine.Rendering.EntitySystems;

namespace CoreEngine.Samples.SceneViewer
{
    public class App : CoreEngineApp
    {
        public override string Name => "Scene Viewer";

        public override void OnInit(CoreEngineContext context)
        {
            var resourcesManager = context.SystemManagerContainer.GetSystemManager<ResourcesManager>();

            context.CurrentScene = resourcesManager.LoadResourceAsync<Scene>("/Data/TestScene.scene");
            // this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Bistro/Bistro.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/BistroV4/Bistro.scene");
            //this.currentScene = resourcesManager.LoadResourceAsync<Scene>("/Moana/island.scene");

            if (context.CurrentScene != null)
            {
                var entitySystemManager = context.CurrentScene.EntitySystemManager;

                entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
                entitySystemManager.RegisterEntitySystem<ManageActiveCameraSystem>();
                entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
                entitySystemManager.RegisterEntitySystem<LightGeneratorSystem>();
                entitySystemManager.RegisterEntitySystem<AutomaticMovementSystem>();
                entitySystemManager.RegisterEntitySystem<ComputeWorldMatrixSystem>();
                entitySystemManager.RegisterEntitySystem<UpdateCameraSystem>();
                entitySystemManager.RegisterEntitySystem<UpdateLightSystem>();
                entitySystemManager.RegisterEntitySystem<UpdateGraphicsSceneSystem>();
                entitySystemManager.RegisterEntitySystem<RenderMeshSystem>();
            }
        }

        public override void OnUpdate(CoreEngineContext context, float deltaTime)
        {
            if (context.CurrentScene != null)
            {
                context.CurrentScene.EntitySystemManager.Process(context.SystemManagerContainer, context.CurrentScene.EntityManager, deltaTime);
            }
        }
    }
}
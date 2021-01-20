using CoreEngine.Components;
using CoreEngine.Diagnostics;

namespace CoreEngine.Samples.SuperMario
{
    public class App : CoreEngineApp
    {
        public override string Name => "Super Mario";

        public override void OnInit(CoreEngineContext context)
        {
            Logger.WriteMessage("Starting Super Mario...");

            context.CurrentScene = new Scene();
            var entityManager = context.CurrentScene.EntityManager;

            var componentLayout = entityManager.CreateComponentLayout<TransformComponent, BlockComponent>();
            var entity = entityManager.CreateEntity(componentLayout);

            Logger.WriteMessage($"{componentLayout}");
            entityManager.SetComponentData<TransformComponent>(entity, new TransformComponent() { Position = new System.Numerics.Vector3() });
        }

        public override void OnUpdate(CoreEngineContext context, float deltaTime)
        {
            Logger.WriteMessage("Update Super Mario...");
        }
    }
}
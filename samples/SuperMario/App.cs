using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;
using CoreEngine.Samples.SuperMario.Components;
using CoreEngine.Samples.SuperMario.EntitySystems;

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

            var playerComponentLayout = entityManager.CreateComponentLayout<TransformComponent, PlayerComponent, SpriteComponent>();
            var entity = entityManager.CreateEntity(playerComponentLayout);

            entityManager.SetComponentData(entity, new TransformComponent() { Position = new Vector3(800, 400, 0) });
            entityManager.SetComponentData(entity, new SpriteComponent() {  });

            if (context.CurrentScene != null)
            {
                var entitySystemManager = context.CurrentScene.EntitySystemManager;

                entitySystemManager.RegisterEntitySystem<InputsUpdateSystem>();
                entitySystemManager.RegisterEntitySystem<MovementUpdateSystem>();
                entitySystemManager.RegisterEntitySystem<RenderSpriteSystem>();
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
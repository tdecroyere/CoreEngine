using CoreEngine.Diagnostics;

namespace CoreEngine.Samples.SuperMario
{
    public class App : CoreEngineApp
    {
        public override string Name => "Super Mario";

        public override void OnInit(CoreEngineContext context)
        {
            Logger.WriteMessage("Starting Super Mario...");
        }

        public override void OnUpdate(CoreEngineContext context, float deltaTime)
        {
            Logger.WriteMessage("Update Super Mario...");
        }
    }
}
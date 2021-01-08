using CoreEngine.Diagnostics;

namespace CoreEngine.Samples.SuperMario
{
    public class App : CoreEngineApp
    {
        public override string Name => "Super Mario";

        public App(SystemManagerContainer systemManagerContainer) : base(systemManagerContainer)
        {
            Logger.WriteMessage("Starting Super Mario...");
        }

        public override void Update(float deltaTime)
        {
            Logger.WriteMessage("Update Super Mario...");
        }
    }
}
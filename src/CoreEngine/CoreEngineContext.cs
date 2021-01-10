namespace CoreEngine
{
    public class CoreEngineContext
    {
        public CoreEngineContext(SystemManagerContainer systemManagerContainer)
        {
            this.SystemManagerContainer = systemManagerContainer;
        }

        public Scene? CurrentScene { get; set; }
        public SystemManagerContainer SystemManagerContainer { get; }
    }
}
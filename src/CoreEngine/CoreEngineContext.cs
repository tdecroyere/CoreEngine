namespace CoreEngine
{
    public class CoreEngineContext
    {
        public CoreEngineContext(SystemManagerContainer systemManagerContainer)
        {
            this.SystemManagerContainer = systemManagerContainer;
        }

        public SystemManagerContainer SystemManagerContainer
        {
            get;
        }
    }
}
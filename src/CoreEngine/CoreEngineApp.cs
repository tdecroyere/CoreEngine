using System;

namespace CoreEngine
{
    public abstract class CoreEngineApp
    {
        protected CoreEngineApp(SystemManagerContainer systemManagerContainer)
        {
            this.SystemManagerContainer = systemManagerContainer;
        }

        public abstract string Name
        {
            get;
        }

        public SystemManagerContainer SystemManagerContainer
        {
            get;
        }

        public abstract void Update(float deltaTime);
    }
}
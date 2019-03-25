using System;

namespace CoreEngine
{
    public abstract class CoreEngineApp
    {
        protected CoreEngineApp()
        {
            this.SystemManagerContainer = new SystemManagerContainer(this);
        }

        public abstract string Name
        {
            get;
        }

        public SystemManagerContainer SystemManagerContainer
        {
            get;
        }

        public abstract void Init();
        public abstract void Update(float deltaTime);
    }
}
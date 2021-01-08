using System;

namespace CoreEngine
{
    public abstract class CoreEngineApp
    {
        public abstract string Name
        {
            get;
        }

        public virtual void OnInit(CoreEngineContext context)
        {

        }

        public abstract void OnUpdate(CoreEngineContext context, float deltaTime);
    }
}
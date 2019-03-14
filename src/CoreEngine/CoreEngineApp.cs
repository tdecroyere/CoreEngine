using System;

namespace CoreEngine
{
    public abstract class CoreEngineApp
    {
        public abstract string Name
        {
            get;
        }

        public abstract void Init();
        public abstract void Update(float deltaTime);
    }
}
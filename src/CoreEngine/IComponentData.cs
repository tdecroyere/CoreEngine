using System;

namespace CoreEngine
{
    public interface IComponentData
    {
        void SetDefaultValues()
        {
            
        }

        ComponentHash GetComponentHash() => throw new NotImplementedException();
    }
}
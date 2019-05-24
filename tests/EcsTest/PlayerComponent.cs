using System;
using System.Numerics;

namespace CoreEngine.Tests.EcsTest
{
    public struct PlayerComponent : IComponentData
    {
        public Vector3 InputVector;

        // TODO: Bool is not working with MemoryMarshal.Cast method (alignment problem)
        public int ChangeColorAction;

        public void SetDefaultValues()
        {
            
        }
    }
}
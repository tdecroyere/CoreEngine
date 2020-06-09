using System;
using System.Numerics;

namespace CoreEngine.Tests.EcsTest
{
    public struct LightGeneratorComponent : IComponentData
    {
        public Vector3 Dimensions { get; set; }
        public float LightCount { get; set; }

        public void SetDefaultValues()
        {
            this.LightCount = 10;
        }
    }
}
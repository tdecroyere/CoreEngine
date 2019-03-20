using System;
using System.Numerics;

namespace CoreEngine.Tests.EcsTest
{
    public struct DebugTriangleComponent : IComponentData
    {
        public Vector4 Color1;
        public Vector4 Color2;
        public Vector4 Color3;
    }
}
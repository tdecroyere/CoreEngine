using System;
using System.Numerics;

namespace CoreEngine.Tests.EcsTest
{
    public struct MeshComponent : IComponentData
    {
        public uint MeshResourceId;
    }
}
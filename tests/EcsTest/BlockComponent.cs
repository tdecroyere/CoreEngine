using System;

namespace CoreEngine.Tests.EcsTest
{
    public struct BlockComponent : IComponentData
    {
        // TODO: Bool is not working with MemoryMarshal.Cast method (alignment problem)
        public int IsWall;
        public int IsWater;
    }
}
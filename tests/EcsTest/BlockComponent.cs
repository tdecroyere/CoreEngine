using System;
using System.Runtime.InteropServices;

namespace CoreEngine.Tests.EcsTest
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockComponent : IComponentData
    {
        public bool IsWall;
        public bool IsWater;
    }
}
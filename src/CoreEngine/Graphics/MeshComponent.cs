using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public struct MeshComponent : IComponentData
    {
        public uint MeshResourceId;

        public void SetDefaultValues()
        {
            
        }
    }
}
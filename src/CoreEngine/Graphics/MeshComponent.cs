using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public struct MeshComponent : IComponentData
    {
        public uint MeshResourceId { get; set; }

        public void SetDefaultValues()
        {
            
        }
    }
}
using System;
using System.Numerics;

namespace CoreEngine.Graphics.Components
{
    public struct MeshComponent : IComponentData
    {
        public uint MeshResourceId { get; set; }

        public void SetDefaultValues()
        {
            
        }
    }
}
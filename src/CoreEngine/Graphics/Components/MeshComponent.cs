using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Graphics.Components
{
    public struct MeshComponent : IComponentData
    {
        public uint MeshResourceId { get; set; }
        public ItemIdentifier MeshInstance { get; set; }

        public void SetDefaultValues()
        {
            
        }
    }
}
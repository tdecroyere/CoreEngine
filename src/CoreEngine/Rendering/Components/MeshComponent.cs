using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering.Components
{
    public partial struct MeshComponent : IComponentData
    {
        public uint MeshResourceId { get; set; }
        public ItemIdentifier MeshInstance { get; set; }

        public void SetDefaultValues()
        {
            
        }
    }
}
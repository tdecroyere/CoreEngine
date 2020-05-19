using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering.Components
{
    public struct MaterialComponent : IComponentData
    {
        public uint MaterialResourceId { get; set; }

        public void SetDefaultValues()
        {
            
        }
    }
}
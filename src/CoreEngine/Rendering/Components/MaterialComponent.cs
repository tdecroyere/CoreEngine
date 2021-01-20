using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering.Components
{
    public partial struct MaterialComponent : IComponentData
    {
        public uint MaterialResourceId { get; set; }

        public void SetDefaultValues()
        {
            
        }
    }
}
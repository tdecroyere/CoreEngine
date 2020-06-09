using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering.Components
{
    public struct LightComponent : IComponentData
    {
        public Vector3 Color { get; set; }
        public float LightType { get; set; }
        
        public ItemIdentifier Light { get; set; }

        public void SetDefaultValues()
        {
            this.Color = new Vector3(1, 1, 1);
        }
    }
}
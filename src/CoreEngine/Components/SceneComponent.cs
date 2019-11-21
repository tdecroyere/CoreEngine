using System.Numerics;

namespace CoreEngine.Components
{
    public struct SceneComponent : IComponentData
    {
        public Entity? ActiveCamera { get; set; }

        public void SetDefaultValues()
        {
            this.ActiveCamera = null;
        }
    }
}
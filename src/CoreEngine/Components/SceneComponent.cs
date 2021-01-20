using System.Numerics;

namespace CoreEngine.Components
{
    public partial struct SceneComponent : IComponentData
    {
        public Entity? ActiveCamera { get; set; }
        public Entity? DebugCamera { get; set; }

        public void SetDefaultValues()
        {
            this.ActiveCamera = null;
            this.DebugCamera = null;
        }
    }
}
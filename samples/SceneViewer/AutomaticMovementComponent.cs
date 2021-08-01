using System;
using System.Numerics;

namespace CoreEngine.Samples.SceneViewer
{
    public partial struct AutomaticMovementComponent : IComponentData
    {
        public float Radius { get; set; }
        public float Speed { get; set; }
        public Vector3 OriginalPosition { get; set; }
        public Vector3 RandomValues { get; set; }

        public void SetDefaultValues()
        {
            this.Radius = 1.0f;
            this.Speed = 1.0f;
        }
    }
}
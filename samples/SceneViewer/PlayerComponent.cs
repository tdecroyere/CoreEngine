using System;
using System.Numerics;

namespace CoreEngine.Samples.SceneViewer
{
    public partial struct PlayerComponent : IComponentData
    {
        public Vector3 MovementVector { get; set; }
        public Vector3 MovementVelocity { get; set; }
        public Vector3 RotationVector { get; set; }
        public Vector3 RotationVelocity { get; set; }
        public float MovementAcceleration { get; set; }
        public float RotationAcceleration { get; set; }
        public bool IsActive { get; set; }

        public void SetDefaultValues()
        {
            this.IsActive = true;
            this.MovementAcceleration = 5000.0f;
            this.RotationAcceleration = 12000.0f;
        }
    }
}
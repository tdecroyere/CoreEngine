using System;
using System.Numerics;

namespace CoreEngine.Samples.SuperMario.Components
{
    public partial struct PlayerComponent : IComponentData
    {
        public Vector3 MovementVector { get; set; }
        public Vector3 MovementVelocity { get; set; }
        public float MovementAcceleration { get; set; }

        public void SetDefaultValues()
        {
            this.MovementAcceleration = 20000.0f;
        }
    }
}
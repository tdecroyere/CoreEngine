using System;
using System.Numerics;

namespace CoreEngine.Tests.EcsTest
{
    public struct PlayerComponent : IComponentData
    {
        public Vector3 MovementVector;
        public Vector3 MovementVelocity;
        public Vector3 RotationVector;
        public Vector3 RotationVelocity;
        public float MovementAcceleration;
        public float RotationAcceleration;

        public void SetDefaultValues()
        {
            this.MovementAcceleration = 5000.0f;
            this.RotationAcceleration = 12000.0f;
        }
    }
}
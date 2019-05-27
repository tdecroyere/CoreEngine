using System;
using System.Numerics;

namespace CoreEngine.Tests.EcsTest
{
    public struct PlayerComponent : IComponentData
    {
        public Vector3 TranslationVector;
        public Vector3 RotationVector;
        public float MovementSpeed;
        public float RotationSpeed;

        public void SetDefaultValues()
        {
            this.MovementSpeed = 50.0f;
            this.RotationSpeed = 150.0f;
        }
    }
}
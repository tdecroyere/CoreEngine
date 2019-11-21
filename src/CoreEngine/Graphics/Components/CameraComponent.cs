using System;
using System.Numerics;

namespace CoreEngine.Graphics.Components
{
    public struct CameraComponent : IComponentData
    {
        public Vector3 EyePosition { get; set; }
        public Vector3 LookAtPosition { get; set; }

        public void SetDefaultValues()
        {

        }
    }
}
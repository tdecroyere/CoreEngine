using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public struct CameraComponent : IComponentData
    {
        public Vector3 EyePosition;
        public Vector3 LookAtPosition;
        
        public void SetDefaultValues()
        {

        }
    }
}
using System;
using System.Numerics;
using CoreEngine.Collections;

namespace CoreEngine.Rendering.Components
{
    public partial struct CameraComponent : IComponentData
    {
        public Vector3 EyePosition { get; set; }
        public Vector3 LookAtPosition { get; set; }

        public ItemIdentifier Camera { get; set; }

        public void SetDefaultValues()
        {

        }
    }
}
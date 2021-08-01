using System.Numerics;

namespace CoreEngine.Components
{
    // TODO: Split the compute values like rotation quaternion and worldmatrix
    // in a separate component
    // TODO: Make scale into a float, for the moment we don't support non-uniform scaling
    // this should be threated as a special case without impacting the perf of the majority
    // of the meshes that will have uniform scaling
    public partial struct TransformComponent : IComponentData
    {
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public Quaternion RotationQuaternion { get; set; }
        public Matrix4x4 WorldMatrix { get; set; }
        public uint HasChanged { get; set; }
        
        public void SetDefaultValues()
        {
            this.Scale = Vector3.One;
            this.WorldMatrix = Matrix4x4.Identity;
        }
    }
}
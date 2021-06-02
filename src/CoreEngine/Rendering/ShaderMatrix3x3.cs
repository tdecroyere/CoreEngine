using System.Numerics;

namespace CoreEngine.Rendering
{
    public readonly struct ShaderMatrix3x3
    {
        public ShaderMatrix3x3(Matrix4x4 matrix)
        {
            // TODO: Optimize that with SIMD?
            this.M11 = matrix.M11;
            this.M21 = matrix.M21;
            this.M31 = matrix.M31;

            this.M12 = matrix.M12;
            this.M22 = matrix.M22;
            this.M32 = matrix.M32;

            this.M13 = matrix.M13;
            this.M23 = matrix.M23;
            this.M33 = matrix.M33;
        }

        public float M11 { get; }
        public float M21 { get; }
        public float M31 { get; }

        public float M12 { get; }
        public float M22 { get; }
        public float M32 { get; }

        public float M13 { get; }
        public float M23 { get; }
        public float M33 { get; }

        public static implicit operator ShaderMatrix3x3(Matrix4x4 matrix) => new ShaderMatrix3x3(matrix);
        public static ShaderMatrix3x3 FromMatrix4x4(Matrix4x4 matrix) => new ShaderMatrix3x3(matrix);
    }
}
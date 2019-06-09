using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public struct RenderPassConstants
    {
        public Matrix4x4 ViewMatrix { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }
    }
}
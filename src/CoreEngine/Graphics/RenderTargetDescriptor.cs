using System.Numerics;

namespace CoreEngine.Graphics
{
    public enum BlendOperation
    {
        None,
        AlphaBlending,
        AddOneOne,
        AddOneMinusSourceColor
    }

    public readonly struct RenderTargetDescriptor
    {
        public RenderTargetDescriptor(Texture colorTexture, Vector4? clearColor, BlendOperation blendOperation)
        {
            this.ColorTexture = colorTexture;
            this.ClearColor = clearColor;
            this.BlendOperation = blendOperation;
        }

        public readonly Texture ColorTexture { get; }
        public readonly Vector4? ClearColor { get; }
        public readonly BlendOperation BlendOperation { get; }
    }
}
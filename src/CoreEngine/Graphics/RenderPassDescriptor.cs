using System.Numerics;

namespace CoreEngine.Graphics
{
    public readonly struct RenderPassDescriptor
    {
        public RenderPassDescriptor(Texture colorTexture, Vector4? clearColor, Texture? depthTexture, bool depthCompare, bool depthWrite, bool backfaceCulling, bool writeToHardwareRenderTarget)
        {
            this.ColorTexture = colorTexture;
            this.ClearColor = clearColor;
            this.DepthTexture = depthTexture;
            this.DepthCompare = depthCompare;
            this.DepthWrite = depthWrite;
            this.BackfaceCulling = backfaceCulling;
            this.WriteToHardwareRenderTarget = writeToHardwareRenderTarget;
        }

        public readonly Texture ColorTexture { get; }
        public readonly Vector4? ClearColor { get; }
        public readonly Texture? DepthTexture { get; }
        public readonly bool DepthCompare { get; }
        public readonly bool DepthWrite { get; }
        public readonly bool BackfaceCulling { get; }
        public readonly bool WriteToHardwareRenderTarget { get; }
    }
}
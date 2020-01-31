using System.Numerics;

namespace CoreEngine.Graphics
{
    public enum DepthBufferOperation
    {
        None,
        CompareEqual,
        CompareLess,
        Write,
        ClearWrite
    }

    public readonly struct RenderPassDescriptor
    {
        public RenderPassDescriptor(RenderTargetDescriptor? renderTarget1, Texture? depthTexture, DepthBufferOperation depthBufferOperation, bool backfaceCulling)
        {
            this.RenderTarget1 = renderTarget1;
            this.RenderTarget2 = null;
            this.RenderTarget3 = null;
            this.RenderTarget4 = null;
            this.DepthTexture = depthTexture;
            this.DepthBufferOperation = depthBufferOperation;
            this.BackfaceCulling = backfaceCulling;
        }

        public RenderPassDescriptor(RenderTargetDescriptor renderTarget1, RenderTargetDescriptor renderTarget2, Texture? depthTexture, DepthBufferOperation depthBufferOperation, bool backfaceCulling)
        {
            this.RenderTarget1 = renderTarget1;
            this.RenderTarget2 = renderTarget2;
            this.RenderTarget3 = null;
            this.RenderTarget4 = null;
            this.DepthTexture = depthTexture;
            this.DepthBufferOperation = depthBufferOperation;
            this.BackfaceCulling = backfaceCulling;
        }

        public RenderPassDescriptor(RenderTargetDescriptor renderTarget1, RenderTargetDescriptor renderTarget2, RenderTargetDescriptor renderTarget3, Texture? depthTexture, DepthBufferOperation depthBufferOperation, bool backfaceCulling)
        {
            this.RenderTarget1 = renderTarget1;
            this.RenderTarget2 = renderTarget2;
            this.RenderTarget3 = renderTarget3;
            this.RenderTarget4 = null;
            this.DepthTexture = depthTexture;
            this.DepthBufferOperation = depthBufferOperation;
            this.BackfaceCulling = backfaceCulling;
        }

        public RenderPassDescriptor(RenderTargetDescriptor renderTarget1, RenderTargetDescriptor renderTarget2, RenderTargetDescriptor renderTarget3, RenderTargetDescriptor renderTarget4, Texture? depthTexture, DepthBufferOperation depthBufferOperation, bool backfaceCulling)
        {
            this.RenderTarget1 = renderTarget1;
            this.RenderTarget2 = renderTarget2;
            this.RenderTarget3 = renderTarget3;
            this.RenderTarget4 = renderTarget4;
            this.DepthTexture = depthTexture;
            this.DepthBufferOperation = depthBufferOperation;
            this.BackfaceCulling = backfaceCulling;
        }

        public readonly RenderTargetDescriptor? RenderTarget1 { get; }
        public readonly RenderTargetDescriptor? RenderTarget2 { get; }
        public readonly RenderTargetDescriptor? RenderTarget3 { get; }
        public readonly RenderTargetDescriptor? RenderTarget4 { get; }
        public readonly Texture? DepthTexture { get; }
        public readonly DepthBufferOperation DepthBufferOperation { get; }
        public readonly bool BackfaceCulling { get; }
    }
}
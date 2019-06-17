using System;

namespace CoreEngine.Graphics
{
    public readonly struct GeometryPacket
    {
        public GeometryPacket(VertexLayout vertexLayout, GraphicsBuffer vertexBuffer, GraphicsBuffer indexBuffer)
        {
            this.VertexLayout = vertexLayout;
            this.VertexBuffer = vertexBuffer;
            this.IndexBuffer = indexBuffer;
        }

        public readonly VertexLayout VertexLayout { get; }
        public readonly GraphicsBuffer VertexBuffer { get; }
        public readonly GraphicsBuffer IndexBuffer { get; }
    }
}
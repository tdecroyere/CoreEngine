using System;

namespace CoreEngine.Graphics
{
    public readonly struct GeometryPacket
    {
        public GeometryPacket(GraphicsBuffer vertexBuffer, GraphicsBuffer indexBuffer)
        {
            this.VertexBuffer = vertexBuffer;
            this.IndexBuffer = indexBuffer;
        }

        public readonly GraphicsBuffer VertexBuffer { get; }
        public readonly GraphicsBuffer IndexBuffer { get; }
    }
}
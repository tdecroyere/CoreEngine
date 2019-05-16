using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class MeshSubObject
    {
        internal MeshSubObject(GraphicsService graphicsService, MemoryService memoryService, Span<byte> vertexBufferData, Span<byte> indexBufferData)
        {
            var vertexMemoryBuffer = memoryService.CreateMemoryBuffer(vertexBufferData.Length);
            var indexMemoryBuffer = memoryService.CreateMemoryBuffer(indexBufferData.Length);

            this.VertexBuffer = new GraphicsBuffer(graphicsService, vertexMemoryBuffer);
            this.IndexBuffer = new GraphicsBuffer(graphicsService, indexMemoryBuffer);

            memoryService.DestroyMemoryBuffer(vertexMemoryBuffer.Id);
            memoryService.DestroyMemoryBuffer(indexMemoryBuffer.Id);
        }

        public GraphicsBuffer VertexBuffer { get; }
        public GraphicsBuffer IndexBuffer { get; }
    }
}
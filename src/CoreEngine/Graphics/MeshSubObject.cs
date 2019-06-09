using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class MeshSubObject
    {
        internal MeshSubObject(GraphicsManager graphicsManager, MemoryService memoryService, int vertexCount, int indexCount, Span<byte> vertexBufferData, Span<byte> indexBufferData)
        {
            this.VertexCount = vertexCount;
            this.IndexCount = indexCount;

            var vertexMemoryBuffer = memoryService.CreateMemoryBuffer(vertexBufferData.Length);

            if (!vertexBufferData.TryCopyTo(vertexMemoryBuffer.AsSpan()))
            {
                throw new InvalidOperationException("Error while copying vertex buffer data.");
            }

            var indexMemoryBuffer = memoryService.CreateMemoryBuffer(indexBufferData.Length);

            if (!indexBufferData.TryCopyTo(indexMemoryBuffer.AsSpan()))
            {
                throw new InvalidOperationException("Error while copying index buffer data.");
            }

            this.VertexBuffer = graphicsManager.CreateGraphicsBuffer(vertexMemoryBuffer);
            this.IndexBuffer = graphicsManager.CreateGraphicsBuffer(indexMemoryBuffer);

            memoryService.DestroyMemoryBuffer(vertexMemoryBuffer.Id);
            memoryService.DestroyMemoryBuffer(indexMemoryBuffer.Id);
        }

        public int VertexCount { get; }
        public int IndexCount { get; }
        public GraphicsBuffer VertexBuffer { get; }
        public GraphicsBuffer IndexBuffer { get; }
    }
}
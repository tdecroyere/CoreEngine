using System;

namespace CoreEngine.Graphics
{
    public class GraphicsBuffer
    {
        internal GraphicsBuffer(GraphicsService graphicsService, MemoryBuffer memoryBuffer)
        {
            this.SizeInBytes = memoryBuffer.Length;
            this.Id = graphicsService.CreateGraphicsBuffer(memoryBuffer);
        }

        public uint Id { get; }
        public int SizeInBytes { get; }
    }
}
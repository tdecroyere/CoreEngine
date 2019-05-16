using System;

namespace CoreEngine.Graphics
{
    public class GraphicsBuffer
    {
        internal GraphicsBuffer(GraphicsService graphicsService, MemoryBuffer memoryBuffer)
        {
            this.SizeInBytes = memoryBuffer.Length;

            // TODO: Call graphics service create graphics buffer method
        }

        public int Id { get; }

        public int SizeInBytes { get; }
    }
}
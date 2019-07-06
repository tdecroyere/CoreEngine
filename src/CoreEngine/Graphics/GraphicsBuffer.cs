using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer
    {
        private readonly MemoryBuffer? internalMemoryBuffer;

        internal GraphicsBuffer(uint id, int sizeInBytes)
        {
            this.Id = id;
            this.SizeInBytes = sizeInBytes;
            this.BufferType = GraphicsBufferType.Static;
            this.internalMemoryBuffer = null;
        }

        internal GraphicsBuffer(MemoryBuffer memoryBuffer)
        {
            this.Id = memoryBuffer.Id;
            this.SizeInBytes = memoryBuffer.Length;
            this.BufferType = GraphicsBufferType.Dynamic;
            this.internalMemoryBuffer = memoryBuffer;
        }

        public readonly uint Id { get; }
        public readonly int SizeInBytes { get; }
        public readonly GraphicsBufferType BufferType { get; }

        public MemoryBuffer MemoryBuffer
        {
            get
            {
                if (this.BufferType == GraphicsBufferType.Static)
                {
                    throw new InvalidOperationException("The current graphics buffer is static");
                }

                return this.internalMemoryBuffer!.Value;
            }
        }
    }
}
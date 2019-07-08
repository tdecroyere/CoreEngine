using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer
    {
        private readonly HostMemoryBuffer? internalMemoryBuffer;

        internal GraphicsBuffer(uint id, uint sizeInBytes)
        {
            this.Id = id;
            this.SizeInBytes = sizeInBytes;
            this.BufferType = GraphicsBufferType.Static;
            this.internalMemoryBuffer = null;
        }

        internal GraphicsBuffer(HostMemoryBuffer memoryBuffer)
        {
            this.Id = memoryBuffer.Id;
            this.SizeInBytes = memoryBuffer.Length;
            this.BufferType = GraphicsBufferType.Dynamic;
            this.internalMemoryBuffer = memoryBuffer;
        }

        public readonly uint Id { get; }
        public readonly uint SizeInBytes { get; }
        public readonly GraphicsBufferType BufferType { get; }

        public HostMemoryBuffer MemoryBuffer
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
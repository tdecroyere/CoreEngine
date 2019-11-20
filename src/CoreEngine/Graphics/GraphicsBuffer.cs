using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer
    {
        private readonly Memory<byte>? internalMemoryBuffer;

        internal GraphicsBuffer(uint id, int sizeInBytes, GraphicsBufferType graphicsBufferType)
        {
            this.Id = id;
            this.SizeInBytes = sizeInBytes;
            this.BufferType = graphicsBufferType;
            this.internalMemoryBuffer = null;

            if (this.BufferType == GraphicsBufferType.Dynamic)
            {
                this.internalMemoryBuffer = new Memory<byte>(new byte[sizeInBytes]);
            }
        }

        public readonly uint Id { get; }
        public readonly int SizeInBytes { get; }
        public readonly GraphicsBufferType BufferType { get; }

        public Span<byte> MemoryBuffer
        {
            get
            {
                if (this.BufferType == GraphicsBufferType.Static)
                {
                    throw new InvalidOperationException("The current graphics buffer is static");
                }

                return this.internalMemoryBuffer!.Value.Span;
            }
        }
    }
}
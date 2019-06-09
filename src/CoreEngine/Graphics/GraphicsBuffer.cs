using System;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer
    {
        internal GraphicsBuffer(uint id, int sizeInBytes)
        {
            this.Id = id;
            this.SizeInBytes = sizeInBytes;
        }

        public readonly uint Id { get; }
        public readonly int SizeInBytes { get; }
    }
}
using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandBuffer
    {
        internal CommandBuffer(uint id)
        {
            this.Id = id;
        }

        public readonly uint Id { get; }
    }
}
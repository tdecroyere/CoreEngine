using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandQueue
    {
        internal CommandQueue(uint id, CommandType type, string label)
        {
            this.Id = id;
            this.Type = type;
            this.Label = label;
        }

        public readonly uint Id { get; }
        public readonly CommandType Type { get; }
        public readonly string Label { get; }
    }
}
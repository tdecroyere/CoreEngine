using System;

namespace CoreEngine.Graphics
{
    public readonly struct CommandList
    {
        internal CommandList(uint id, CommandListType type)
        {
            this.Id = id;
            this.Type = type;
        }

        public readonly uint Id { get; }
        public readonly CommandListType Type { get; }
    }
}
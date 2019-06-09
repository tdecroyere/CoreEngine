using System;

namespace CoreEngine
{
    public readonly struct Entity
    {
        public Entity(uint id)
        {
            this.EntityId = id;
        }

        public readonly uint EntityId { get; }
    }
}
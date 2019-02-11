using System;

namespace CoreEngine
{
    public struct Entity
    {
        public Entity(uint id)
        {
            this.EntityId = id;
        }

        public uint EntityId;
    }
}
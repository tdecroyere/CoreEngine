using System;

namespace CoreEngine
{
    public readonly struct Entity : IEquatable<Entity>
    {
        public Entity(uint id)
        {
            this.EntityId = id;
        }

        public readonly uint EntityId { get; }

        public override bool Equals(Object obj) 
        {
            return obj is Entity && this == (Entity)obj;
        }

        public bool Equals(Entity other)
        {
            return this == other;
        }

        public override int GetHashCode() 
        {
            return this.EntityId.GetHashCode();
        }

        public static bool operator ==(Entity entity1, Entity entity2) 
        {
            return entity1.EntityId == entity2.EntityId;
        }

        public static bool operator !=(Entity entity1, Entity entity2) 
        {
            return !(entity1 == entity2);
        }
    }
}
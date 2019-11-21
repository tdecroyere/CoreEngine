using System;

namespace CoreEngine.Collections
{
    public readonly struct ItemIdentifier : IEquatable<ItemIdentifier>
    {
        public ItemIdentifier(uint id)
        {
            this.Id = id;
        }

        public readonly uint Id { get; }

        public override bool Equals(Object? obj) 
        {
            return obj is ItemIdentifier && this == (ItemIdentifier)obj;
        }

        public bool Equals(ItemIdentifier other)
        {
            return this == other;
        }

        public override int GetHashCode() 
        {
            return this.Id.GetHashCode();
        }

        public static bool operator ==(ItemIdentifier item1, ItemIdentifier item2) 
        {
            return item1.Id == item2.Id;
        }

        public static bool operator !=(ItemIdentifier item1, ItemIdentifier item2) 
        {
            return !(item1 == item2);
        }
    }
}
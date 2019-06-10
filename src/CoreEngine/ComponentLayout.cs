using System;

namespace CoreEngine
{
    public readonly struct ComponentLayout : IEquatable<ComponentLayout>
    {
        public ComponentLayout(uint id)
        {
            this.EntityComponentLayoutId = id;
        }
        
        public readonly uint EntityComponentLayoutId { get; }

        public override bool Equals(Object obj) 
        {
            return obj is ComponentLayout && this == (ComponentLayout)obj;
        }

        public bool Equals(ComponentLayout other)
        {
            return this == other;
        }

        public override int GetHashCode() 
        {
            return this.EntityComponentLayoutId.GetHashCode();
        }

        public static bool operator ==(ComponentLayout layout1, ComponentLayout layout2) 
        {
            return layout1.EntityComponentLayoutId == layout2.EntityComponentLayoutId;
        }

        public static bool operator !=(ComponentLayout layout1, ComponentLayout layout2) 
        {
            return !(layout1 == layout2);
        }
    }
}
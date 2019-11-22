using System;
using System.Numerics;

namespace CoreEngine.Collections
{
    public abstract class TrackedItem
    {
        protected TrackedItem()
        {
            this.Id = ItemIdentifier.Empty;
            this.IsAlive = true;
            this.IsDirty = true;
        }

        public ItemIdentifier Id { get; internal set; }
        public bool IsAlive { get; internal set; }
        public bool IsDirty { get; internal set; }

        protected void UpdateField<T>(ref T field, T value) where T : IEquatable<T>
        {
            if (!field.Equals(value))
            {
                field = value;
                this.IsDirty = true;
            }

            this.IsAlive = true;
        }
    }
}
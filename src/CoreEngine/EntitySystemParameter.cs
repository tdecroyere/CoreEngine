using System;

namespace CoreEngine
{
    public class EntitySystemParameter
    {
        public EntitySystemParameter(Type componentType, bool isReadOnly = false)
        {
            this.ComponentType = componentType;
            this.IsReadOnly = isReadOnly;
        }

        public Type ComponentType { get; }
        public bool IsReadOnly { get; }
    }
}
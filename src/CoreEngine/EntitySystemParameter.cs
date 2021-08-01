namespace CoreEngine
{
    public interface IEntitySystemParameter
    {
        ComponentHash ComponentHash { get; }
        bool IsReadOnly { get; }
    }

    public class EntitySystemParameter<T> : IEntitySystemParameter where T: struct, IComponentData
    {
        public EntitySystemParameter(bool isReadOnly = false)
        {
            this.ComponentHash = new T().GetComponentHash();
            this.IsReadOnly = isReadOnly;
        }

        public ComponentHash ComponentHash { get; }
        public bool IsReadOnly { get; }
    }
}
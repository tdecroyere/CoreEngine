using System.Collections.Generic;

namespace CoreEngine
{
    public class EntitySystemDefinition
    {
        public EntitySystemDefinition(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
        public IList<EntitySystemParameter> Parameters { get; } = new List<EntitySystemParameter>();
    }
}
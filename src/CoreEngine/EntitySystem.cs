using System;
using System.Collections.Generic;

namespace CoreEngine
{
    public abstract class EntitySystem
    {
        // TODO: Storing ArrayPool buffers here is a not great, we do that for the moment so the API is cleaner
        private Entity[] entitiesArray = new Entity[0];
        private IDictionary<int, byte[]> componentsData = new Dictionary<int, byte[]>();

        public abstract EntitySystemDefinition BuildDefinition();
        
        public abstract void Process(float deltaTime);

        protected ReadOnlySpan<Entity> GetEntityArray()
        {
            return entitiesArray;
        }

        internal void SetEntityArray(Entity[] data)
        {
            this.entitiesArray = data;
        }

        protected Span<T> GetComponentDataArray<T>() where T : struct, IComponentData
        {
            // TODO: Check system registered definitions

            throw new NotImplementedException();
        }

        internal void SetComponentDataArray<T>(Span<T> data) where T : struct, IComponentData
        {
            // TODO: Check system registered definitions

            throw new NotImplementedException();
        }
    }
}
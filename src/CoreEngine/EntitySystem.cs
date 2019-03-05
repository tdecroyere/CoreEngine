using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public abstract class EntitySystem
    {
        private EntitySystemData? entitySystemData;

        public abstract EntitySystemDefinition BuildDefinition();
        
        public abstract void Process(float deltaTime);

        protected ReadOnlySpan<Entity> GetEntityArray()
        {
            if (entitySystemData != null)
            {
                return this.entitySystemData.entitiesArray;
            }

            throw new InvalidOperationException("Entity system data was never set.");
        }

        protected Span<T> GetComponentDataArray<T>() where T : struct, IComponentData
        {
            // TODO: Check system registered definitions

            if (entitySystemData != null)
            {
                var componentTypeHashCode = typeof(T).GetHashCode();

                if (!this.entitySystemData.componentsData.ContainsKey(componentTypeHashCode))
                {
                    throw new ArgumentException("The type passed is not registered by the system.");
                }
                    
                return MemoryMarshal.Cast<byte, T>(this.entitySystemData.componentsData[componentTypeHashCode].AsSpan());
            }

            throw new InvalidOperationException("Entity system data was never set.");
        }

        internal void SetEntitySystemData(EntitySystemData entitySystemData)
        {
            this.entitySystemData = entitySystemData;
        }
    }
}
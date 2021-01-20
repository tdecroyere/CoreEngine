using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreEngine.Collections;

namespace CoreEngine
{
    public abstract class EntitySystem
    {
        private EntitySystemData? entitySystemData;

        public abstract EntitySystemDefinition BuildDefinition();
        
        public abstract void Process(EntityManager entityManager, float deltaTime);

        protected EntitySystemArray<Entity> GetEntityArray()
        {
            if (entitySystemData != null)
            {
                return entitySystemData.EntityArray;
            }

            throw new InvalidOperationException("Entity system data was never set.");
        }

        protected EntitySystemArray<T> GetComponentDataArray<T>() where T : struct, IComponentData
        {
            // TODO: Check system registered definitions

            if (entitySystemData != null)
            {
                var componentTypeHashCode = new T().GetComponentHash();

                if (!this.entitySystemData.ComponentsData.ContainsKey(componentTypeHashCode))
                {
                    throw new ArgumentException("The type passed is not registered by the system.");
                }

                return new EntitySystemArray<T>(this.entitySystemData.ComponentsData[componentTypeHashCode]);
            }

            throw new InvalidOperationException("Entity system data was never set.");
        }

        internal void SetEntitySystemData(EntitySystemData entitySystemData)
        {
            this.entitySystemData = entitySystemData;
        }
    }
}
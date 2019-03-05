using System;
using System.Collections.Generic;

namespace CoreEngine
{
    // TODO: Manage exclusive component definitions

    public class EntitySystemManager
    {
        private EntityManager entityManager;
        private IList<EntitySystemDefinition> registeredSystemDefinitions;

        // TODO: Refactor that!
        private IList<Type[]> componentTypes;

        public IList<EntitySystem> RegisteredSystems { get; } = new List<EntitySystem>();
        
        public EntitySystemManager(EntityManager entityManager)
        {
            this.entityManager = entityManager;
            this.registeredSystemDefinitions = new List<EntitySystemDefinition>();
            this.componentTypes = new List<Type[]>();
        }

        public void RegisterSystem(EntitySystem entitySystem)
        {
            if (this.RegisteredSystems.Contains(entitySystem))
            {
                throw new ArgumentException("The specified entity system has already been registered.");
            }

            var systemDefinition = entitySystem.BuildDefinition();
            this.registeredSystemDefinitions.Add(systemDefinition);

            this.RegisteredSystems.Add(entitySystem);

            // TODO: Be carrefull of memory management and small buffers
            var componentTypes = new Type[systemDefinition.Parameters.Count];
            this.componentTypes.Add(componentTypes);

            for (var i = 0; i < componentTypes.Length; i++)
            {
                componentTypes[i] = systemDefinition.Parameters[i].ComponentType;
            }
        }

        public void Process(float deltaTime)
        {
            // TODO: For the moment the systems are executed sequentially
            // TODO: Add multi-thread

            for (var i = 0; i < this.RegisteredSystems.Count; i++)
            {
                // TODO: Get component data in byte arrays from the entity manager
                // TODO: The entitymanager method that do the extract, copy the data to a passed array
                // TODO: The passed array is allocated here
                // TODO: Use the array pool class because the data is temporary
                
                var entitySystem = this.RegisteredSystems[i];
                var entitySystemDefinition = this.registeredSystemDefinitions[i];

                var entitySystemData = this.entityManager.GetEntitySystemData(this.componentTypes[i]);

                entitySystem.SetEntitySystemData(entitySystemData);
                entitySystem.Process(deltaTime);
            }
        }
    }
}
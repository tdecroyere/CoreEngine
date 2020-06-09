using System;
using System.Collections.Generic;

namespace CoreEngine
{
    // TODO: Manage exclusive component definitions
    // TODO: Manage entities by grid for large worlds
    // TODO: Allow entity system manager to ask for local queries

    public class EntitySystemManager
    {
        private readonly SystemManagerContainer systemManagerContainer;
        private IList<EntitySystemDefinition> registeredSystemDefinitions;

        // TODO: Refactor that!
        private IList<Type> registeredSystemTypes;
        private IList<Type[]> componentTypes;
        
        public EntitySystemManager(SystemManagerContainer systemManagerContainer)
        {
            this.systemManagerContainer = systemManagerContainer;
            this.registeredSystemDefinitions = new List<EntitySystemDefinition>();
            this.registeredSystemTypes = new List<Type>();
            this.componentTypes = new List<Type[]>();
        }

        public IList<EntitySystem> RegisteredSystems { get; } = new List<EntitySystem>();

        // TODO: Try to find a way to avoid reflection with code generator
        public void RegisterEntitySystem<T>() where T : EntitySystem
        {
            if (this.registeredSystemTypes.Contains(typeof(T)))
            {
                throw new ArgumentException("The specified entity system has already been registered.");
            }

            this.registeredSystemTypes.Add(typeof(T));

            // TODO: Use manager container to create object
            var entitySystem = this.systemManagerContainer.CreateInstance<T>();

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

        public void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                throw new ArgumentNullException(nameof(entityManager));
            }

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

                var entitySystemData = entityManager.GetEntitySystemData(this.componentTypes[i]);

                entitySystem.SetEntitySystemData(entitySystemData);
                entitySystem.Process(entityManager, deltaTime);
            }
        }
    }
}
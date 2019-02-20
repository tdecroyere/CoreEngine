using System;
using System.Collections.Generic;

namespace CoreEngine
{
    // TODO: Manage exclusive component definitions

    public class EntitySystemManager
    {
        public IList<EntitySystem> RegisteredSystems { get; } = new List<EntitySystem>();
        
        public void RegisterSystem(EntitySystem entitySystem)
        {
            if (this.RegisteredSystems.Contains(entitySystem))
            {
                throw new ArgumentException("The specified entity system has already been registered.");
            }

            this.RegisteredSystems.Add(entitySystem);
        }

        public void Process(float deltaTime)
        {
            // TODO: For the moment the systems are executed sequentially
            // TODO: Add multi-thread

            for (var i = 0; i < this.RegisteredSystems.Count; i++)
            {
                // TODO: Get System registration
                // TODO: Get component data in byte arrays from the entity manager
                // TODO: The entitymanager method that do the extract, copy the data to a passed array
                // TODO: The passed array is allocated here
                // TODO: Use the array pool class because the data is temporary
                
                var entitySystem = this.RegisteredSystems[i];
                entitySystem.Process(deltaTime);
            }
        }
    }
}
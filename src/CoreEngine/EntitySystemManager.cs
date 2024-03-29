namespace CoreEngine
{
    public class EntitySystemManagerEntry
    {
        public EntitySystemManagerEntry(string typeName)
        {
            this.TypeName = typeName;
        }

        public string TypeName { get; }

        public Memory<ComponentHash>? ComponentHashs { get; set; }
        public EntitySystem? EntitySystem { get; set; }
        public EntitySystemDefinition? SystemDefinition { get; set; }
    }

    // TODO: Manage exclusive component definitions
    // TODO: Manage entities by grid for large worlds
    // TODO: Allow entity system manager to ask for local queries

    public class EntitySystemManager
    {
        public IList<EntitySystemManagerEntry> RegisteredSystems { get; private set; }
        
        public EntitySystemManager()
        {
            this.RegisteredSystems = new List<EntitySystemManagerEntry>();
        }

        // TODO: Try to find a way to avoid reflection with code generator
        public void RegisterEntitySystem<T>() where T : EntitySystem
        {
            for (var i = 0; i < this.RegisteredSystems.Count; i++)
            {
                if (this.RegisteredSystems[i].TypeName == typeof(T).AssemblyQualifiedName)
                {
                    throw new ArgumentException($"The entity system '{typeof(T).AssemblyQualifiedName}' has already been registered.");
                }
            }

            this.RegisteredSystems.Add(new EntitySystemManagerEntry(typeof(T).AssemblyQualifiedName));
        }

        public void UnbindRegisteredSystems()
        {
            for (var i = 0; i < this.RegisteredSystems.Count; i++)
            {
                this.RegisteredSystems[i].EntitySystem = null;
                this.RegisteredSystems[i].ComponentHashs = null;
            }
        }

        public void Process(SystemManagerContainer systemManagerContainer, EntityManager entityManager, float deltaTime)
        {
            if (systemManagerContainer is null)
            {
                throw new ArgumentNullException(nameof(systemManagerContainer));
            }

            if (entityManager is null)
            {
                throw new ArgumentNullException(nameof(entityManager));
            }

            var pluginManager = systemManagerContainer.GetSystemManager<PluginManager>();

            // TODO: For the moment the systems are executed sequentially
            // TODO: Add multi-thread

            // Logger.BeginAction("Update Component System");

            for (var i = 0; i < this.RegisteredSystems.Count; i++)
            {
                // TODO: Get component data in byte arrays from the entity manager
                // TODO: The entitymanager method that do the extract, copy the data to a passed array
                // TODO: The passed array is allocated here
                // TODO: Use the array pool class because the data is temporary

                var registeredSystem = this.RegisteredSystems[i];
                
                if (registeredSystem.EntitySystem == null)
                {
                    Logger.WriteMessage($"Rebind EntitySystem: '{registeredSystem.TypeName}'...");

                    // TODO: Use manager container to create object
                    registeredSystem.EntitySystem = systemManagerContainer.CreateInstance<EntitySystem>(Type.GetType(registeredSystem.TypeName, (assemblyName) => {
                        return pluginManager.FindLoadedAssembly(assemblyName.FullName);
                    }, null));

                    var systemDefinition = registeredSystem.EntitySystem.BuildDefinition();

                    registeredSystem.SystemDefinition = systemDefinition;
                    // TODO: Be carreful of memory management and small buffers
                    registeredSystem.ComponentHashs = new ComponentHash[systemDefinition.Parameters.Count];

                    for (var j = 0; j < registeredSystem.ComponentHashs.Value.Length; j++)
                    {
                        registeredSystem.ComponentHashs.Value.Span[j] = systemDefinition.Parameters[j].ComponentHash;
                    }

                    registeredSystem.EntitySystem.Setup(entityManager);
                }

                var entitySystem = registeredSystem.EntitySystem;
                var componentHashs = this.RegisteredSystems[i].ComponentHashs;

                if (entitySystem != null && componentHashs.HasValue)
                {
                    entityManager.FillEntitySystemData(entitySystem.entitySystemData, componentHashs.Value.Span);
                    entitySystem.Process(entityManager, deltaTime);
                }
            }

            // Logger.EndAction();
        }
    }
}
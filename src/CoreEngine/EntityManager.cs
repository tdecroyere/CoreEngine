namespace CoreEngine
{
    public class EntityManager
    {
        // TODO: Switch to memory manager
        // TODO: Don't use list index for entity id because otherwise it is impossible to implement delete
        // TODO: Not thread safe for the moment
        // TODO: Use something different than an array of bytes?
        // TODO: How to handle open world entities? (Large open-worlds, use Grid?)

        private readonly List<EntityInfo> entities;

        // TODO: Replace the key with component layout or create a ComponentLayoutInfo
        private readonly Dictionary<ComponentHash, ComponentStorage> componentStorage;

        private readonly byte[] componentDataStorage;
        private int currentDataIndex;

        /// <summary>
        /// Constructs a new <c>EntityManager</c> object.
        /// </summary>
        public EntityManager()
        {
            this.entities = new List<EntityInfo>();
            this.componentStorage = new Dictionary<ComponentHash, ComponentStorage>();

            // TODO: Init a basic 50 MB buffer for now
            // TODO: Replace that with a proper memory manager
            this.componentDataStorage = new byte[Utils.MegaBytesToBytes(256)];
            this.currentDataIndex = 0;
        }

        public ComponentLayout CreateComponentLayout<T1>() where T1: struct, IComponentData
        {
            var componentLayout = CreateComponentLayout();

            RegisterComponent<T1>(componentLayout);
            
            return componentLayout;
        }

        public ComponentLayout CreateComponentLayout<T1, T2>() where T1: struct, IComponentData 
                                                               where T2: struct, IComponentData
        {
            var componentLayout = CreateComponentLayout();

            RegisterComponent<T1>(componentLayout);
            RegisterComponent<T2>(componentLayout);
            
            return componentLayout;
        }

        public ComponentLayout CreateComponentLayout<T1, T2, T3>() where T1: struct, IComponentData 
                                                                   where T2: struct, IComponentData
                                                                   where T3: struct, IComponentData
        {
            var componentLayout = CreateComponentLayout();

            RegisterComponent<T1>(componentLayout);
            RegisterComponent<T2>(componentLayout);
            RegisterComponent<T3>(componentLayout);
            
            return componentLayout;
        }

        // TODO: Allow the addition of component after component layout
        // When an entity is created with this layout, the layout cannot be modified after that.
        public ComponentLayout CreateComponentLayout()
        {
            return new ComponentLayout();
        }
        
        public void RegisterComponentLayoutComponent(ComponentLayout componentLayout, ComponentHash componentHash, int componentSize, ReadOnlyMemory<byte> componentInitialData)
        {
            componentLayout.RegisterComponent(componentHash, componentSize, componentInitialData);
        }

        public Entity CreateEntity(ComponentLayout componentLayout)
        {
            // TODO: Check for existing entities
            if (componentLayout is null)
            {
                throw new ArgumentNullException(nameof(componentLayout));
            }
            
            if (!componentLayout.IsReadOnly)
            {
                componentLayout.IsReadOnly = true;
                this.componentStorage.Add(componentLayout.LayoutHash, new ComponentStorage(componentLayout));
            }

            var entity = new Entity((uint)this.entities.Count + 1);

            var dataStorage = this.componentStorage[componentLayout.LayoutHash];
            var memoryChunk = FindMemoryChunk(dataStorage);

            if (memoryChunk == null)
            {
                memoryChunk = CreateMemoryChunk(dataStorage);
            }

            var chunkIndex = sizeof(uint) * memoryChunk.EntityCount;
            MemoryMarshal.Write(memoryChunk!.Storage.Span[chunkIndex..], ref entity);

            // FIXME: We need a dictionary here because we can remove entities and potentially
            // re-use entity id and we need to keep the list packed
            var entityInfo = new EntityInfo(entity, componentLayout, memoryChunk, memoryChunk.EntityCount);
            this.entities.Add(entityInfo);

            memoryChunk.EntityCount++;

            return entity;
        }

        public ReadOnlySpan<Entity> GetEntities()
        {
            var entities = new Entity[this.entities.Count];

            for (int i = 0; i < this.entities.Count; i++)
            {
                entities[i] = new Entity((uint)i + 1);
            }
            
            return entities.AsSpan();
        }

        public ReadOnlySpan<Entity> GetEntitiesByComponentType<T>() where T: struct, IComponentData
        {
            var entities = new List<Entity>();

            for (int i = 0; i < this.entities.Count; i++)
            {
                var entity = new Entity((uint)i + 1);

                if (this.HasComponent<T>(entity))
                {
                    entities.Add(entity);
                }
            }
            
            return entities.ToArray();
        }

        public ComponentLayout GetEntityComponentLayout(Entity entity)
        {
            return this.entities[(int)entity.EntityId - 1].ComponentLayout;
        }

        public void SetComponentData<T>(Entity entity, T component) where T: struct, IComponentData
        {
            var componentHash = component.GetComponentHash();

            var size = Marshal.SizeOf(component);
            var componentData = new byte[size];
            MemoryMarshal.Write(componentData, ref component);

            SetComponentData(entity, componentHash, componentData);
        }

        public void SetComponentData(Entity entity, ComponentHash componentHash, ReadOnlySpan<byte> data)
        {
            var entityInfo = this.entities[(int)entity.EntityId - 1];

            var componentLayout = entityInfo.ComponentLayout; 
            var memoryChunk = entityInfo.MemoryChunk;

            var componentOffset = componentLayout.FindComponentOffset(componentHash);

            if (componentOffset == null)
            {
                throw new ArgumentException("Component type is not part of the entity component layout.", nameof(componentHash));
            }

            var componentSize = componentLayout.FindComponentSizeInBytes(componentHash);
         
            var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset.Value, componentSize, entityInfo.LocalStorageIndex);
            data.CopyTo(memoryChunk.Storage.Span.Slice(storageComponentOffet));
        }

        public T GetComponentData<T>(Entity entity) where T : struct, IComponentData
        {
            // TODO: Use ref return?
            // TODO: Make a function for entity indexing
            // TODO: Use Index type?
            var entityInfo = this.entities[(int)entity.EntityId - 1];

            var componentHash = new T().GetComponentHash();
            var componentLayout = entityInfo.ComponentLayout;
            var memoryChunk = entityInfo.MemoryChunk;

            var componentOffset = componentLayout.FindComponentOffset(componentHash);

            if (componentOffset == null)
            {
                throw new ArgumentException("Component type it part of the entity component layout.", nameof(T));
            }

            var componentSize = componentLayout.FindComponentSizeInBytes(componentHash);

            var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset.Value, componentSize, entityInfo.LocalStorageIndex);
            return MemoryMarshal.Read<T>(memoryChunk.Storage.Span.Slice(storageComponentOffet));
        }

        internal static int ComputeDataChunkComponentOffset(ComponentStorageMemoryChunk memoryChunk, int componentOffset, int componentSize, int entityLocalIndex)
        {
            return memoryChunk.MaxEntityCount * sizeof(uint) + memoryChunk.MaxEntityCount * componentOffset + entityLocalIndex * componentSize;
        }

        public bool HasComponent<T>(Entity entity) where T : struct, IComponentData
        {
            var componentHash = new T().GetComponentHash();
            var componentLayout = this.entities[(int)entity.EntityId - 1].ComponentLayout;

            for (var i = 0; i < componentLayout.Components.Count; i++)
            {
                if (componentLayout.Components[i].Hash == componentHash)
                {
                    return true;
                }
            }

            return false;
        }

        // TODO: Replace that with a query system that build the data after each entity/componenet modification?
        internal void FillEntitySystemData(EntitySystemData entitySystemData, ReadOnlySpan<ComponentHash> componentHashCodes)
        {
            var currentMemoryChunkIndex = 0;

            // TODO: Replace that with a faster imp
            foreach (var dataStorage in this.componentStorage.Values)
            {
                var componentLayout = dataStorage.ComponentLayout;
                var numberOfMatches = 0;

                for (var j = 0; j < componentLayout.Components.Count; j++)
                {
                    var component = componentLayout.Components[j];
                    
                    for (var k = 0; k < componentHashCodes.Length; k++)
                    {
                        if (component.Hash == componentHashCodes[k])
                        {
                            numberOfMatches++;
                        }
                    }
                }

                if (numberOfMatches == componentHashCodes.Length)
                {
                    if (!this.componentStorage.ContainsKey(componentLayout.LayoutHash))
                    {
                        continue;
                    }

                    for (var j = 0; j < dataStorage.MemoryChunks.Count; j++)
                    {
                        var memoryChunk = dataStorage.MemoryChunks[j];
                        entitySystemData.WorkingMemoryChunks[currentMemoryChunkIndex++] = memoryChunk;
                    }
                }
            }

            entitySystemData.MemoryChunks = entitySystemData.WorkingMemoryChunks.AsMemory().Slice(0, currentMemoryChunkIndex);
        }

        private static int ComputeChunkItemSize(ComponentLayout componentLayoutDesc)
        {
            return sizeof(uint) + componentLayoutDesc.SizeInBytes;
        }

        private ComponentStorageMemoryChunk CreateMemoryChunk(ComponentStorage dataStorage)
        {
            var componentLayout = dataStorage.ComponentLayout;
            var chunkItemSize = ComputeChunkItemSize(componentLayout);

            // Start first with small chunk count then increase it base on usage?
            // Store 10000 entities per chunk for now
            //var entityCount = 10000;

            // TODO: We need to align the memory here

            var dataChunkSize = (int)Utils.KiloBytesToBytes(16);//entityCount * chunkItemSize;
            var entityCount = dataChunkSize / chunkItemSize;

            var memoryStorage = this.componentDataStorage.AsMemory(this.currentDataIndex, dataChunkSize);
            this.currentDataIndex += dataChunkSize;

            var memoryChunk = new ComponentStorageMemoryChunk(dataStorage.ComponentLayout, memoryStorage, chunkItemSize, entityCount);
            dataStorage.MemoryChunks.Add(memoryChunk);

            for (var i = 0; i < componentLayout.Components.Count; i++)
            {
                var component = componentLayout.Components[i];

                if (component.DefaultData != null)
                {
                    var componentOffset = componentLayout.FindComponentOffset(component.Hash);
                    var componentSize = componentLayout.FindComponentSizeInBytes(component.Hash);

                    for (var j = 0; j < entityCount; j++)
                    {
                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset!.Value, componentSize, j);
                        component.DefaultData.Value.CopyTo(memoryChunk.Storage.Slice(storageComponentOffet));
                    }
                }
            }

            return memoryChunk;
        }

        private static ComponentStorageMemoryChunk? FindMemoryChunk(ComponentStorage dataStorage)
        {
            // TODO: For the data storage store a list of available memory chunks
            for (var i = 0; i < dataStorage.MemoryChunks.Count; i++)
            {
                if (dataStorage.MemoryChunks[i].EntityCount < dataStorage.MemoryChunks[i].MaxEntityCount)
                {
                    return dataStorage.MemoryChunks[i];
                }
            }

            return null;
        }

        private static void RegisterComponent<T>(ComponentLayout componentLayout) where T: struct, IComponentData 
        {
            var component = new T();
            component.SetDefaultValues();
            var componentSize = Marshal.SizeOf(component);

            var componentInitialData = new byte[componentSize];
            MemoryMarshal.Write(componentInitialData, ref component);

            componentLayout.RegisterComponent(component.GetComponentHash(), componentSize, componentInitialData);
        }
    }
}
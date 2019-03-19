using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public class EntityManager
    {
        // TODO: Switch to memory manager
        // TODO: For now, just a naive implementation
        // TODO: Not thread safe for the moment
        // TODO: Use something different than an array of bytes?
        // TODO: Refactor code
        // TODO: Embed entities data into a world class or struct (with ref properties?)?
        // TODO: How to handle open world entities? (Large open-worlds)
        // TODO: Manage entity layouts that the same components with different orders as the same

        private List<EntityComponentLayoutDesc> componentLayouts;
        private Dictionary<uint, List<ComponentDataMemoryChunk>> componentStorage;
        private List<EntityComponentLayout> entityComponentLayouts;
        private byte[] componentDataStorage;
        private int currentDataIndex;

        public EntityManager()
        {
            this.componentLayouts = new List<EntityComponentLayoutDesc>();
            this.componentStorage = new Dictionary<uint, List<ComponentDataMemoryChunk>>();
            this.entityComponentLayouts = new List<EntityComponentLayout>();

            // TODO: Init a basic 50 MB buffer for now
            // TODO: Replace that with a proper memory manager
            this.componentDataStorage = new byte[1024 * 1024 * 50];
            this.currentDataIndex = 0;
        }

        public EntityComponentLayout CreateEntityComponentLayout(params Type[] componentTypes)
        {
            // TODO: Check if the type inherit from IComponentData
            
            var arrayHashCode = componentTypes.GetHashCode();

            for (int i = 0; i < this.componentLayouts.Count; i++)
            {
                if (this.componentLayouts[i].HashCode == arrayHashCode)
                {
                    return new EntityComponentLayout(this.componentLayouts[i].EntityComponentLayoutId);
                }
            }

            var componentLayout = new EntityComponentLayout((uint)this.componentLayouts.Count);
            var componentLayoutDesc = new EntityComponentLayoutDesc(componentLayout.EntityComponentLayoutId, arrayHashCode, componentTypes.Length);
            this.componentLayouts.Add(componentLayoutDesc);
            this.componentStorage.Add(componentLayout.EntityComponentLayoutId, new List<ComponentDataMemoryChunk>());

            for (int i = 0; i < componentLayoutDesc.ComponentCount; i++)
            {
                componentLayoutDesc.ComponentTypes[i] = componentTypes[i].GetHashCode();
                componentLayoutDesc.ComponentOffsets[i] = componentLayoutDesc.Size;
                componentLayoutDesc.ComponentSizes[i] = Marshal.SizeOf(componentTypes[i]);

                componentLayoutDesc.Size += componentLayoutDesc.ComponentSizes[i];
            }

            return componentLayout;
        }

        public Entity CreateEntity(EntityComponentLayout componentLayout)
        {
            // TODO: Init buffer values with component default data
            // TODO: Check for existing entities
            // TODO: Group data in component storage by component layouts so the memory access is linear

            var entity = new Entity((uint)this.entityComponentLayouts.Count + 1);
            this.entityComponentLayouts.Add(componentLayout);

            var dataStorage = this.componentStorage[componentLayout.EntityComponentLayoutId];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var chunkItemSize = sizeof(uint) + componentLayoutDesc.Size;
            var memoryChunk = FindMemoryChunk(dataStorage);

            if (memoryChunk == null)
            {
                memoryChunk = CreateMemoryChunk(dataStorage, componentLayoutDesc, chunkItemSize);
            }

            var chunkIndex = chunkItemSize * memoryChunk.EntityCount;
            MemoryMarshal.Write(memoryChunk!.Storage.Span.Slice(chunkIndex), ref entity.EntityId);
            memoryChunk.EntityCount++;

            return entity;
        }

        public Span<Entity> GetEntities()
        {
            var entities = new Entity[this.entityComponentLayouts.Count];

            for (int i = 0; i < this.entityComponentLayouts.Count; i++)
            {
                entities[i] = new Entity((uint)i + 1);
            }
            
            return entities.AsSpan();
        }

        public void SetComponentData<T>(Entity entity, T component) where T : struct, IComponentData
        {
            // TODO: Make a function for entity indexing
            // TODO: Use Index type?
            var componentLayout = this.entityComponentLayouts[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout.EntityComponentLayoutId];

            var componentOffset = FindComponentOffset(component.GetType().GetHashCode(), componentLayoutDesc);
            var chunkItemSize = sizeof(uint) + componentLayoutDesc.Size;

            // TODO: Throw an exception if entity not found
            for (var i = 0; i < dataStorage.Count; i++)
            {
                var memoryChunk = dataStorage[i];

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var chunkIndex = chunkItemSize * j;
                    var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(chunkIndex));

                    if (entityId == entity.EntityId)
                    {
                        MemoryMarshal.Write(memoryChunk.Storage.Span.Slice(chunkIndex + sizeof(uint) + componentOffset), ref component);
                    }
                }
            }
        }

        public T GetComponentData<T>(Entity entity) where T : struct, IComponentData
        {
            // TODO: Use ref return?
            // TODO: Make a function for entity indexing
            // TODO: Use Index type?
            var componentLayout = this.entityComponentLayouts[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout.EntityComponentLayoutId];

            var componentOffset = FindComponentOffset(typeof(T).GetHashCode(), componentLayoutDesc);
            var chunkItemSize = sizeof(uint) + componentLayoutDesc.Size;

            // TODO: Throw an exception if entity not found
            for (var i = 0; i < dataStorage.Count; i++)
            {
                var memoryChunk = dataStorage[i];

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var chunkIndex = chunkItemSize * j;
                    var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(chunkIndex));

                    if (entityId == entity.EntityId)
                    {
                        return MemoryMarshal.Read<T>(memoryChunk.Storage.Span.Slice(chunkIndex + sizeof(uint) + componentOffset));
                    }
                }
            }

            // TODO: Throw exception
            throw new InvalidOperationException("Entity has no data for the specified component.");
        }

        public bool HasComponent<T>(Entity entity) where T : IComponentData
        {
            var componentLayout = this.entityComponentLayouts[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];

            for (var i = 0; i < componentLayoutDesc.ComponentCount; i++)
            {
                if (componentLayoutDesc.ComponentTypes[i] == typeof(T).GetHashCode())
                {
                    return true;
                }
            }

            return false;
        }

        internal EntitySystemData GetEntitySystemData(Type[] componentTypes)
        {
            // Find compatible component layouts
            var compatibleLayouts = new List<EntityComponentLayoutDesc>();

            // TODO: Replace that with a faster imp
            for (var i = 0; i < this.componentLayouts.Count; i++)
            {
                var componentLayout = this.componentLayouts[i];
                var numberOfMatches = 0;

                for (var j = 0; j < componentLayout.ComponentTypes.Length; j++)
                {
                    var componentType = componentLayout.ComponentTypes[j];
                    
                    for (var k = 0; k < componentTypes.Length; k++)
                    {
                        if (componentType.GetHashCode() == componentTypes[k].GetHashCode())
                        {
                            numberOfMatches++;
                        }
                    }
                }

                if (numberOfMatches == componentTypes.Length)
                {
                    compatibleLayouts.Add(componentLayout);
                }
            }

            // Create list to fill entity system data
            var entityList = new List<Entity>();
            var componentDatas = new Dictionary<int, List<byte>>();

            for (var i = 0; i < componentTypes.Length; i++)
            {
                componentDatas.Add(componentTypes[i].GetHashCode(), new List<byte>());
            }

            // Fill in the data from the component storage
            for (var i = 0; i < compatibleLayouts.Count; i++)
            {
                var compatibleLayout = compatibleLayouts[i];
                var dataStorage = this.componentStorage[compatibleLayout.EntityComponentLayoutId];
                var chunkItemSize = sizeof(uint) + compatibleLayout.Size;

                for (var j = 0; j < dataStorage.Count; j++) 
                {
                    var memoryChunk = dataStorage[j];

                    for (var k = 0; k < memoryChunk.EntityCount; k++)
                    {
                        var chunkIndex = chunkItemSize * k;
                        var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(chunkIndex));

                        entityList.Add(new Entity(entityId));

                        for (var l = 0; l < componentTypes.Length; l++)
                        {
                            // TODO: Big performance issues here (mutliple copies of arrays)
                            var componentType = componentTypes[l];
                            var componentOffset = FindComponentOffset(componentType.GetHashCode(), compatibleLayout);

                            var componentData = memoryChunk.Storage.Span.Slice(chunkIndex + sizeof(uint) + componentOffset, Marshal.SizeOf(componentType));
                            componentDatas[componentType.GetHashCode()].AddRange(componentData.ToArray());
                        }
                    }
                }
            }

            var entitySystemData = new EntitySystemData();
            entitySystemData.entitiesArray = entityList.ToArray();
            entitySystemData.componentsData = new Dictionary<int, byte[]>();

            foreach (var entry in componentDatas)
            {
                entitySystemData.componentsData.Add(entry.Key, entry.Value.ToArray());
            }

            return entitySystemData;
        }

        internal void SetEntitySystemData(EntitySystemData entitySystemData)
        {
            var entityList = new List<Entity>(entitySystemData.entitiesArray);
            var compatibleLayouts = new List<EntityComponentLayoutDesc>();
            var componentTypeHashList = new List<int>();

            foreach (var key in entitySystemData.componentsData.Keys)
            {
                componentTypeHashList.Add(key);
            }

            // Get the layouts of the entities
            for (var i = 0; i < entitySystemData.entitiesArray.Length; i++)
            {
                // TODO: Perf issues
                var layout = this.entityComponentLayouts[(int)entitySystemData.entitiesArray[i].EntityId - 1];
                var layoutDesc = this.componentLayouts[(int)layout.EntityComponentLayoutId];

                if (!compatibleLayouts.Contains(layoutDesc))
                {
                    compatibleLayouts.Add(layoutDesc);
                }
            }

            // Fill in the data from the component storage
            for (var i = 0; i < compatibleLayouts.Count; i++)
            {
                var compatibleLayout = compatibleLayouts[i];
                var dataStorage = this.componentStorage[compatibleLayout.EntityComponentLayoutId];
                var chunkItemSize = sizeof(uint) + compatibleLayout.Size;

                for (var j = 0; j < dataStorage.Count; j++) 
                {
                    var memoryChunk = dataStorage[j];

                    for (var k = 0; k < memoryChunk.EntityCount; k++)
                    {
                        var chunkIndex = chunkItemSize * k;
                        var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(chunkIndex));
                        var entity = new Entity(entityId);
                        var entityIndex = entityList.IndexOf(entity);

                        if (entityIndex != -1)
                        {
                            for (var l = 0; l < componentTypeHashList.Count; l++)
                            {
                                // TODO: Skip readonly components here
                                // TODO: Big performance issues here (mutliple copies of arrays, multiple finds of type info)
                                var componentTypeHash = componentTypeHashList[l];
                                var componentOffset = FindComponentOffset(componentTypeHash, compatibleLayout);
                                var componentSize = FindComponentSize(componentTypeHash, compatibleLayout);

                                var sourceData = new Span<byte>(entitySystemData.componentsData[componentTypeHash], entityIndex * componentSize, componentSize);
                                var destinationData = memoryChunk.Storage.Span.Slice(chunkIndex + sizeof(uint) + componentOffset);

                                sourceData.TryCopyTo(destinationData);
                            }
                        }
                    }
                }
            }
        }

        private static int FindComponentOffset(int componentTypeHash, EntityComponentLayoutDesc componentLayoutDesc)
        {
            var componentIndex = -1;

            for (var i = 0; i < componentLayoutDesc.ComponentCount; i++)
            {
                if (componentLayoutDesc.ComponentTypes[i] == componentTypeHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                // TODO: Throw error
            }

            var componentOffset = componentLayoutDesc.ComponentOffsets[componentIndex];
            return componentOffset;
        }

        private static int FindComponentSize(int componentTypeHash, EntityComponentLayoutDesc componentLayoutDesc)
        {
            var componentIndex = -1;

            for (var i = 0; i < componentLayoutDesc.ComponentCount; i++)
            {
                if (componentLayoutDesc.ComponentTypes[i] == componentTypeHash)
                {
                    componentIndex = i;
                    break;
                }
            }

            if (componentIndex == -1)
            {
                // TODO: Throw error
            }

            var componentSize = componentLayoutDesc.ComponentSizes[componentIndex];
            return componentSize;
        }

        private ComponentDataMemoryChunk CreateMemoryChunk(List<ComponentDataMemoryChunk> dataStorage, EntityComponentLayoutDesc componentLayoutDesc, int chunkItemSize)
        {
            // Store 50 entities per chunk for now
            var entityCount = 50;

            var dataChunkSize = entityCount * chunkItemSize;
            var memoryStorage = this.componentDataStorage.AsMemory(this.currentDataIndex, dataChunkSize);
            this.currentDataIndex += dataChunkSize;

            var memoryChunk = new ComponentDataMemoryChunk(componentLayoutDesc, memoryStorage, chunkItemSize, entityCount);
            dataStorage.Add(memoryChunk);

            return memoryChunk;
        }

        private static ComponentDataMemoryChunk? FindMemoryChunk(List<ComponentDataMemoryChunk> dataStorage)
        {
            ComponentDataMemoryChunk? memoryChunk = null;

            for (int i = 0; i < dataStorage.Count; i++)
            {
                if (dataStorage[i].EntityCount < dataStorage[i].MaxEntityCount)
                {
                    memoryChunk = dataStorage[i];
                }
            }

            return memoryChunk;
        }
    }
}
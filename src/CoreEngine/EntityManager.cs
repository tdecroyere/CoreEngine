using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CoreEngine
{
    public class EntityManager
    {
        // TODO: Switch to memory manager
        // TODO: Not thread safe for the moment
        // TODO: Use something different than an array of bytes?
        // TODO: Embed entities data into a world class or struct (with ref properties?)?
        // TODO: How to handle open world entities? (Large open-worlds, use Grid?)

        private List<ComponentLayoutDesc> componentLayouts;
        private List<ComponentLayout> entityComponentLayouts;
        private Dictionary<ComponentLayout, List<ComponentDataMemoryChunk>> componentStorage;
        private byte[] componentDataStorage;
        private int currentDataIndex;

        public EntityManager()
        {
            this.componentLayouts = new List<ComponentLayoutDesc>();
            this.entityComponentLayouts = new List<ComponentLayout>();
            this.componentStorage = new Dictionary<ComponentLayout, List<ComponentDataMemoryChunk>>();

            // TODO: Init a basic 50 MB buffer for now
            // TODO: Replace that with a proper memory manager
            this.componentDataStorage = new byte[1024 * 1024 * 50];
            this.currentDataIndex = 0;
        }

        public ComponentLayout CreateComponentLayout(params Type[] componentTypes)
        {
            if (componentTypes == null)
            {
                throw new ArgumentNullException(nameof(componentTypes));
            }

            // TODO: Check if the type inherit from IComponentData
            
            var arrayHashCode = ComputeComponentLayoutHashCodeAndSort(ref componentTypes);

            for (int i = 0; i < this.componentLayouts.Count; i++)
            {
                if (this.componentLayouts[i].HashCode == arrayHashCode)
                {
                    return this.componentLayouts[i].ComponentLayout;
                }
            }

            var componentLayout = new ComponentLayout((uint)this.componentLayouts.Count);
            var componentLayoutDesc = new ComponentLayoutDesc(componentLayout, arrayHashCode, componentTypes);
            this.componentLayouts.Add(componentLayoutDesc);
            this.componentStorage.Add(componentLayout, new List<ComponentDataMemoryChunk>());

            return componentLayout;
        }

        public Entity CreateEntity(ComponentLayout componentLayout)
        {
            // TODO: Check for existing entities
            // TODO: Group data in component storage by component layouts so the memory access is linear

            var entity = new Entity((uint)this.entityComponentLayouts.Count + 1);
            this.entityComponentLayouts.Add(componentLayout);

            var dataStorage = this.componentStorage[componentLayout];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var chunkItemSize = ComputeChunkItemSize(componentLayoutDesc);
            var memoryChunk = FindMemoryChunk(dataStorage);

            if (memoryChunk == null)
            {
                memoryChunk = CreateMemoryChunk(dataStorage, componentLayoutDesc, chunkItemSize);
            }

            var chunkIndex = sizeof(uint) * memoryChunk.EntityCount;
            var entityId = entity.EntityId;
            MemoryMarshal.Write(memoryChunk!.Storage.Span.Slice(chunkIndex), ref entityId);
            memoryChunk.EntityCount++;

            for (var i = 0; i < componentLayoutDesc.ComponentDefaultValues.Length; i++)
            {
                var componentDefaultValues = componentLayoutDesc.ComponentDefaultValues[i];
                SetComponentData(entity, componentDefaultValues.GetType(), componentDefaultValues);
            }

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
            var componentLayout = this.entityComponentLayouts[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout];

            var componentOffset = componentLayoutDesc.FindComponentOffset(typeof(T).GetHashCode());
            var componentSize = componentLayoutDesc.FindComponentSize(typeof(T).GetHashCode());

            // TODO: Throw an exception if entity not found
            for (var i = 0; i < dataStorage.Count; i++)
            {
                var memoryChunk = dataStorage[i];

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var entityOffset = sizeof(uint) * j;
                    var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(entityOffset));

                    if (entityId == entity.EntityId)
                    {
                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset, componentSize, j);
                        MemoryMarshal.Write(memoryChunk.Storage.Span.Slice(storageComponentOffet), ref component);
                        break;
                    }
                }
            }
        }

        internal void SetComponentData(Entity entity, Type componentType, IComponentData componentData)
        {
            var componentLayout = this.entityComponentLayouts[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout];

            var componentOffset = componentLayoutDesc.FindComponentOffset(componentType.GetHashCode());
            var componentSize = componentLayoutDesc.FindComponentSize(componentType.GetHashCode());

            // TODO: Throw an exception if entity not found
            for (var i = 0; i < dataStorage.Count; i++)
            {
                var memoryChunk = dataStorage[i];

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var entityOffset = sizeof(uint) * j;
                    var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(entityOffset));

                    if (entityId == entity.EntityId)
                    {
                        var size = Marshal.SizeOf(componentData);
                        // Both managed and unmanaged buffers required.
                        var bytes = new byte[size];
                        var ptr = Marshal.AllocHGlobal(size);
                        // Copy object byte-to-byte to unmanaged memory.
                        Marshal.StructureToPtr(componentData, ptr, false);
                        // Copy data from unmanaged memory to managed buffer.
                        Marshal.Copy(ptr, bytes, 0, size);
                        // Release unmanaged memory.
                        Marshal.FreeHGlobal(ptr);

                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset, componentSize, j);
                        bytes.CopyTo(memoryChunk.Storage.Span.Slice(storageComponentOffet));
                        break;
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
            var dataStorage = this.componentStorage[componentLayout];

            var componentOffset = componentLayoutDesc.FindComponentOffset(typeof(T).GetHashCode());
            var componentSize = componentLayoutDesc.FindComponentSize(typeof(T).GetHashCode());

            // TODO: Throw an exception if entity not found
            for (var i = 0; i < dataStorage.Count; i++)
            {
                var memoryChunk = dataStorage[i];

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    var entityOffset = sizeof(uint) * j;
                    var entityId = MemoryMarshal.Read<uint>(memoryChunk.Storage.Span.Slice(entityOffset));

                    if (entityId == entity.EntityId)
                    {
                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset, componentSize, j);
                        return MemoryMarshal.Read<T>(memoryChunk.Storage.Span.Slice(storageComponentOffet));
                    }
                }
            }

            throw new InvalidOperationException("Entity has no data for the specified component.");
        }

        private static int ComputeDataChunkComponentOffset(ComponentDataMemoryChunk memoryChunk, int componentOffset, int componentSize, int entityLocalIndex)
        {
            return memoryChunk.MaxEntityCount * sizeof(uint) + memoryChunk.MaxEntityCount * componentOffset + entityLocalIndex * componentSize;
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
            var componentSizes = new int[componentTypes.Length];
            var componentHashCodes = new int[componentTypes.Length];

            for (var i = 0; i < componentTypes.Length; i++)
            {
                componentHashCodes[i] = componentTypes[i].GetHashCode();
            }

            // TODO: Re-use entity system data?
            var entitySystemData = new EntitySystemData();

            // Find compatible component layouts
            var compatibleLayouts = new List<ComponentLayoutDesc>();

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
                        if (componentType == componentHashCodes[k])
                        {
                            componentSizes[k] = componentLayout.ComponentSizes[j];
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
            for (var i = 0; i < componentTypes.Length; i++)
            {
                entitySystemData.ComponentsData.Add(componentHashCodes[i], new EntitySystemArray<byte>(componentSizes[i]));
            }

            // Fill in the data from the component storage
            for (var i = 0; i < compatibleLayouts.Count; i++)
            {
                var compatibleLayout = compatibleLayouts[i];
                var dataStorage = this.componentStorage[compatibleLayout.ComponentLayout];

                for (var j = 0; j < dataStorage.Count; j++) 
                {
                    var memoryChunk = dataStorage[j];

                    var entityListMemory = memoryChunk.Storage.Slice(0, memoryChunk.EntityCount * Marshal.SizeOf<Entity>());
                    entitySystemData.EntityArray.AddMemorySlot(entityListMemory, memoryChunk.EntityCount);

                    for (var k = 0; k < componentTypes.Length; k++)
                    {
                        var componentHashCode = componentHashCodes[k];
                        var componentOffset = compatibleLayout.FindComponentOffset(componentHashCode);
                        var componentSize = compatibleLayout.FindComponentSize(componentHashCode);

                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset, componentSize, 0);
                        var componentDataListMemory = memoryChunk.Storage.Slice(storageComponentOffet, memoryChunk.EntityCount * componentSize);
                        
                        entitySystemData.ComponentsData[componentHashCode].AddMemorySlot(componentDataListMemory, memoryChunk.EntityCount);
                    }
                }
            }

            return entitySystemData;
        }

        private static int ComputeComponentLayoutHashCodeAndSort(ref Type[] componentTypes)
        {
            var result = 0;
            var sortedList = new SortedList<int, Type>();

            for (var i = 0; i < componentTypes.Length; i++)
            {
                var typeHashCode = componentTypes[i].GetHashCode();
                sortedList.Add(typeHashCode, componentTypes[i]);
                result |= typeHashCode;
            }

            sortedList.Values.CopyTo(componentTypes, 0);

            return result;
        }

        private static int ComputeChunkItemSize(ComponentLayoutDesc componentLayoutDesc)
        {
            return sizeof(uint) + componentLayoutDesc.Size;
        }

        private ComponentDataMemoryChunk CreateMemoryChunk(List<ComponentDataMemoryChunk> dataStorage, ComponentLayoutDesc componentLayoutDesc, int chunkItemSize)
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
            for (var i = 0; i < dataStorage.Count; i++)
            {
                if (dataStorage[i].EntityCount < dataStorage[i].MaxEntityCount)
                {
                    return dataStorage[i];
                }
            }

            return null;
        }
    }
}
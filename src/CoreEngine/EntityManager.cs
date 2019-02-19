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
            var arrayHashCode = componentTypes.GetHashCode();

            for (int i = 0; i < this.componentLayouts.Count; i++)
            {
                if (this.componentLayouts[i].HashCode == arrayHashCode)
                {
                    return new EntityComponentLayout(this.componentLayouts[i].EntityComponentLayoutId);
                }
            }

            var componentLayout = new EntityComponentLayout((uint)this.componentLayouts.Count);
            
            var componentLayoutDesc = new EntityComponentLayoutDesc();
            this.componentLayouts.Add(componentLayoutDesc);
            this.componentStorage.Add(componentLayout.EntityComponentLayoutId, new List<ComponentDataMemoryChunk>());

            componentLayoutDesc.EntityComponentLayoutId = componentLayout.EntityComponentLayoutId;
            componentLayoutDesc.HashCode = arrayHashCode;
            componentLayoutDesc.ComponentCount = componentTypes.Length;
            componentLayoutDesc.ComponentTypes = new int[componentTypes.Length];
            componentLayoutDesc.ComponentOffsets = new int[componentTypes.Length];
            componentLayoutDesc.ComponentSizes = new int[componentTypes.Length];

            for (int i = 0; i < componentTypes.Length; i++)
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
            ComponentDataMemoryChunk? memoryChunk = null;

            for (int i = 0; i < dataStorage.Count; i++)
            {
                if (dataStorage[i].EntityCount < dataStorage[i].MaxEntityCount)
                {
                    memoryChunk = dataStorage[i];
                }
            }

            if (memoryChunk == null)
            {
                // Store 50 entities per chunk for now
                var entityCount = 50;
                
                var dataChunkSize = entityCount * chunkItemSize;
                var memoryStorage = this.componentDataStorage.AsMemory(this.currentDataIndex, dataChunkSize);
                this.currentDataIndex += dataChunkSize;

                memoryChunk = new ComponentDataMemoryChunk(componentLayoutDesc, memoryStorage, chunkItemSize, entityCount);
                dataStorage.Add(memoryChunk);
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

            var componentOffset = FindComponentOffset(component.GetType(), componentLayoutDesc);
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
            // TODO: Make a function for entity indexing
            // TODO: Use Index type?
            var componentLayout = this.entityComponentLayouts[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout.EntityComponentLayoutId];

            var componentOffset = FindComponentOffset(typeof(T), componentLayoutDesc);
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

        private static int FindComponentOffset(Type componentType, EntityComponentLayoutDesc componentLayoutDesc)
        {
            var componentIndex = -1;

            for (var i = 0; i < componentLayoutDesc.ComponentCount; i++)
            {
                if (componentLayoutDesc.ComponentTypes[i] == componentType.GetHashCode())
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
    }
}
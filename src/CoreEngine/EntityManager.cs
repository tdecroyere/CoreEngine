using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreEngine.Collections;

namespace CoreEngine
{
    public class EntityManager
    {
        // TODO: Switch to memory manager
        // TODO: Don't use list index for entity id because otherwise it is impossible to implement delete
        // TODO: Not thread safe for the moment
        // TODO: Use something different than an array of bytes?
        // TODO: How to handle open world entities? (Large open-worlds, use Grid?)

        private readonly List<ComponentLayout> componentLayouts;
        private readonly List<ComponentLayout> entityComponentLayoutsMapping;
        private readonly Dictionary<ComponentHash, List<ComponentDataMemoryChunk>> componentStorage;
        private readonly byte[] componentDataStorage;
        private int currentDataIndex;

        /// <summary>
        /// Constructs a new <c>EntityManager</c> object.
        /// </summary>
        public EntityManager()
        {
            this.componentLayouts = new List<ComponentLayout>();
            this.entityComponentLayoutsMapping = new List<ComponentLayout>();
            this.componentStorage = new Dictionary<ComponentHash, List<ComponentDataMemoryChunk>>();

            // TODO: Init a basic 50 MB buffer for now
            // TODO: Replace that with a proper memory manager
            this.componentDataStorage = new byte[1024 * 1024 * 50];
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
            var componentLayout = new ComponentLayout((uint)this.componentLayouts.Count);
            this.componentLayouts.Add(componentLayout);

            return componentLayout;
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
                this.componentStorage.Add(componentLayout.LayoutHash, new List<ComponentDataMemoryChunk>());
            }

            var entity = new Entity((uint)this.entityComponentLayoutsMapping.Count + 1);
            this.entityComponentLayoutsMapping.Add(componentLayout);

            var dataStorage = this.componentStorage[componentLayout.LayoutHash];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var chunkItemSize = ComputeChunkItemSize(componentLayoutDesc);
            var memoryChunk = FindMemoryChunk(dataStorage);

            if (memoryChunk == null)
            {
                memoryChunk = CreateMemoryChunk(dataStorage, componentLayoutDesc, chunkItemSize);
            }

            var chunkIndex = sizeof(uint) * memoryChunk.EntityCount;
            var entityId = entity.EntityId;
            MemoryMarshal.Write(memoryChunk!.Storage.Span[chunkIndex..], ref entityId);
            memoryChunk.EntityCount++;

            for (var i = 0; i < componentLayoutDesc.ComponentDefaultValues.Count; i++)
            {
                var componentHash = componentLayoutDesc.ComponentHashs[i];
                var componentDefaultValues = componentLayoutDesc.ComponentDefaultValues[i];

                if (componentDefaultValues != null)
                {
                    SetComponentData(entity, componentHash, componentDefaultValues.Value.Span);
                }
            }

            return entity;
        }

        public ReadOnlySpan<Entity> GetEntities()
        {
            var entities = new Entity[this.entityComponentLayoutsMapping.Count];

            for (int i = 0; i < this.entityComponentLayoutsMapping.Count; i++)
            {
                entities[i] = new Entity((uint)i + 1);
            }
            
            return entities.AsSpan();
        }

        public ReadOnlySpan<Entity> GetEntitiesByComponentType<T>() where T: struct, IComponentData
        {
            var entities = new List<Entity>();

            for (int i = 0; i < this.entityComponentLayoutsMapping.Count; i++)
            {
                var entity = new Entity((uint)i + 1);
                if (this.HasComponent<T>(entity))
                {
                    entities.Add(entity);
                }
            }
            
            return entities.ToArray();
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
            var componentLayout = this.entityComponentLayoutsMapping[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout.LayoutHash];

            var componentOffset = componentLayoutDesc.FindComponentOffset(componentHash);

            if (componentOffset == null)
            {
                throw new ArgumentException("Component type is not part of the entity component layout.", nameof(componentHash));
            }

            var componentSize = componentLayoutDesc.FindComponentSize(componentHash);

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
                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset.Value, componentSize, j);
                        data.CopyTo(memoryChunk.Storage.Span.Slice(storageComponentOffet));
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
            var componentHash = new T().GetComponentHash();
            var componentLayout = this.entityComponentLayoutsMapping[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];
            var dataStorage = this.componentStorage[componentLayout.LayoutHash];

            var componentOffset = componentLayoutDesc.FindComponentOffset(componentHash);

            if (componentOffset == null)
            {
                throw new ArgumentException("Component type it part of the entity component layout.", nameof(T));
            }

            var componentSize = componentLayoutDesc.FindComponentSize(componentHash);

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
                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset.Value, componentSize, j);
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

        public bool HasComponent<T>(Entity entity) where T : struct, IComponentData
        {
            var componentHash = new T().GetComponentHash();
            var componentLayout = this.entityComponentLayoutsMapping[(int)entity.EntityId - 1];
            var componentLayoutDesc = this.componentLayouts[(int)componentLayout.EntityComponentLayoutId];

            for (var i = 0; i < componentLayoutDesc.ComponentCount; i++)
            {
                if (componentLayoutDesc.ComponentHashs[i] == componentHash)
                {
                    return true;
                }
            }

            return false;
        }

        internal EntitySystemData GetEntitySystemData(ComponentHash[] componentHashCodes)
        {
            var componentSizes = new int[componentHashCodes.Length];

            // TODO: Re-use entity system data?
            var entitySystemData = new EntitySystemData();

            // Find compatible component layouts
            var compatibleLayouts = new List<ComponentLayout>();

            // TODO: Replace that with a faster imp
            for (var i = 0; i < this.componentLayouts.Count; i++)
            {
                var componentLayout = this.componentLayouts[i];
                var numberOfMatches = 0;

                for (var j = 0; j < componentLayout.ComponentHashs.Count; j++)
                {
                    var componentType = componentLayout.ComponentHashs[j];
                    
                    for (var k = 0; k < componentHashCodes.Length; k++)
                    {
                        if (componentType == componentHashCodes[k])
                        {
                            componentSizes[k] = componentLayout.ComponentSizes[j];
                            numberOfMatches++;
                        }
                    }
                }

                if (numberOfMatches == componentHashCodes.Length)
                {
                    compatibleLayouts.Add(componentLayout);
                }
            }

            // Create list to fill entity system data
            for (var i = 0; i < componentHashCodes.Length; i++)
            {
                entitySystemData.ComponentsData.Add(componentHashCodes[i], new EntitySystemArray<byte>(componentSizes[i]));
            }

            // Fill in the data from the component storage
            for (var i = 0; i < compatibleLayouts.Count; i++)
            {
                var compatibleLayout = compatibleLayouts[i];

                if (!this.componentStorage.ContainsKey(compatibleLayout.LayoutHash))
                {
                    continue;
                }
                
                var dataStorage = this.componentStorage[compatibleLayout.LayoutHash];

                for (var j = 0; j < dataStorage.Count; j++) 
                {
                    var memoryChunk = dataStorage[j];

                    var entityListMemory = memoryChunk.Storage.Slice(0, memoryChunk.EntityCount * Marshal.SizeOf<Entity>());
                    entitySystemData.EntityArray.AddMemorySlot(entityListMemory, memoryChunk.EntityCount);

                    for (var k = 0; k < componentHashCodes.Length; k++)
                    {
                        var componentHashCode = componentHashCodes[k];
                        var componentOffset = compatibleLayout.FindComponentOffset(componentHashCode);
                        var componentSize = compatibleLayout.FindComponentSize(componentHashCode);

                        if (componentOffset == null)
                        {
                            continue;
                        }

                        var storageComponentOffet = ComputeDataChunkComponentOffset(memoryChunk, componentOffset.Value, componentSize, 0);
                        var componentDataListMemory = memoryChunk.Storage.Slice(storageComponentOffet, memoryChunk.EntityCount * componentSize);
                        
                        entitySystemData.ComponentsData[componentHashCode].AddMemorySlot(componentDataListMemory, memoryChunk.EntityCount);
                    }
                }
            }

            return entitySystemData;
        }

        private static int ComputeChunkItemSize(ComponentLayout componentLayoutDesc)
        {
            return sizeof(uint) + componentLayoutDesc.Size;
        }

        private ComponentDataMemoryChunk CreateMemoryChunk(List<ComponentDataMemoryChunk> dataStorage, ComponentLayout componentLayoutDesc, int chunkItemSize)
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

        private static void RegisterComponent<T>(ComponentLayout componentLayout) where T: struct, IComponentData 
        {
            var component = new T();
            component.SetDefaultValues();
            var component1Size = Marshal.SizeOf(component);

            var component1InitialData = new byte[component1Size];
            MemoryMarshal.Write(component1InitialData, ref component);

            componentLayout.RegisterComponent(component.GetComponentHash(), component1Size, component1InitialData);
        }
    }
}
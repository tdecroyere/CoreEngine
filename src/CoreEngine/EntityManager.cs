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

        private List<EntityComponentLayoutDesc> componentLayouts;
        private Dictionary<uint, List<ComponentDataMemoryChunk>> componentStorage;
        private List<EntityComponentLayout> entityComponentLayouts;

        public EntityManager()
        {
            this.componentLayouts = new List<EntityComponentLayoutDesc>();
            this.componentStorage = new Dictionary<uint, List<ComponentDataMemoryChunk>>();
            this.entityComponentLayouts = new List<EntityComponentLayout>();
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
                componentLayoutDesc.ComponentTypes[i] = componentTypes.GetHashCode();
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

            var entity = new Entity((uint)this.entityComponentLayouts.Count);
            this.entityComponentLayouts.Add(componentLayout);

            return entity;
        }

        public Span<Entity> GetEntities()
        {
            var entities = new Entity[this.entityComponentLayouts.Count];

            for (int i = 0; i < this.entityComponentLayouts.Count; i++)
            {
                entities[i] = new Entity((uint)i);
            }
            
            return entities.AsSpan();
        }

        public void SetComponentData<T>(Entity entity, T component) where T : IComponentData
        {
            throw new NotImplementedException();
        }

        public T GetComponentData<T>(Entity entity) where T : IComponentData
        {
            throw new NotImplementedException();
        }

        public bool HasComponent<T>(Entity entity) where T : IComponentData
        {
            throw new NotImplementedException();
        }
    }
}
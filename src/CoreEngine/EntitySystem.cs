namespace CoreEngine
{
    public abstract class EntitySystem
    {
        internal EntitySystemData entitySystemData = new EntitySystemData();

        public abstract EntitySystemDefinition BuildDefinition();
        
        public virtual void Setup(EntityManager entityManager)
        {
        }

        public virtual void Process(EntityManager entityManager, float deltaTime)
        {
        }

        // TODO: Move that code to the query class?
        protected ReadOnlyMemory<ComponentStorageMemoryChunk> GetMemoryChunks()
        {
            if (this.entitySystemData == null || this.entitySystemData.MemoryChunks == null)
            {
                return new ReadOnlyMemory<ComponentStorageMemoryChunk>();
            }

            return this.entitySystemData.MemoryChunks.Value;
        }
        
        protected Span<T> GetComponentArray<T>(ComponentStorageMemoryChunk memoryChunk) where T : struct, IComponentData
        {
            var componentHash = new T().GetComponentHash();

            var componentOffset = memoryChunk.ComponentLayout.FindComponentOffset(componentHash);
            var componentSize = memoryChunk.ComponentLayout.FindComponentSizeInBytes(componentHash);
            var componentMemoryOffset = EntityManager.ComputeDataChunkComponentOffset(memoryChunk, componentOffset!.Value, componentSize, 0);

            return MemoryMarshal.Cast<byte, T>(memoryChunk.Storage.Span.Slice(componentMemoryOffset, memoryChunk.EntityCount * componentSize));
        }

        protected Span<Entity> GetEntityArray(ComponentStorageMemoryChunk memoryChunk)
        {
            return MemoryMarshal.Cast<byte, Entity>(memoryChunk.Storage.Span.Slice(0, memoryChunk.EntityCount * Marshal.SizeOf<Entity>()));
        }
    }
}
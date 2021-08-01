namespace CoreEngine
{
    internal class EntitySystemData
    {
        public EntitySystemData()
        {
            // TODO: Find a way to properly allocate memory here in the queries
            this.WorkingMemoryChunks = new ComponentStorageMemoryChunk[10000];
        }

        internal ComponentStorageMemoryChunk[] WorkingMemoryChunks { get; set; }
        public ReadOnlyMemory<ComponentStorageMemoryChunk>? MemoryChunks { get; set; }
    }
}
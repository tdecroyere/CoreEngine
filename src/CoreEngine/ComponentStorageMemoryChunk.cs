namespace CoreEngine
{
    public class ComponentStorageMemoryChunk
    {
        public ComponentStorageMemoryChunk(ComponentLayout componentLayout, Memory<byte> storage, int chunkItemSize, int maxEntityCount)
        {
            this.ComponentLayout = componentLayout;
            this.Storage = storage;
            this.EntityCount = 0;
            this.MaxEntityCount = maxEntityCount;
            this.ChunkItemSize = chunkItemSize;
        }

        public ComponentLayout ComponentLayout; // TODO: Move that
        public Memory<byte> Storage;
        public int EntityCount;
        public int ChunkItemSize;
        public int MaxEntityCount;
    }
}
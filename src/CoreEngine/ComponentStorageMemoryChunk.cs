using System;

namespace CoreEngine
{
    internal class ComponentStorageMemoryChunk
    {
        public ComponentStorageMemoryChunk(Memory<byte> storage, int chunkItemSize, int maxEntityCount)
        {
            this.Storage = storage;
            this.EntityCount = 0;
            this.MaxEntityCount = maxEntityCount;
            this.ChunkItemSize = chunkItemSize;
        }

        public Memory<byte> Storage;
        public int EntityCount;
        public int ChunkItemSize;
        public int MaxEntityCount;
    }
}
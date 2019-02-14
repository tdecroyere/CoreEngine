using System;

namespace CoreEngine
{
    internal class ComponentDataMemoryChunk
    {
        public ComponentDataMemoryChunk(EntityComponentLayoutDesc componentLayout, Memory<byte> storage, int chunkItemSize, int maxEntityCount)
        {
            this.ComponentLayout = componentLayout;
            this.Storage = storage;
            this.EntityCount = 0;
            this.MaxEntityCount = maxEntityCount;
            this.ChunkItemSize = chunkItemSize;
        }

        public EntityComponentLayoutDesc ComponentLayout;
        public Memory<byte> Storage;
        public int EntityCount;
        public int ChunkItemSize;
        public int MaxEntityCount;
    }
}
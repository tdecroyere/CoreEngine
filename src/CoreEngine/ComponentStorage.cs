namespace CoreEngine
{
    internal class ComponentStorage
    {
        public ComponentStorage(ComponentLayout componentLayout)
        {
            this.ComponentLayout = componentLayout;
            this.MemoryChunks = new List<ComponentStorageMemoryChunk>();
        }

        public ComponentLayout ComponentLayout { get; }
        public IList<ComponentStorageMemoryChunk> MemoryChunks { get; }
    }
}
namespace CoreEngine;

public readonly record struct EntityInfo
{
    public EntityInfo(Entity entity, ComponentLayout componentLayout, ComponentStorageMemoryChunk memoryChunk, int localStorageIndex)
    {
        this.Entity = entity;
        this.ComponentLayout = componentLayout;
        this.MemoryChunk = memoryChunk;
        this.LocalStorageIndex = localStorageIndex;
    }

    public Entity Entity { get; init; }
    public ComponentLayout ComponentLayout { get; init; }
    public ComponentStorageMemoryChunk MemoryChunk { get; init; }
    public int LocalStorageIndex { get; init; }
}
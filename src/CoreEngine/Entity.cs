namespace CoreEngine;

public readonly record struct Entity : IEquatable<Entity>
{
    public Entity(uint id)
    {
        this.EntityId = id;
    }

    public readonly uint EntityId { get; init; }

    public bool Equals(Entity other)
    {
        return this.EntityId == other.EntityId;
    }

    public override int GetHashCode()
    {
        return this.EntityId.GetHashCode();
    }
}
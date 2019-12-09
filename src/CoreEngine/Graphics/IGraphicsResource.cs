namespace CoreEngine.Graphics
{
    public interface IGraphicsResource
    {
        uint Id { get; }
        uint SystemId { get; }
        uint? SystemId2 { get; }
    }
}
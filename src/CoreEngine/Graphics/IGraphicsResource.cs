namespace CoreEngine.Graphics
{
    public interface IGraphicsResource
    {
        uint GraphicsResourceId { get; }
        uint GraphicsResourceSystemId { get; }
        uint? GraphicsResourceSystemId2 { get; }
        bool IsStatic { get; }
        GraphicsResourceType ResourceType { get; }
        string Label { get; }
    }
}
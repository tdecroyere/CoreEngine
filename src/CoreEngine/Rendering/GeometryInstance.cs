using CoreEngine.Graphics;

namespace CoreEngine.Rendering
{
    // TODO: Rename that struct?
    public readonly struct GeometryInstance
    {
        public GeometryInstance(GeometryPacket geometryPacket, Material? material, int startIndex, int indexCount, int vertexCount, BoundingBox boundingBox = new BoundingBox())
        {
            this.GeometryPacket = geometryPacket;
            this.Material = material;
            this.StartIndex = startIndex;
            this.IndexCount = indexCount;
            this.VertexCount = vertexCount;
            this.BoundingBox = boundingBox;
        }

        public GeometryPacket GeometryPacket { get; }
        public Material? Material { get; }
        public int StartIndex { get; }
        public int IndexCount { get; }
        public int VertexCount { get; }
        public BoundingBox BoundingBox { get; }
    }
}
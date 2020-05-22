using CoreEngine.Graphics;

namespace CoreEngine.Rendering
{
    // TODO: Rename that struct?
    public readonly struct GeometryInstance
    {
        public GeometryInstance(GeometryPacket geometryPacket, Material? material, int startIndex, int indexCount, BoundingBox boundingBox = new BoundingBox(), PrimitiveType primitiveType = PrimitiveType.Triangle)
        {
            this.GeometryPacket = geometryPacket;
            this.Material = material;
            this.StartIndex = startIndex;
            this.IndexCount = indexCount;
            this.PrimitiveType = primitiveType;
            this.BoundingBox = boundingBox;
        }

        public PrimitiveType PrimitiveType { get; }
        public GeometryPacket GeometryPacket { get; }
        public Material? Material { get; }
        public int StartIndex { get; }
        public int IndexCount { get; }
        public BoundingBox BoundingBox { get; }
    }
}
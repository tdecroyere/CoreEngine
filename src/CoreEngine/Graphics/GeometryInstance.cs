using System;

namespace CoreEngine.Graphics
{
    // TODO: Rename that struct?
    public readonly struct GeometryInstance
    {
        public GeometryInstance(GeometryPacket geometryPacket, Material? material, int startIndex, int indexCount, BoundingBox boundingBox = new BoundingBox(), GeometryPrimitiveType primitiveType = GeometryPrimitiveType.Triangle)
        {
            this.GeometryPacket = geometryPacket;
            this.Material = material;
            this.StartIndex = startIndex;
            this.IndexCount = indexCount;
            this.PrimitiveType = primitiveType;
            this.BoundingBox = boundingBox;
        }

        public GeometryPrimitiveType PrimitiveType { get; }
        public GeometryPacket GeometryPacket { get; }
        public Material? Material { get; }
        public int StartIndex { get; }
        public int IndexCount { get; }
        public BoundingBox BoundingBox { get; }
    }
}
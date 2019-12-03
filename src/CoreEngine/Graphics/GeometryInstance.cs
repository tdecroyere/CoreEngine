using System;

namespace CoreEngine.Graphics
{
    public readonly struct GeometryInstance
    {
        public GeometryInstance(GeometryPacket geometryPacket, Material material, uint startIndex, uint indexCount, BoundingBox boundingBox = new BoundingBox(), GeometryPrimitiveType primitiveType = GeometryPrimitiveType.Triangle)
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
        public Material Material { get; }
        public uint StartIndex { get; }
        public uint IndexCount { get; }
        public BoundingBox BoundingBox { get; }
    }
}
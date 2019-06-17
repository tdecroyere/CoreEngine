using System;

namespace CoreEngine.Graphics
{
    public readonly struct GeometryInstance
    {
        public GeometryInstance(GeometryPacket geometryPacket, Material material, uint startIndex, uint indexCount)
        {
            this.GeometryPacket = geometryPacket;
            this.Material = material;
            this.StartIndex = startIndex;
            this.IndexCount = indexCount;
        }

        public GeometryPacket GeometryPacket { get; }
        public Material Material { get; }
        public uint StartIndex { get; }
        public uint IndexCount { get; }
    }
}
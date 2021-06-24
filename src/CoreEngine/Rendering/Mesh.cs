using System;
using System.Collections.Generic;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class Mesh : Resource
    {
        public Mesh() : base(0, string.Empty)
        {
        }

        internal Mesh(uint resourceId, string path) : base(resourceId, path)
        {

        }

        public BoundingBox BoundingBox { get; set; }
        public uint MeshletCount { get; set; }
        public uint TriangleCount { get; set; }

        public ulong VerticesOffset { get; set; }
        public ulong VerticesSizeInBytes { get; set; }
        public ulong VertexIndicesOffset { get; set; }
        public ulong VertexIndicesSizeInBytes { get; set; }
        public ulong TriangleIndicesOffset { get; set; }
        public ulong TriangleIndicesSizeInBytes { get; set; }
        public ulong MeshletsOffset { get; set; }
        public ulong MeshletsSizeInBytes { get; set; }
    }
}
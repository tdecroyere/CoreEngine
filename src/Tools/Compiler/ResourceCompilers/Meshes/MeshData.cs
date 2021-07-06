using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Meshes
{
    public readonly struct MeshVertex : IEquatable<MeshVertex>
    {
        public MeshVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinates)
        {
            this.Position = position;
            this.Normal = normal;
            this.TextureCoordinates = textureCoordinates;
        }

        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public Vector2 TextureCoordinates { get; }

        public override int GetHashCode()
        {
            return this.Position.GetHashCode() ^
                   this.Normal.GetHashCode() ^
                   this.TextureCoordinates.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is MeshVertex vertex && this == vertex;
        }

        public bool Equals(MeshVertex other)
        {
            return this == other;
        }

        public static bool operator ==(MeshVertex vertex1, MeshVertex vertex2)
        {
            return vertex1.Position == vertex2.Position && vertex1.Normal == vertex2.Normal && vertex1.TextureCoordinates == vertex2.TextureCoordinates;
        }

        public static bool operator !=(MeshVertex layout1, MeshVertex layout2)
        {
            return !(layout1 == layout2);
        }
    }

    public readonly struct Meshlet
    {
        public Meshlet(int packedCone, Vector4 boundingSphere, uint vertexCount, uint vertexOffset, uint triangleCount, uint triangleOffset)
        {
            this.PackedCone = packedCone;
            this.BoundingSphere = boundingSphere;
            this.VertexCount = vertexCount;
            this.VertexOffset = vertexOffset;
            this.TriangleCount = triangleCount;
            this.TriangleOffset = triangleOffset;
        }

        public readonly int PackedCone { get; }
        public readonly Vector4 BoundingSphere { get; }
        public readonly uint VertexCount { get; }
        public readonly uint VertexOffset { get; }
        public readonly uint TriangleCount { get; }
        public readonly uint TriangleOffset { get; }
    }

    // TODO: Optimize index size
    public readonly struct MeshData
    {
        public MeshData(BoundingBox boundingBox, ReadOnlyMemory<MeshVertex> vertices, ReadOnlyMemory<uint> vertexIndices, ReadOnlyMemory<uint> triangleIndices, ReadOnlyMemory<Meshlet> meshlets)
        {
            this.BoundingBox = boundingBox;
            this.Vertices = vertices;
            this.VertexIndices = vertexIndices;
            this.TriangleIndices = triangleIndices;
            this.Meshlets = meshlets;
        }

        public readonly BoundingBox BoundingBox { get; }
        public readonly ReadOnlyMemory<MeshVertex> Vertices { get; }
        public readonly ReadOnlyMemory<uint> VertexIndices { get; }
        public readonly ReadOnlyMemory<uint> TriangleIndices { get; } // Local meshlet indices
        public readonly ReadOnlyMemory<Meshlet> Meshlets { get; }
    }

    public class ImportMeshData
    {
        public List<MeshVertex> Vertices { get; } = new List<MeshVertex>();
        public List<uint> Indices { get; } = new List<uint>();
    }

    public class MeshSubObject
    {
        public MeshSubObject()
        {
            this.BoundingBox = new BoundingBox();
            this.MaterialPath = string.Empty;
        }

        public uint StartIndex { get; set; }
        public uint IndexCount { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public string MaterialPath { get; set; }
    }
}
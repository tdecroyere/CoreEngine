using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Meshes
{
    public class MeshResourceDataCompiler : ResourceCompiler
    {
        public override string Name
        {
            get
            {
                return "Mesh Resource Data Compiler";
            }
        }

        public override IList<string> SupportedSourceExtensions
        {
            get
            {
                return new string[] { ".obj" };
            }
        }

        public override string DestinationExtension
        {
            get
            {
                return ".mesh";
            }
        }

        public override async Task<ReadOnlyMemory<ResourceCompilerOutput>> CompileAsync(ReadOnlyMemory<byte> sourceData, CompilerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }


            // TODO: Add extension to the parameters in order to do a factory here base on the file extension

            MeshDataReader? meshDataReader = null;

            if (Path.GetExtension(context.SourceFilename) == ".obj")
            {
                meshDataReader = new ObjMeshDataReader();
            }

            // else if (Path.GetExtension(context.SourceFilename) == ".fbx")
            // {
            //     meshDataReader = new FbxMeshDataReader(Path.Combine(context.InputDirectory, context.SourceFilename));
            // }

            if (meshDataReader != null)
            {
                var importMeshData = await meshDataReader.ReadAsync(sourceData);

                if (importMeshData != null)
                {
                    var meshData = BuildMeshData(importMeshData);

                    var resourceData = WriteMeshData(meshData);
                    var resourceEntry = new ResourceCompilerOutput($"{Path.GetFileNameWithoutExtension(context.SourceFilename)}{this.DestinationExtension}", resourceData);

                    return new ReadOnlyMemory<ResourceCompilerOutput>(new ResourceCompilerOutput[] { resourceEntry });
                }
            }

            return null;
        }

        private static MeshData BuildMeshData(ImportMeshData importMeshData)
        {
            // TODO: Optimize memory accesses

            const uint maxVertexCount = 64;
            const uint maxTriangleCount = 42;

            var meshBoundingBox = new BoundingBox();

            var vertexIndices = new List<uint>();
            var triangleIndices = new List<uint>();
            var meshlets = new List<Meshlet>();

            var meshletVertexIndices = new List<uint>();
            var meshletTriangleIndices = new List<uint>();

            for (var i = 0; i < importMeshData.Indices.Count; i += 3)
            {
                // TODO: This is sub-optimal because triangles can share vertices
                if (meshletVertexIndices.Count + 3 > maxVertexCount || meshletTriangleIndices.Count + 3 > maxTriangleCount * 3)
                {
                    var vertexOffset = (uint)vertexIndices.Count;
                    var triangleOffset = (uint)triangleIndices.Count;

                    vertexIndices.AddRange(meshletVertexIndices);
                    triangleIndices.AddRange(meshletTriangleIndices);

                    meshlets.Add(new Meshlet((uint)meshletVertexIndices.Count, vertexOffset, (uint)meshletTriangleIndices.Count / 3, triangleOffset / 3));

                    meshletVertexIndices.Clear();
                    meshletTriangleIndices.Clear();
                }

                var vertexIndex0 = importMeshData.Indices[i];
                var vertexIndex1 = importMeshData.Indices[i + 1];
                var vertexIndex2 = importMeshData.Indices[i + 2];

                var vertex0 = importMeshData.Vertices[(int)vertexIndex0];
                var vertex1 = importMeshData.Vertices[(int)vertexIndex1];
                var vertex2 = importMeshData.Vertices[(int)vertexIndex2];

                meshBoundingBox = BoundingBox.AddPoint(meshBoundingBox, vertex0.Position);
                meshBoundingBox = BoundingBox.AddPoint(meshBoundingBox, vertex1.Position);
                meshBoundingBox = BoundingBox.AddPoint(meshBoundingBox, vertex2.Position);

                if (!meshletVertexIndices.Contains(vertexIndex0))
                {
                    meshletVertexIndices.Add(vertexIndex0);
                }

                var vertexLocalIndex0 = (uint)meshletVertexIndices.IndexOf(vertexIndex0);
                meshletTriangleIndices.Add(vertexLocalIndex0);

                if (!meshletVertexIndices.Contains(vertexIndex1))
                {
                    meshletVertexIndices.Add(vertexIndex1);
                }

                var vertexLocalIndex1 = (uint)meshletVertexIndices.IndexOf(vertexIndex1);
                meshletTriangleIndices.Add(vertexLocalIndex1);

                if (!meshletVertexIndices.Contains(vertexIndex2))
                {
                    meshletVertexIndices.Add(vertexIndex2);
                }

                var vertexLocalIndex2 = (uint)meshletVertexIndices.IndexOf(vertexIndex2);
                meshletTriangleIndices.Add(vertexLocalIndex2);
            }

            var finalVertexOffset = (uint)vertexIndices.Count;
            var finalTriangleOffset = (uint)triangleIndices.Count;

            vertexIndices.AddRange(meshletVertexIndices);
            triangleIndices.AddRange(meshletTriangleIndices);

            meshlets.Add(new Meshlet((uint)meshletVertexIndices.Count, finalVertexOffset, (uint)meshletTriangleIndices.Count / 3, finalTriangleOffset / 3));

            // TODO: Remove the ToArray here, big perf impact
            return new MeshData(meshBoundingBox, importMeshData.Vertices.ToArray(), vertexIndices.ToArray(), triangleIndices.ToArray(), meshlets.ToArray());
        }

        private static ReadOnlyMemory<byte> WriteMeshData(in MeshData meshData)
        {
            var version = 1u;
            var destinationMemoryStream = new MemoryStream();

            using var streamWriter = new BinaryWriter(destinationMemoryStream);
            streamWriter.Write(new char[] { 'M', 'E', 'S', 'H'});
            streamWriter.Write(version);

            streamWriter.Write(meshData.BoundingBox.MinPoint.X);
            streamWriter.Write(meshData.BoundingBox.MinPoint.Y);
            streamWriter.Write(meshData.BoundingBox.MinPoint.Z);
            streamWriter.Write(meshData.BoundingBox.MaxPoint.X);
            streamWriter.Write(meshData.BoundingBox.MaxPoint.Y);
            streamWriter.Write(meshData.BoundingBox.MaxPoint.Z);

            streamWriter.Write((uint)meshData.Meshlets.Length);
            streamWriter.Write((uint)meshData.TriangleIndices.Length / 3);

            streamWriter.Flush();

            var currentOffset = (ulong)destinationMemoryStream.Position + 64;

            var verticesSizeInBytes = (ulong)meshData.Vertices.Length * (ulong)Marshal.SizeOf<MeshVertex>();
            streamWriter.Write(currentOffset);
            streamWriter.Write(verticesSizeInBytes);
            currentOffset += verticesSizeInBytes;

            var vertexIndicesSizeInBytes = (ulong)meshData.VertexIndices.Length * sizeof(uint);
            streamWriter.Write(currentOffset);
            streamWriter.Write(vertexIndicesSizeInBytes);
            currentOffset += vertexIndicesSizeInBytes;

            var triangleIndicesSizeInBytes = (ulong)meshData.TriangleIndices.Length * sizeof(uint);
            streamWriter.Write(currentOffset);
            streamWriter.Write(triangleIndicesSizeInBytes);
            currentOffset += triangleIndicesSizeInBytes;

            var meshletsSizeInBytes = (ulong)meshData.Meshlets.Length * (ulong)Marshal.SizeOf<Meshlet>();
            streamWriter.Write(currentOffset);
            streamWriter.Write(meshletsSizeInBytes);
            
            for (var i = 0; i < meshData.Vertices.Length; i++)
            {
                var vertex = meshData.Vertices.Span[i];

                streamWriter.Write(vertex.Position.X);
                streamWriter.Write(vertex.Position.Y);
                streamWriter.Write(vertex.Position.Z);
                streamWriter.Write(vertex.Normal.X);
                streamWriter.Write(vertex.Normal.Y);
                streamWriter.Write(vertex.Normal.Z);
                streamWriter.Write(vertex.TextureCoordinates.X);
                streamWriter.Write(vertex.TextureCoordinates.Y);
            }

            for (var i = 0; i < meshData.VertexIndices.Length; i++)
            {
                streamWriter.Write(meshData.VertexIndices.Span[i]);
            }

            for (var i = 0; i < meshData.TriangleIndices.Length; i++)
            {
                streamWriter.Write(meshData.TriangleIndices.Span[i]);
            }

            for (var i = 0; i < meshData.Meshlets.Length; i++)
            {
                var meshlet = meshData.Meshlets.Span[i];

                streamWriter.Write(meshlet.VertexCount);
                streamWriter.Write(meshlet.VertexOffset);
                streamWriter.Write(meshlet.TriangleCount);
                streamWriter.Write(meshlet.TriangleOffset);
            }

            streamWriter.Flush();

            destinationMemoryStream.Flush();
            return new Memory<byte>(destinationMemoryStream.GetBuffer(), 0, (int)destinationMemoryStream.Length);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using MeshOptimizerLib;
using CoreEngine.Diagnostics;

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
                    var meshData = BuildMeshData(importMeshData, optimize: true);

                    var resourceData = WriteMeshData(meshData);
                    var resourceEntry = new ResourceCompilerOutput($"{Path.GetFileNameWithoutExtension(context.SourceFilename)}{this.DestinationExtension}", resourceData);

                    return new ReadOnlyMemory<ResourceCompilerOutput>(new ResourceCompilerOutput[] { resourceEntry });
                }
            }

            return null;
        }

        private static MeshData BuildMeshData(ImportMeshData importMeshData, bool optimize)
        {
            // TODO: Remove the ToArray here, big perf impact
            var vertexBuffer = importMeshData.Vertices.ToArray().AsSpan();
            var indexBuffer = importMeshData.Indices.ToArray().AsSpan();

            if (optimize)
            {
                Logger.BeginAction("Optimizing Mesh");
                var meshOptimizer = new MeshOptimizer();
                var remap = meshOptimizer.GenerateVertexRemap<MeshVertex>(indexBuffer, vertexBuffer);

                meshOptimizer.RemapVertexBuffer(ref vertexBuffer, remap);
                meshOptimizer.RemapIndexBuffer(indexBuffer, remap);

                meshOptimizer.OptimizeVertexCache(indexBuffer, (ulong)vertexBuffer.Length);
                meshOptimizer.OptimizeVertexFetch(indexBuffer, ref vertexBuffer);
                Logger.EndAction();
            }
            
            Logger.BeginAction("Generate Meshlets");

            // Build Bounding Box
            var meshBoundingBox = new BoundingBox();

            for (var i = 0; i < vertexBuffer.Length; i++)
            {
                var vertex = vertexBuffer[i];
                meshBoundingBox = BoundingBox.AddPoint(meshBoundingBox, vertex.Position);
            }

            // Build meshlets
            const uint maxVertexCount = 64;
            const uint maxTriangleCount = 126;

            var vertexIndices = new List<uint>();
            var triangleIndices = new List<uint>();
            var meshlets = new List<Meshlet>();

            var meshletVertexIndices = new List<uint>();
            var meshletTriangleIndices = new List<uint>();

            for (var i = 0; i < indexBuffer.Length; i += 3)
            {
                var vertexIndex0 = indexBuffer[i];
                var vertexIndex1 = indexBuffer[i + 1];
                var vertexIndex2 = indexBuffer[i + 2];

                var vertexIndex0Flag = !meshletVertexIndices.Contains(vertexIndex0) ? 1 : 0;
                var vertexIndex1Flag = !meshletVertexIndices.Contains(vertexIndex1) ? 1 : 0;
                var vertexIndex2Flag = !meshletVertexIndices.Contains(vertexIndex2) ? 1 : 0;

                // TODO: This is sub-optimal because triangles can share vertices
                // Big mesh should have 11284 and currently we have 11292
                if ((meshletVertexIndices.Count + vertexIndex0Flag + vertexIndex1Flag + vertexIndex2Flag) > maxVertexCount || meshletTriangleIndices.Count >= maxTriangleCount)
                {
                    var vertexOffset = (uint)vertexIndices.Count;
                    var triangleOffset = (uint)triangleIndices.Count;

                    vertexIndices.AddRange(meshletVertexIndices);
                    triangleIndices.AddRange(meshletTriangleIndices);

                    meshlets.Add(new Meshlet((uint)meshletVertexIndices.Count, vertexOffset, (uint)meshletTriangleIndices.Count, triangleOffset));

                    meshletVertexIndices.Clear();
                    meshletTriangleIndices.Clear();

                    vertexIndex0Flag = !meshletVertexIndices.Contains(vertexIndex0) ? 1 : 0;
                    vertexIndex1Flag = !meshletVertexIndices.Contains(vertexIndex1) ? 1 : 0;
                    vertexIndex2Flag = !meshletVertexIndices.Contains(vertexIndex2) ? 1 : 0;
                }

                if (vertexIndex0Flag == 1)
                {
                    meshletVertexIndices.Add(vertexIndex0);
                }

                var vertexLocalIndex0 = (uint)meshletVertexIndices.IndexOf(vertexIndex0);

                if (vertexIndex1Flag == 1)
                {
                    meshletVertexIndices.Add(vertexIndex1);
                }

                var vertexLocalIndex1 = (uint)meshletVertexIndices.IndexOf(vertexIndex1);

                if (vertexIndex2Flag == 1)
                {
                    meshletVertexIndices.Add(vertexIndex2);
                }

                var vertexLocalIndex2 = (uint)meshletVertexIndices.IndexOf(vertexIndex2);

                meshletTriangleIndices.Add(vertexLocalIndex2 << 16 | vertexLocalIndex1 << 8 | vertexLocalIndex0);
            }

            var finalVertexOffset = (uint)vertexIndices.Count;
            var finalTriangleOffset = (uint)triangleIndices.Count;

            vertexIndices.AddRange(meshletVertexIndices);
            triangleIndices.AddRange(meshletTriangleIndices);

            meshlets.Add(new Meshlet((uint)meshletVertexIndices.Count, finalVertexOffset, (uint)meshletTriangleIndices.Count, finalTriangleOffset));

            Logger.EndAction();

            // TODO: Remove the ToArray here, big perf impact
            return new MeshData(meshBoundingBox, vertexBuffer.ToArray(), vertexIndices.ToArray(), triangleIndices.ToArray(), meshlets.ToArray());
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
            streamWriter.Write((uint)meshData.TriangleIndices.Length);

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
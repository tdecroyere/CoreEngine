using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using MeshOptimizerLib;
using CoreEngine.Diagnostics;
using System.Diagnostics;

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
            // TODO: Remove the ToArray here, big perf impact
            var vertexBuffer = importMeshData.Vertices.ToArray().AsSpan();
            var indexBuffer = importMeshData.Indices.ToArray().AsSpan();

            Logger.BeginAction("Optimizing Mesh");
            var meshOptimizer = new MeshOptimizer();
            var remap = meshOptimizer.GenerateVertexRemap<MeshVertex>(indexBuffer, vertexBuffer);

            meshOptimizer.RemapVertexBuffer(ref vertexBuffer, remap);
            meshOptimizer.RemapIndexBuffer(indexBuffer, remap);

            meshOptimizer.OptimizeVertexCache(indexBuffer, (ulong)vertexBuffer.Length);
            meshOptimizer.OptimizeVertexFetch(indexBuffer, ref vertexBuffer);
            Logger.EndAction();
            
            Logger.BeginAction("Generate Meshlets");
            
            // Build meshlets
            const uint maxVertexCount = 64;
            const uint maxTriangleCount = 126;
            
            var meshletCount = (int)meshOptimizer.BuildMeshletsBound((ulong)indexBuffer.Length, maxVertexCount, maxTriangleCount);
            
            var tmpMeshlets = new MeshoptMeshlet[meshletCount].AsSpan();
            var tmpMeshletVertices = new uint[meshletCount * maxVertexCount].AsSpan();
            var tmpMeshletTriangles = new byte[meshletCount * maxTriangleCount * 3].AsSpan();
            meshOptimizer.BuildMeshletsScan(ref tmpMeshlets, ref tmpMeshletVertices, ref tmpMeshletTriangles, indexBuffer, (ulong)indexBuffer.Length, (ulong)vertexBuffer.Length, maxVertexCount, maxTriangleCount);

            var meshlets = new Meshlet[tmpMeshlets.Length];
            var triangleIndices = new List<uint>();
            var meshletTriangleIndices = new List<uint>();

            for (var i = 0; i < tmpMeshlets.Length; i ++)
            {
                meshletTriangleIndices.Clear();

                for (var j = 0; j < tmpMeshlets[i].TriangleCount * 3; j += 3)
                {
                    var vertexLocalIndex = (uint)tmpMeshletTriangles[(int)tmpMeshlets[i].TriangleOffset + j];
                    var vertexLocalIndex1 = (uint)tmpMeshletTriangles[(int)tmpMeshlets[i].TriangleOffset + j + 1];
                    var vertexLocalIndex2 = (uint)tmpMeshletTriangles[(int)tmpMeshlets[i].TriangleOffset + j + 2];
                
                    meshletTriangleIndices.Add(vertexLocalIndex2 << 16 | vertexLocalIndex1 << 8 | vertexLocalIndex);
                }

                var bounds = meshOptimizer.ComputeMeshletBounds<MeshVertex>(tmpMeshlets[i], tmpMeshletVertices, tmpMeshletTriangles, vertexBuffer);
                var packedCone = (byte)bounds.cone_cutoff_s8 << 24 | (byte)bounds.cone_axis_s8_2 << 16 | (byte)bounds.cone_axis_s8_1 << 8 | (byte)bounds.cone_axis_s8_0; 

                meshlets[i] = new Meshlet(packedCone, new Vector4(bounds.Center, bounds.Radius), tmpMeshlets[i].VertexCount, tmpMeshlets[i].VertexOffset, tmpMeshlets[i].TriangleCount, (uint)triangleIndices.Count);
                triangleIndices.AddRange(meshletTriangleIndices);
            }

            // Build Bounding Box
            var meshBoundingBox = new BoundingBox();

            for (var i = 0; i < vertexBuffer.Length; i++)
            {
                var vertex = vertexBuffer[i];
                meshBoundingBox = BoundingBox.AddPoint(meshBoundingBox, vertex.Position);
            }

            Logger.EndAction();

            Logger.WriteMessage($"Meshlet Count: {meshlets.Length}");

            // TODO: Remove the ToArray here, big perf impact
            return new MeshData(meshBoundingBox, vertexBuffer.ToArray(), tmpMeshletVertices.ToArray(), triangleIndices.ToArray(), meshlets);
        }

        private static Vector4 ComputeMeshletCone(List<uint> meshletVertexIndices, List<uint> meshletTriangleIndices, ReadOnlySpan<MeshVertex> vertexBuffer)
        {
            var meshletNormal = Vector3.Zero;
            var triangleNormals = new Vector3[meshletTriangleIndices.Count];

            for (var i = 0; i < meshletTriangleIndices.Count; i++)
            {
                var trianglePackedIndices = meshletTriangleIndices[i];

                var vertexIndex0 = trianglePackedIndices & 0xFF;
                var vertexIndex1 = trianglePackedIndices >> 8 & 0xFF;
                var vertexIndex2 = trianglePackedIndices >> 16 & 0xFF;

                var vertexPosition0 = vertexBuffer[(int)meshletVertexIndices[(int)vertexIndex0]].Position;              
                var vertexPosition1 = vertexBuffer[(int)meshletVertexIndices[(int)vertexIndex1]].Position;              
                var vertexPosition2 = vertexBuffer[(int)meshletVertexIndices[(int)vertexIndex2]].Position;

                // Check for zero area triangles
                if (vertexPosition1 == vertexPosition2 || vertexPosition0 == vertexPosition1 || vertexPosition0 == vertexPosition2)
                {
                    continue;
                }

                var triangleVector0 = vertexPosition1 - vertexPosition0;
                var triangleVector1 = vertexPosition2 - vertexPosition0;

                var triangleNormal = Vector3.Normalize(Vector3.Cross(triangleVector0, triangleVector1));
                triangleNormals[i] = triangleNormal;

                meshletNormal += triangleNormal;
            }

            var averageNormal = Vector3.Normalize(meshletNormal);
            var minDotProduct = 1.0f;

            for (var i = 0; i < triangleNormals.Length; i++)
            {
                var triangleNormal = triangleNormals[i];

                if (triangleNormal != Vector3.Zero)
                {
                    var dotProduct = Vector3.Dot(averageNormal, triangleNormal);
                    minDotProduct = MathF.Min(minDotProduct, dotProduct);
                }
            }

            var coneW = minDotProduct <= 0.0f ? 1.0f : MathF.Sqrt(1 - minDotProduct * minDotProduct);

            return new Vector4(averageNormal, coneW);
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

                streamWriter.Write(meshlet.PackedCone);
                streamWriter.Write(meshlet.BoundingSphere.X);
                streamWriter.Write(meshlet.BoundingSphere.Y);
                streamWriter.Write(meshlet.BoundingSphere.Z);
                streamWriter.Write(meshlet.BoundingSphere.W);
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
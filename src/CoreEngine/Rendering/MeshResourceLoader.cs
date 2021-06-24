using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class MeshResourceLoader : ResourceLoader
    {
        public MeshResourceLoader(ResourcesManager resourcesManager) : base(resourcesManager)
        {
        }

        public override string Name => "Mesh Loader";
        public override string FileExtension => ".mesh";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Mesh(resourceId, path);
        }

        public override Resource LoadResourceData(Resource resource, byte[] data)
        {
            var mesh = resource as Mesh;

            if (mesh == null)
            {
                throw new ArgumentException("Resource is not a mesh resource.", nameof(resource));
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var meshSignature = reader.ReadChars(4);
            var meshVersion = reader.ReadUInt32();

            if (meshSignature.ToString() != "MESH" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for mesh '{resource.Path}'");
                return resource;
            }

            var xBoundingBox = reader.ReadSingle();
            var yBoundingBox = reader.ReadSingle();
            var zBoundingBox = reader.ReadSingle();

            var minPointBoundingBox = new Vector3(xBoundingBox, yBoundingBox, zBoundingBox);

            xBoundingBox = reader.ReadSingle();
            yBoundingBox = reader.ReadSingle();
            zBoundingBox = reader.ReadSingle();

            var maxPointBoundingBox = new Vector3(xBoundingBox, yBoundingBox, zBoundingBox);
            mesh.BoundingBox = new BoundingBox(minPointBoundingBox, maxPointBoundingBox);

            mesh.MeshletCount = reader.ReadUInt32();
            mesh.TriangleCount = reader.ReadUInt32();

            mesh.VerticesOffset = reader.ReadUInt64();
            mesh.VerticesSizeInBytes = reader.ReadUInt64();
            mesh.VertexIndicesOffset = reader.ReadUInt64();
            mesh.VertexIndicesSizeInBytes = reader.ReadUInt64();
            mesh.TriangleIndicesOffset = reader.ReadUInt64();
            mesh.TriangleIndicesSizeInBytes = reader.ReadUInt64();
            mesh.MeshletsOffset = reader.ReadUInt64();
            mesh.MeshletsSizeInBytes = reader.ReadUInt64();

            return mesh;
        }

        public override void DestroyResource(Resource resource)
        {
            var mesh = resource as Mesh;

            if (mesh == null)
            {
                throw new ArgumentException("Resource is not a mesh resource.", nameof(resource));
            }

            Logger.WriteMessage($"Destroying mesh '{resource.Path}' (NOT IMPLEMENTED YET)...");
        }
    }
}
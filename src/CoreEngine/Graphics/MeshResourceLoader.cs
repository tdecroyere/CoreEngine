using System;
using System.IO;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class MeshResourceLoader : ResourceLoader
    {
        private readonly GraphicsService graphicsService;
        private readonly MemoryService memoryService;

        public MeshResourceLoader(ResourcesManager resourcesManager, GraphicsService graphicsService, MemoryService memoryService) : base(resourcesManager)
        {
            this.graphicsService = graphicsService;
            this.memoryService = memoryService;
        }

        public override string Name => "Mesh Loader";
        public override string FileExtension => ".mesh";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Mesh(resourceId, path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var mesh = resource as Mesh;

            if (mesh == null)
            {
                throw new ArgumentException("Resource is not a mesh resource.", "resource");
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var meshSignature = reader.ReadChars(4);
            var meshVersion = reader.ReadInt32();

            if (meshSignature.ToString() != "MESH" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for mesh '{resource.Path}'");
                return Task.FromResult(resource);
            }

            Logger.WriteMessage("Mesh Loading");

            var subObjectsCount = reader.ReadInt32();
            Logger.WriteMessage($"SubObjects Count: {subObjectsCount}");

            for (var i = 0; i < subObjectsCount; i++)
            {
                var vertexCount = reader.ReadInt32();
                var indexCount = reader.ReadInt32();

                //Logger.WriteMessage($"Vertices Count: {vertexCount}, Indices Count: {indexCount}");

                // TODO: Change the calculation of the vertex size (current is fixed to Position, Normal)
                var vertexSize = sizeof(float) * 6;
                var vertexBufferSize = vertexCount * vertexSize;
                var indexBufferSize = indexCount * sizeof(uint);

                var vertexBufferData = reader.ReadBytes(vertexBufferSize);
                var indexBufferData = reader.ReadBytes(indexBufferSize);

                var meshSubObject = new MeshSubObject(this.graphicsService, this.memoryService, vertexCount, indexCount, vertexBufferData.AsSpan(), indexBufferData.AsSpan());
                mesh.SubObjects.Add(meshSubObject);
            }

            return Task.FromResult((Resource)mesh);
        }

        public override void DestroyResource(Resource resource)
        {
            var mesh = resource as Mesh;

            if (mesh == null)
            {
                throw new ArgumentException("Resource is not a mesh resource.", "resource");
            }

            Logger.WriteMessage($"Destroying mesh '{resource.Path}' (NOT IMPLEMENTED YET)...");
        }
    }
}
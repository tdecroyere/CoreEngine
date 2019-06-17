using System;
using System.IO;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class MeshResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;
        private readonly MemoryService memoryService;

        public MeshResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager, MemoryService memoryService) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
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
                throw new ArgumentException("Resource is not a mesh resource.", nameof(resource));
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

            // TODO: Read the vertex format from the mesh file
            var vertexLayout = new VertexLayout(VertexElementType.Float3, VertexElementType.Float3);

            var geometryPacketVertexCount = reader.ReadInt32();
            var geometryPacketIndexCount = reader.ReadInt32();

            //Logger.WriteMessage($"Vertices Count: {vertexCount}, Indices Count: {indexCount}");

            // TODO: Change the calculation of the vertex size (current is fixed to Position, Normal)
            var vertexSize = sizeof(float) * 6;
            var vertexBufferSize = geometryPacketVertexCount * vertexSize;
            var indexBufferSize = geometryPacketIndexCount * sizeof(uint);

            var vertexBufferData = reader.ReadBytes(vertexBufferSize);
            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer(vertexBufferData.AsSpan());

            var indexBufferData = reader.ReadBytes(indexBufferSize);
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer(indexBufferData.AsSpan());
            
            var geometryPacket = new GeometryPacket(vertexLayout, vertexBuffer, indexBuffer);

            var geometryInstancesCount = reader.ReadInt32();
            Logger.WriteMessage($"GeometryInstances Count: {geometryInstancesCount}");

            for (var i = 0; i < geometryInstancesCount; i++)
            {
                var materialPath = reader.ReadString();
                var startIndex = reader.ReadUInt32();
                var indexCount = reader.ReadUInt32();

                var material = this.ResourcesManager.LoadResourceAsync<Material>(materialPath);
                resource.DependentResources.Add(material);

                var geometryInstance = new GeometryInstance(geometryPacket, material, startIndex, indexCount);
                mesh.GeometryInstances.Add(geometryInstance);
            }

            return Task.FromResult((Resource)mesh);
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
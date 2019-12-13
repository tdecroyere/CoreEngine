using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class MeshResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;

        public MeshResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
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

            var geometryPacketVertexCount = reader.ReadInt32();
            var geometryPacketIndexCount = reader.ReadInt32();

            //Logger.WriteMessage($"Vertices Count: {vertexCount}, Indices Count: {indexCount}");

            // TODO: Change the calculation of the vertex size (current is fixed to Position, Normal)
            var vertexSize = sizeof(float) * 8;
            var vertexBufferSize = geometryPacketVertexCount * vertexSize;
            var indexBufferSize = geometryPacketIndexCount * sizeof(uint);

            var vertexBufferData = reader.ReadBytes(vertexBufferSize);
            var indexBufferData = reader.ReadBytes(indexBufferSize);

            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(vertexBufferData.Length, GraphicsResourceType.Static, $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexBuffer");
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(indexBufferData.Length, GraphicsResourceType.Static, $"{Path.GetFileNameWithoutExtension(mesh.Path)}IndexBuffer");

            // TODO: Refactor that
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("MeshLoaderCommandList", true);
            this.graphicsManager.UploadDataToGraphicsBuffer<byte>(copyCommandList, vertexBuffer, vertexBufferData);
            this.graphicsManager.UploadDataToGraphicsBuffer<byte>(copyCommandList, indexBuffer, indexBufferData);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
            
            var geometryPacket = new GeometryPacket(vertexBuffer, indexBuffer);
            mesh.GeometryPacket = geometryPacket;

            var geometryInstancesCount = reader.ReadInt32();
            Logger.WriteMessage($"GeometryInstances Count: {geometryInstancesCount}");

            for (var i = 0; i < geometryInstancesCount; i++)
            {
                var materialPath = reader.ReadString();
                var startIndex = reader.ReadInt32();
                var indexCount = reader.ReadInt32();

                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();

                var minPoint = new Vector3(x, y, z);

                x = reader.ReadSingle();
                y = reader.ReadSingle();
                z = reader.ReadSingle();

                var maxPoint = new Vector3(x, y, z);
                var boundingBox = new BoundingBox(minPoint, maxPoint);

                var material = this.ResourcesManager.LoadResourceAsync<Material>(materialPath);
                resource.DependentResources.Add(material);

                var geometryInstance = new GeometryInstance(geometryPacket, material, startIndex, indexCount, boundingBox);
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
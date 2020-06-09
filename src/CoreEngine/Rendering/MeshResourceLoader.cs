using System;
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
            var meshVersion = reader.ReadInt32();

            if (meshSignature.ToString() != "MESH" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for mesh '{resource.Path}'");
                return resource;
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

            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(vertexBufferData.Length, isStatic: true, isWriteOnly: true, label: $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexBuffer");
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(indexBufferData.Length, isStatic: true, isWriteOnly: true, label: $"{Path.GetFileNameWithoutExtension(mesh.Path)}IndexBuffer");

            var commandBuffer = this.graphicsManager.CreateCommandBuffer("MeshLoader");
            this.graphicsManager.ResetCommandBuffer(commandBuffer);
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "MeshLoaderCommandList");
            this.graphicsManager.UploadDataToGraphicsBuffer<byte>(copyCommandList, vertexBuffer, vertexBufferData);
            this.graphicsManager.UploadDataToGraphicsBuffer<byte>(copyCommandList, indexBuffer, indexBufferData);
            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);
            
            var geometryPacket = new GeometryPacket(vertexBuffer, indexBuffer);

            var geometryInstancesCount = reader.ReadInt32();
            Logger.WriteMessage($"GeometryInstances Count: {geometryInstancesCount}");

            mesh.GeometryInstances.Clear();

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

                Material? material = null;

                if (!string.IsNullOrEmpty(materialPath))
                {
                    material = this.ResourcesManager.LoadResourceAsync<Material>($"{Path.GetDirectoryName(resource.Path)}/{materialPath}");
                    resource.DependentResources.Add(material);
                }

                var geometryInstance = new GeometryInstance(geometryPacket, material, startIndex, indexCount, boundingBox);
                mesh.GeometryInstances.Add(geometryInstance);
            }

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
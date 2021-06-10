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
        private readonly GraphicsManager graphicsManager;
        private readonly RenderManager renderManager;

        public MeshResourceLoader(ResourcesManager resourcesManager, RenderManager renderManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
            this.renderManager = renderManager;
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

            var cpuVertexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, vertexBufferSize, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexBuffer");
            var cpuIndexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, indexBufferSize, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexBuffer");

            var vertexBufferData = ArrayPool<byte>.Shared.Rent(vertexBufferSize);
            reader.Read(vertexBufferData, 0, vertexBufferSize);
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(cpuVertexBuffer, 0, vertexBufferData.AsSpan().Slice(0, vertexBufferSize));
            ArrayPool<byte>.Shared.Return(vertexBufferData);

            var indexBufferData = ArrayPool<byte>.Shared.Rent(indexBufferSize);
            reader.Read(indexBufferData, 0, indexBufferSize);
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(cpuIndexBuffer, 0, indexBufferData.AsSpan().Slice(0, indexBufferSize));
            ArrayPool<byte>.Shared.Return(indexBufferData);

            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Gpu, vertexBufferSize, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexBuffer");
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Gpu, indexBufferSize, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(mesh.Path)}IndexBuffer");

            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "MeshLoader");
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(copyCommandList, vertexBuffer, cpuVertexBuffer, vertexBufferSize);
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(copyCommandList, indexBuffer, cpuIndexBuffer, indexBufferSize);
            this.graphicsManager.CommitCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList });
            
            var geometryPacket = new GeometryPacket(vertexBuffer, indexBuffer);

            var geometryInstancesCount = reader.ReadInt32();
            Logger.WriteMessage($"GeometryInstances Count: {geometryInstancesCount}");

            mesh.GeometryInstances.Clear();

            for (var i = 0; i < geometryInstancesCount; i++)
            {
                var materialPath = reader.ReadString();
                var startIndex = reader.ReadInt32();
                var indexCount = reader.ReadInt32();

                // TODO: Hack
                var vertexCount = indexCount;

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

                var geometryInstance = new GeometryInstance(geometryPacket, material, startIndex, indexCount, vertexCount, boundingBox);
                mesh.GeometryInstances.Add(geometryInstance);
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
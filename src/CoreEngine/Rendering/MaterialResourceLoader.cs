using System;
using System.Buffers;
using System.IO;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class MaterialResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;
        private readonly RenderManager renderManager;

        public MaterialResourceLoader(ResourcesManager resourcesManager, RenderManager renderManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
            this.renderManager = renderManager;
        }

        public override string Name => "Material Loader";
        public override string FileExtension => ".material";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Material(resourceId, path);
        }

        public override Resource LoadResourceData(Resource resource, byte[] data)
        {
            var material = resource as Material;

            if (material == null)
            {
                throw new ArgumentException("Resource is not a material resource.", nameof(resource));
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var meshSignature = reader.ReadChars(8);
            var meshVersion = reader.ReadInt32();

            if (meshSignature.ToString() != "MATERIAL" && meshVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for material '{resource.Path}'");
                return resource;
            }

            material.IsTransparent = reader.ReadBoolean();

            var textureResourceListCount = reader.ReadInt32();
            material.TextureList = new Texture[textureResourceListCount];

            for (var i = 0; i < textureResourceListCount; i++)
            {
                var offset = reader.ReadInt32();
                var resourcePath = reader.ReadString();
                resourcePath = resourcePath.Replace("resource:", string.Empty);

                var texture = this.ResourcesManager.LoadResourceAsync<Texture>(resourcePath);
                resource.DependentResources.Add(texture);

                material.TextureList.Span[i] = texture;
            }

            var materialDataLength = reader.ReadInt32();
            var materialData = ArrayPool<byte>.Shared.Rent(materialDataLength);
            reader.Read(materialData, 0, materialDataLength);
            using var cpuBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, materialDataLength, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(material.Path)}MaterialBuffer");
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(cpuBuffer, 0, materialData.AsSpan().Slice(0, materialDataLength));
            ArrayPool<byte>.Shared.Return(materialData);

            material.MaterialData = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, materialData.Length, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(material.Path)}MaterialBuffer");

            // TODO: Refactor that
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "MaterialLoader");
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(copyCommandList, material.MaterialData, cpuBuffer, materialDataLength);
            this.graphicsManager.CommitCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList });

            return resource;
        }
    }
}
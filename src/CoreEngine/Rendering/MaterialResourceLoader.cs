using System;
using System.IO;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class MaterialResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;

        public MaterialResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
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

            var cpuBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(materialDataLength, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(material.Path)}MaterialBuffer", GraphicsHeapType.Upload);
            var materialData = this.graphicsManager.GetCpuGraphicsBufferPointer<byte>(cpuBuffer);
            reader.Read(materialData);

            material.MaterialData = this.graphicsManager.CreateGraphicsBuffer<byte>(materialData.Length, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(material.Path)}MaterialBuffer");

            // TODO: Refactor that
            var commandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "MaterialLoader");
            this.graphicsManager.ResetCommandBuffer(commandBuffer);

            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "MaterialLoaderCommandList");
            this.graphicsManager.UploadDataToGraphicsBuffer<byte>(copyCommandList, material.MaterialData.Value, cpuBuffer, materialDataLength);
            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteGraphicsBuffer(cpuBuffer);

            return resource;
        }
    }
}
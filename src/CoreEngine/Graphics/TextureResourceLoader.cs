using System;
using System.IO;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class TextureResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;

        public TextureResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
        }

        public override string Name => "Texture Loader";
        public override string FileExtension => ".texture";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            // TODO: Provide a default visible texture

            return new Texture(this.graphicsManager, 256, 256, resourceId, path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var texture = resource as Texture;

            if (texture == null)
            {
                throw new ArgumentException("Resource is not a Texture resource.", nameof(resource));
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var textureSignature = reader.ReadChars(7);
            var textureVersion = reader.ReadInt32();

            if (textureSignature.ToString() != "TEXTURE" && textureVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for Texture '{resource.Path}'");
                return Task.FromResult(resource);
            }

            texture.Width = reader.ReadInt32();
            texture.Height = reader.ReadInt32();
            var textureDataLength = reader.ReadInt32();
            var textureData = reader.ReadBytes(textureDataLength);

            if (texture.Id != 0)
            {
                // TODO: Implement remove texture
                //this.graphicsService.RemoveTexture(texture.TextureId);
            }

            var createdTexture = this.graphicsManager.CreateTexture(texture.Width, texture.Height);
            texture.SystemId = createdTexture.SystemId;
            texture.SystemId2 = createdTexture.SystemId2;

            // TODO: Make only one frame copy command list for all resource loaders
            var copyCommandList = this.graphicsManager.CreateCopyCommandList();
            this.graphicsManager.UploadDataToTexture<byte>(copyCommandList, texture, textureData);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            // TODO: Upload data
            //textureData

            return Task.FromResult((Resource)texture);
        }
    }
}
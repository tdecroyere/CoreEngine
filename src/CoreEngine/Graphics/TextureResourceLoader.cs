using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class TextureResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;
        private Texture emptyTexture;

        public TextureResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsManager = graphicsManager;
            this.emptyTexture = graphicsManager.CreateTexture(TextureFormat.Rgba8UnormSrgb, 256, 256, 1, 1, 1, false, isStatic: true, label: "EmptyTexture");

            var textureData = new byte[256 * 256 * 4];
            Array.Fill<byte>(textureData, 255);

            var commandBuffer = this.graphicsManager.CreateCommandBuffer("TextureLoader");
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "TextureLoaderCommandList");
            this.graphicsManager.UploadDataToTexture<byte>(copyCommandList, this.emptyTexture, 256, 256, 0, 0, textureData);
            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);
        }

        public override string Name => "Texture Loader";
        public override string FileExtension => ".texture";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            var texture = new Texture(this.graphicsManager, 256, 256, resourceId, path, $"{Path.GetFileNameWithoutExtension(path)}Texture");
            texture.GraphicsResourceSystemId = this.emptyTexture.GraphicsResourceSystemId;
            return texture;
        }

        public override Resource LoadResourceData(Resource resource, byte[] data)
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
                return resource;
            }

            texture.Width = reader.ReadInt32();
            texture.Height = reader.ReadInt32();
            texture.TextureFormat = (TextureFormat)reader.ReadInt32();
            texture.FaceCount = reader.ReadInt32();
            texture.MipLevels = reader.ReadInt32();

            if (texture.GraphicsResourceId != 0 && texture.GraphicsResourceSystemId != this.emptyTexture.GraphicsResourceSystemId)
            {
                this.graphicsManager.DeleteTexture(texture);
            }

            // TODO: Wait for the command buffer to finish execution before switching the system ids.

            var createdTexture = this.graphicsManager.CreateTexture(texture.TextureFormat, texture.Width, texture.Height, texture.FaceCount, texture.MipLevels, 1, false, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(texture.Path)}Texture");
            texture.GraphicsResourceSystemId = createdTexture.GraphicsResourceSystemId;
            texture.GraphicsResourceSystemId2 = createdTexture.GraphicsResourceSystemId2;

            var commandBuffer = this.graphicsManager.CreateCommandBuffer("TextureLoader");
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "TextureLoaderCommandList");

            for (var i = 0; i < texture.FaceCount; i++)
            {
                var textureWidth = texture.Width;
                var textureHeight = texture.Height;

                for (var j = 0; j < texture.MipLevels; j++)
                {
                    var textureDataLength = reader.ReadInt32();
                    var textureData = reader.ReadBytes(textureDataLength);

                    if (j > 0)
                    {
                        textureWidth = (textureWidth > 1) ? textureWidth / 2 : 1;
                        textureHeight = (textureHeight > 1) ? textureHeight / 2 : 1;
                    }

                    // TODO: Make only one frame copy command list for all resource loaders
                    this.graphicsManager.UploadDataToTexture<byte>(copyCommandList, texture, textureWidth, textureHeight, i, j, textureData);
                }
            }

            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);
            return texture;
        }
    }
}
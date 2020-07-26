using System;
using System.IO;
using System.Runtime.InteropServices;
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

            // TODO: Remove the responsability of the loader to create empty resources
            Logger.BeginAction("Create Empty Texture");
            Logger.BeginAction("Create Resource");
            this.emptyTexture = graphicsManager.CreateTexture(GraphicsHeapType.Gpu, TextureFormat.Rgba8UnormSrgb, 256, 256, 1, 1, 1, false, isStatic: true, label: "EmptyTexture");
            Logger.EndAction();

            var cpuBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, 256 * 256 * 4, isStatic: true, label: "TextureCpuBuffer");
            var textureData = this.graphicsManager.GetCpuGraphicsBufferPointer<byte>(cpuBuffer);
            textureData.Fill(255);

            var commandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "TextureLoader");
            this.graphicsManager.ResetCommandBuffer(commandBuffer);
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "TextureLoaderCommandList");
            this.graphicsManager.CopyDataToTexture<byte>(copyCommandList, this.emptyTexture, cpuBuffer, 256, 256, 0, 0);
            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);
            Logger.EndAction();

            this.graphicsManager.DeleteGraphicsBuffer(cpuBuffer);
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
            var createdTexture = this.graphicsManager.CreateTexture(GraphicsHeapType.Gpu, texture.TextureFormat, texture.Width, texture.Height, texture.FaceCount, texture.MipLevels, 1, false, isStatic: true, label: $"{Path.GetFileNameWithoutExtension(texture.Path)}Texture");
            texture.GraphicsResourceSystemId = createdTexture.GraphicsResourceSystemId;
            texture.GraphicsResourceSystemId2 = createdTexture.GraphicsResourceSystemId2;

            var commandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "TextureLoader");
            this.graphicsManager.ResetCommandBuffer(commandBuffer);
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "TextureLoaderCommandList");

            for (var i = 0; i < texture.FaceCount; i++)
            {
                var textureWidth = texture.Width;
                var textureHeight = texture.Height;

                for (var j = 0; j < texture.MipLevels; j++)
                {
                    var textureDataLength = reader.ReadInt32();
                 
                    var cpuBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, textureDataLength, isStatic: true, label: "TextureCpuBuffer");
                    var textureData = this.graphicsManager.GetCpuGraphicsBufferPointer<byte>(cpuBuffer);
                    
                    reader.Read(textureData);

                    if (j > 0)
                    {
                        textureWidth = (textureWidth > 1) ? textureWidth / 2 : 1;
                        textureHeight = (textureHeight > 1) ? textureHeight / 2 : 1;
                    }

                    // TODO: Make only one frame copy command list for all resource loaders
                    this.graphicsManager.CopyDataToTexture<byte>(copyCommandList, texture, cpuBuffer, textureWidth, textureHeight, i, j);
                    this.graphicsManager.DeleteGraphicsBuffer(cpuBuffer);
                }
            }

            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);

            return texture;
        }
    }
}
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class FontResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;

        public FontResourceLoader(ResourcesManager resourcesManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
        }

        public override string Name => "Font Loader";
        public override string FileExtension => ".font";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Font(this.graphicsManager, 256, 256, resourceId, path);
        }

        public override Task<Resource> LoadResourceDataAsync(Resource resource, byte[] data)
        {
            var font = resource as Font;

            if (font == null)
            {
                throw new ArgumentException("Resource is not a Texture resource.", nameof(resource));
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var textureSignature = reader.ReadChars(4);
            var textureVersion = reader.ReadInt32();

            if (textureSignature.ToString() != "FONT" && textureVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for Font '{resource.Path}'");
                return Task.FromResult(resource);
            }

            var glyphCount = reader.ReadInt32();
            font.GlyphInfos.Clear();

            for (var i = 0; i < glyphCount; i++)
            {
                var glyph = new FontGlyphInfo();

                glyph.AsciiCode = reader.ReadInt32();
                glyph.Width = reader.ReadInt32();
                glyph.Height = reader.ReadInt32();
                glyph.BearingLeft = reader.ReadInt32();
                glyph.BearingRight = reader.ReadInt32();

                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                glyph.TextureMinPoint = new Vector2(x, y);

                x = reader.ReadSingle();
                y = reader.ReadSingle();
                glyph.TextureMaxPoint = new Vector2(x, y);

                font.GlyphInfos.Add((char)glyph.AsciiCode, glyph);
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            
            var textureDataLength = reader.ReadInt32();
            var textureData = reader.ReadBytes(textureDataLength);

            if (font.Texture.GraphicsResourceId != 0)
            {
                // TODO: Implement remove texture
                //this.graphicsService.RemoveTexture(texture.TextureId);
            }

            font.Texture = this.graphicsManager.CreateTexture(TextureFormat.Rgba8UnormSrgb, width, height, 1);

            // TODO: Make only one frame copy command list for all resource loaders
            var copyCommandList = this.graphicsManager.CreateCopyCommandList();
            this.graphicsManager.UploadDataToTexture<byte>(copyCommandList, font.Texture, font.Texture.Width, font.Texture.Height, 0, textureData);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            // TODO: Upload data
            //textureData

            return Task.FromResult((Resource)font);
        }
    }
}
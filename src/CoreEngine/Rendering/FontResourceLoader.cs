using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class FontResourceLoader : ResourceLoader
    {
        private readonly GraphicsManager graphicsManager;
        private readonly RenderManager renderManager;

        public FontResourceLoader(ResourcesManager resourcesManager, RenderManager renderManager, GraphicsManager graphicsManager) : base(resourcesManager)
        {
            this.graphicsManager = graphicsManager;
            this.renderManager = renderManager;
        }

        public override string Name => "Font Loader";
        public override string FileExtension => ".font";

        public override Resource CreateEmptyResource(uint resourceId, string path)
        {
            return new Font(this.graphicsManager, 256, 256, resourceId, path);
        }

        public override Resource LoadResourceData(Resource resource, byte[] data)
        {
            var font = resource as Font;

            if (font == null)
            {
                throw new ArgumentException("Resource is not a Font resource.", nameof(resource));
            }

            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var textureSignature = reader.ReadChars(4);
            var textureVersion = reader.ReadInt32();

            if (textureSignature.ToString() != "FONT" && textureVersion != 1)
            {
                Logger.WriteMessage($"ERROR: Wrong signature or version for Font '{resource.Path}'");
                return resource;
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

            var textureData = ArrayPool<byte>.Shared.Rent(textureDataLength);
            reader.Read(textureData, 0, textureDataLength);
            using var cpuBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, textureDataLength, isStatic: true, label: "TextureCpuBuffer");
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(cpuBuffer, 0, textureData.AsSpan().Slice(0, textureDataLength));
            ArrayPool<byte>.Shared.Return(textureData);

            if (font.Texture.NativePointer != IntPtr.Zero)
            {
                font.Texture.Dispose();
            }

            font.Texture = this.graphicsManager.CreateTexture(GraphicsHeapType.Gpu, TextureFormat.Rgba8UnormSrgb, TextureUsage.ShaderRead, width, height, 1, 1, 1, isStatic: true, label: "FontTexture");

            // TODO: Make only one frame copy command list for all resource loaders
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "FontLoader");
            this.graphicsManager.CopyDataToTexture<byte>(copyCommandList, font.Texture, cpuBuffer, font.Texture.Width, font.Texture.Height, 0, 0);
            this.graphicsManager.CommitCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList });

            return font;
        }
    }
}
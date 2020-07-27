
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public struct FontGlyphInfo
    {
        public int AsciiCode { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BearingLeft { get; set; }
        public int BearingRight { get; set; }
        public Vector2 TextureMinPoint { get; set; }
        public Vector2 TextureMaxPoint { get; set; }
    }

    public class Font : Resource
    {
        private readonly GraphicsManager graphicsManager;

        internal Font(GraphicsManager graphicsManager, int width, int height, uint resourceId, string path) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.Texture = this.graphicsManager.CreateTexture(GraphicsHeapType.Gpu, TextureFormat.Rgba8UnormSrgb, TextureUsage.ShaderRead, width, height, 1, 1, 1, isStatic: true, label: "FontTexture");
            this.GlyphInfos = new Dictionary<char, FontGlyphInfo>();
        }

        public Texture Texture { get; internal set; }
        public Dictionary<char, FontGlyphInfo> GlyphInfos { get; internal set; }
    }
}
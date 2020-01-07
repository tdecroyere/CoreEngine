using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Material : Resource
    {
        public Material() : base(0, string.Empty)
        {
            this.TextureList = Array.Empty<Texture>();
        }

        internal Material(uint resourceId, string path) : base(resourceId, path)
        {
            this.TextureList = Array.Empty<Texture>();
        }

        public bool IsTransparent { get; set; }
        public GraphicsBuffer? MaterialData { get; internal set; }
        public Memory<Texture> TextureList { get; internal set; }
    }
}
using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Material : Resource
    {
        public Material() : base(0, string.Empty)
        {
        }

        internal Material(uint resourceId, string path) : base(resourceId, path)
        {
        }

        public Shader? Shader { get; }
    }
}
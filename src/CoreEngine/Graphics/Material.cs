using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Material : Resource
    {
        internal Material(uint resourceId, string path) : base(resourceId, path)
        {
        }

        public Shader? Shader { get; }
    }
}
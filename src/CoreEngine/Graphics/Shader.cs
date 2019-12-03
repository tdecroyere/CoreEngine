using System;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Shader : Resource
    {
        internal Shader(uint shaderId) : base(shaderId, string.Empty)
        {
            this.ShaderId = shaderId;
        }

        internal Shader(uint resourceId, string path) : base(resourceId, path)
        {
        }

        public uint ShaderId { get; internal set; }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Shader : Resource
    {
        internal Shader(string label, uint shaderId) : base(shaderId, string.Empty)
        {
            this.ShaderId = shaderId;
            this.Label = label;
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
        }

        internal Shader(uint resourceId, string path) : base(resourceId, path)
        {
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
            this.Label = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public uint ShaderId { get; internal set; }
        public string Label { get; internal set; }
        public IDictionary<GraphicsRenderPassDescriptor, PipelineState> PipelineStates { get; }
    }
}
using System;
using System.Collections.Generic;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Shader : Resource
    {
        internal Shader(string? debugName, uint shaderId) : base(shaderId, string.Empty)
        {
            this.ShaderId = shaderId;
            this.DebugName = debugName;
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
        }

        internal Shader(uint resourceId, string path) : base(resourceId, path)
        {
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
            this.DebugName = null;
        }

        public uint ShaderId { get; internal set; }
        public string? DebugName { get; internal set; }
        public IDictionary<GraphicsRenderPassDescriptor, PipelineState> PipelineStates { get; }
    }
}
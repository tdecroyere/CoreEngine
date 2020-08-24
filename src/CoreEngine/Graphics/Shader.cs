using System;
using System.Collections.Generic;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Shader : Resource
    {
        internal Shader(IntPtr nativePointer, string label) : base(0, string.Empty)
        {
            this.NativePointer = nativePointer;
            this.Label = label;
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
        }

        internal Shader(uint resourceId, string path) : base(resourceId, path)
        {
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
            this.Label = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public IntPtr NativePointer { get; internal set; }
        public string Label { get; internal set; }
        public IDictionary<GraphicsRenderPassDescriptor, PipelineState> PipelineStates { get; }
    }
}
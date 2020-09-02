using System;
using System.Collections.Generic;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class Shader : Resource, IDisposable
    {
        private readonly GraphicsManager graphicsManager;

        internal Shader(GraphicsManager graphicsManager, IntPtr nativePointer, string label) : base(0, string.Empty)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer = nativePointer;
            this.Label = label;
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
        }

        internal Shader(GraphicsManager graphicsManager, uint resourceId, string path) : base(resourceId, path)
        {
            this.graphicsManager = graphicsManager;
            this.PipelineStates = new Dictionary<GraphicsRenderPassDescriptor, PipelineState>();
            this.Label = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public void Dispose()
        {
            this.graphicsManager.ScheduleDeleteShader(this);
            GC.SuppressFinalize(this);
        }

        public IntPtr NativePointer { get; internal set; }
        public string Label { get; internal set; }
        public IDictionary<GraphicsRenderPassDescriptor, PipelineState> PipelineStates { get; }
    }
}
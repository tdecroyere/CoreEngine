using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public readonly struct IndirectCommandBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal IndirectCommandBuffer(GraphicsManager graphicsManager, IntPtr nativePointer1, IntPtr? nativePointer2, int maxCommandCount, bool isStatic, string label)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer1 = nativePointer1;
            this.NativePointer2 = nativePointer2;
            this.MaxCommandCount = maxCommandCount;
            this.ResourceType = GraphicsResourceType.IndirectCommandBuffer;
            this.IsStatic = isStatic;
            this.Label = label;
        }

        public IntPtr NativePointer 
        { 
            get
            {
                var result = this.NativePointer1;

                if (!IsStatic && this.NativePointer2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.NativePointer2.Value;
                }

                return result;
            }
        }

        public IntPtr NativePointer1
        {
            get;
        }

        public IntPtr? NativePointer2
        {
            get;
        }

        public int MaxCommandCount { get; }
        public GraphicsResourceType ResourceType { get; }
        public bool IsStatic { get; }

        public string Label
        {
            get;
        }
    }
}
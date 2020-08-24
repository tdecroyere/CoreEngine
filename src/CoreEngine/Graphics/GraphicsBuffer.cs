using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public readonly struct GraphicsBuffer : IGraphicsResource
    {
        private readonly GraphicsManager graphicsManager;

        internal GraphicsBuffer(GraphicsManager graphicsManager, GraphicsMemoryAllocation graphicsMemoryAllocation, GraphicsMemoryAllocation? graphicsMemoryAllocation2, IntPtr nativePointer1, IntPtr? nativePointer2, IntPtr cpuPointer, IntPtr cpuPointer2, int length, bool isStatic, string label)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer1 = nativePointer1;
            this.NativePointer2 = nativePointer2;
            this.Length = length;
            this.IsStatic = isStatic;
            this.ResourceType = GraphicsResourceType.Buffer;
            this.Label = label;
            this.GraphicsMemoryAllocation = graphicsMemoryAllocation;
            this.GraphicsMemoryAllocation2 = graphicsMemoryAllocation2;
            this.CpuPointer1 = cpuPointer;
            this.CpuPointer2 = cpuPointer2;
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

        public int Length { get; }

        public GraphicsResourceType ResourceType { get; }
        public bool IsStatic { get; }
        public GraphicsMemoryAllocation GraphicsMemoryAllocation { get; }
        public GraphicsMemoryAllocation? GraphicsMemoryAllocation2 { get; }

        public IntPtr CpuPointer 
        { 
            get
            {
                var result = this.CpuPointer1;

                if (!IsStatic && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.CpuPointer2;
                }

                return result;
            }
        }

        public IntPtr CpuPointer1 { get; }
        public IntPtr CpuPointer2 { get; }

        public string Label
        {
            get;
        }
    }
}
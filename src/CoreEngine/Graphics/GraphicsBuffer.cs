using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class GraphicsBuffer : IGraphicsResource, IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private bool isDisposed;

        internal GraphicsBuffer(GraphicsManager graphicsManager, GraphicsMemoryAllocation graphicsMemoryAllocation, GraphicsMemoryAllocation? graphicsMemoryAllocation2, IntPtr nativePointer1, IntPtr? nativePointer2, int length, bool isStatic, string label)
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing && !this.isDisposed)
            {
                this.graphicsManager.ScheduleDeleteGraphicsBuffer(this);
                this.isDisposed = true;
            }
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

        public uint ShaderResourceIndex 
        { 
            get
            {
                var result = this.ShaderResourceIndex1;

                if (!IsStatic && this.ShaderResourceIndex2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
                {
                    result = this.ShaderResourceIndex2.Value;
                }

                return result;
            }
        }

        public uint ShaderResourceIndex1 { get; internal set;}
        public uint? ShaderResourceIndex2 { get; internal set;}

        public string Label
        {
            get;
        }
    }
}
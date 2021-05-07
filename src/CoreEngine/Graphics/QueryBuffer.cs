using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class QueryBuffer : IGraphicsResource, IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private bool isDisposed;

        internal QueryBuffer(GraphicsManager graphicsManager, IntPtr nativePointer1, IntPtr nativePointer2, int length, string label)
        {
            this.graphicsManager = graphicsManager;
            this.NativePointer1 = nativePointer1;
            this.NativePointer2 = nativePointer2;
            this.Length = length;
            this.ResourceType = GraphicsResourceType.QueryBuffer;
            this.Label = label;
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
                this.graphicsManager.ScheduleDeleteQueryBuffer(this);
                this.isDisposed = true;
            }
        }

        public IntPtr NativePointer 
        { 
            get
            {
                var result = this.NativePointer1;

                if (this.NativePointer2 != null && ((this.graphicsManager.CurrentFrameNumber % 2) == 1))
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

        public string Label
        {
            get;
        }

        public bool IsStatic => false;
    }
}
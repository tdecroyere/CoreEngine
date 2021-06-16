using System;
using System.Collections.Generic;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    public class ShaderResourceManager : IDisposable
    {
        private readonly IGraphicsService graphicsService;
        private readonly ShaderResourceHeap shaderResourceHeap;
        private readonly Queue<uint> availableIndexes;
        private uint currentIndex;
        private bool isDisposed;

        public ShaderResourceManager(IGraphicsService graphicsService)
        {
            this.graphicsService = graphicsService;
            this.availableIndexes = new Queue<uint>();

            var heapLength = 1000ul;
            var heapLabel = "ShaderResourceHeap";

            var nativePointer = this.graphicsService.CreateShaderResourceHeap(heapLength);
            this.graphicsService.SetShaderResourceHeapLabel(nativePointer, heapLabel);
            this.shaderResourceHeap = new ShaderResourceHeap(nativePointer, heapLength, heapLabel);
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
                this.graphicsService.DeleteShaderResourceHeap(this.shaderResourceHeap.NativePointer);
                this.isDisposed = true;
            }
        }

        public void CreateShaderResourceTexture(Texture texture)
        {
            if (texture is null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (texture.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.Gpu && texture.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.TransientGpu)
            {
                return;
            }

            var index = GetIndex();

            this.graphicsService.CreateShaderResourceTexture(this.shaderResourceHeap.NativePointer, index, texture.NativePointer1);
            texture.ShaderResourceIndex1 = index;

            if (texture.NativePointer2 != null)
            {
                index = GetIndex();
                this.graphicsService.CreateShaderResourceTexture(this.shaderResourceHeap.NativePointer, index, texture.NativePointer2.Value);
                texture.ShaderResourceIndex2 = index;
            }
        }

        public void DeleteShaderResourceTexture(Texture texture)
        {
            if (texture is null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.availableIndexes.Enqueue(texture.ShaderResourceIndex1);

            if (texture.ShaderResourceIndex2 != null)
            {
                this.availableIndexes.Enqueue(texture.ShaderResourceIndex2.Value);
            }
        }

        public void CreateShaderResourceBuffer(GraphicsBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.Gpu && buffer.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.TransientGpu)
            {
                return;
            }

            var index = GetIndex();

            this.graphicsService.CreateShaderResourceBuffer(this.shaderResourceHeap.NativePointer, index, buffer.NativePointer1);
            buffer.ShaderResourceIndex1 = index;

            if (buffer.NativePointer2 != null)
            {
                index = GetIndex();

                this.graphicsService.CreateShaderResourceBuffer(this.shaderResourceHeap.NativePointer, index, buffer.NativePointer2.Value);
                buffer.ShaderResourceIndex2 = index;
            }
        }

        public void DeleteShaderResourceBuffer(GraphicsBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.Gpu && buffer.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.TransientGpu)
            {
                return;
            }

            this.availableIndexes.Enqueue(buffer.ShaderResourceIndex1);

            if (buffer.ShaderResourceIndex2 != null)
            {
                this.availableIndexes.Enqueue(buffer.ShaderResourceIndex2.Value);
            }
        }

        public void SetShaderResourceHeap(CommandList commandList)
        {
            this.graphicsService.SetShaderResourceHeap(commandList.NativePointer, this.shaderResourceHeap.NativePointer);
        }

        private uint GetIndex()
        {
            if (this.availableIndexes.Count > 0)
            {
                return this.availableIndexes.Dequeue();
            }
            
            return currentIndex++;
        }
    }
}
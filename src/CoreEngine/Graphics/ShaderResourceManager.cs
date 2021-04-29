using System;
using System.Collections.Generic;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    // TODO: IMPORTANT: This code is not thread safe for now

    public class ShaderResourceManager
    {
        private readonly IGraphicsService graphicsService;
        private readonly ShaderResourceHeap shaderResourceHeap;
        private readonly Queue<uint> availableIndexes;
        private uint currentIndex;

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

        public void CreateShaderResourceTexture(Texture texture)
        {
            if (texture is null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            // TODO: Scan the available indexes first

            this.graphicsService.CreateShaderResourceTexture(this.shaderResourceHeap.NativePointer, currentIndex, texture.NativePointer1);
            texture.ShaderResourceIndex1 = currentIndex++;

            if (texture.NativePointer2 != null)
            {
                this.graphicsService.CreateShaderResourceTexture(this.shaderResourceHeap.NativePointer, currentIndex, texture.NativePointer2.Value);
                texture.ShaderResourceIndex2 = currentIndex++;
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

            // TODO: Scan the available indexes first

            this.graphicsService.CreateShaderResourceBuffer(this.shaderResourceHeap.NativePointer, currentIndex, buffer.NativePointer1);
            buffer.ShaderResourceIndex1 = currentIndex++;

            if (buffer.NativePointer2 != null)
            {
                this.graphicsService.CreateShaderResourceBuffer(this.shaderResourceHeap.NativePointer, currentIndex, buffer.NativePointer2.Value);
                buffer.ShaderResourceIndex2 = currentIndex++;
            }
        }

        public void DeleteShaderResourceBuffer(GraphicsBuffer buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
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
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: Implement resource remove functions
    public class GraphicsManager : SystemManager
    {
        private readonly IGraphicsService graphicsService;
        private readonly Graphics2DRenderer internal2DRenderer;

        private static object syncObject = new object();
        private uint currentGraphicsResourceId;
        private Vector2 currentFrameSize;
        private Stopwatch stopwatch;
        private int drawCount;
        private int dispatchCount;
        private Stopwatch globalStopwatch;
        private uint startMeasureFrameNumber;
        int framePerSeconds = 0;

        public GraphicsManager(IGraphicsService graphicsService, GraphicsSceneQueue graphicsSceneQueue, ResourcesManager resourcesManager)
        {
            if (graphicsService == null)
            {
                throw new ArgumentNullException(nameof(graphicsService));
            }

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            InitResourceLoaders(resourcesManager);

            this.graphicsService = graphicsService;
            this.currentGraphicsResourceId = 0;
            this.drawCount = 0;
            this.dispatchCount = 0;
            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
            this.globalStopwatch = new Stopwatch();
            this.globalStopwatch.Start();

            this.GraphicsSceneRenderer = new GraphicsSceneRenderer(this, graphicsSceneQueue, resourcesManager);
            this.Graphics2DRenderer = new Graphics2DRenderer(this, resourcesManager);
            this.internal2DRenderer = new Graphics2DRenderer(this, resourcesManager);

            this.currentFrameSize = graphicsService.GetRenderSize();
            this.MainRenderTargetTexture = CreateTexture(TextureFormat.Bgra8UnormSrgb, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, true, GraphicsResourceType.Dynamic, "MainRenderTarget");
        }

        public uint CurrentFrameNumber
        {
            get;
            private set;
        }

        public Texture MainRenderTargetTexture
        {
            get;
            private set;
        }

        public GraphicsSceneRenderer GraphicsSceneRenderer { get; }
        public Graphics2DRenderer Graphics2DRenderer { get; }

        public Vector2 GetRenderSize()
        {
            return this.graphicsService.GetRenderSize();
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(int length, GraphicsResourceType resourceType = GraphicsResourceType.Static, string? debugName = null) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId, sizeInBytes, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            uint? graphicsBufferId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                graphicsBufferId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId2.Value, sizeInBytes, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }
            }

            uint? graphicsBufferId3 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                graphicsBufferId3 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId3.Value, sizeInBytes, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, graphicsBufferId3, sizeInBytes, resourceType);
        }

        // TODO: Add additional parameters (format, depth, mipLevels, etc.<)
        public Texture CreateTexture(TextureFormat textureFormat, int width, int height, bool isRenderTarget = false, GraphicsResourceType resourceType = GraphicsResourceType.Static, string? debugName = null)
        {
            var textureId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateTexture(textureId, (GraphicsTextureFormat)(int)textureFormat, width, height, isRenderTarget, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            uint? textureId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                textureId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateTexture(textureId2.Value, (GraphicsTextureFormat)(int)textureFormat, width, height, isRenderTarget, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            uint? textureId3 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                textureId3 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateTexture(textureId3.Value, (GraphicsTextureFormat)(int)textureFormat, width, height, isRenderTarget, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            return new Texture(this, textureId, textureId2, textureId3, width, height, resourceType);
        }

        public void RemoveTexture(Texture texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.graphicsService.RemoveTexture(texture.GraphicsResourceSystemId);

            if (texture.ResourceType == GraphicsResourceType.Dynamic)
            {
                if (texture.GraphicsResourceSystemId2 != null)
                {
                    this.graphicsService.RemoveTexture(texture.GraphicsResourceSystemId2.Value);
                }
                
                if (texture.GraphicsResourceSystemId3 != null)
                {
                    this.graphicsService.RemoveTexture(texture.GraphicsResourceSystemId3.Value);
                }
            }
        }

        internal Shader CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, bool useDepthBuffer, string? debugName = null)
        {
            var shaderId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateShader(shaderId, computeShaderFunction, shaderByteCode, useDepthBuffer, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the shader resource.");
            }

            return new Shader(shaderId);
        }

        internal void RemoveShader(Shader shader)
        {
            this.graphicsService.RemoveShader(shader.ShaderId);
        }

        public CommandList CreateCopyCommandList(string? debugName = null, bool createNewCommandBuffer = false)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateCopyCommandList(commandListId, debugName, createNewCommandBuffer);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the copy command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Copy);
        }

        public void ExecuteCopyCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Copy)
            {
                throw new InvalidOperationException("The specified command list is not a copy command list.");
            }

            this.graphicsService.ExecuteCopyCommandList(commandList.Id);
        }

        public void UploadDataToGraphicsBuffer<T>(CommandList commandList, GraphicsBuffer graphicsBuffer, ReadOnlySpan<T> data) where T : struct
        {
            // TODO: Do something for memory alignement of data in the shaders?
            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToGraphicsBuffer(commandList.Id, graphicsBuffer.GraphicsResourceId, rawData);
        }

        public void UploadDataToTexture<T>(CommandList commandList, Texture texture, ReadOnlySpan<T> data) where T : struct
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToTexture(commandList.Id, texture.GraphicsResourceId, texture.Width, texture.Height, rawData);
        }

        public void ResetIndirectCommandList(CommandList commandList, CommandList indirectCommandList, int maxCommandCount)
        {
            if (indirectCommandList.Type != CommandListType.Indirect)
            {
                throw new InvalidOperationException("The specified command list is not an indirect command list.");
            }
            
            this.graphicsService.ResetIndirectCommandList(commandList.Id, indirectCommandList.Id, maxCommandCount);
        }

        public void OptimizeIndirectCommandList(CommandList commandList, CommandList indirectCommandList, int maxCommandCount)
        {
            if (indirectCommandList.Type != CommandListType.Indirect)
            {
                throw new InvalidOperationException("The specified command list is not an indirect command list.");
            }

            this.graphicsService.OptimizeIndirectCommandList(commandList.Id, indirectCommandList.Id, maxCommandCount);
        }

        public CommandList CreateComputeCommandList(string? debugName = null, bool createNewCommandBuffer = false)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateComputeCommandList(commandListId, debugName, createNewCommandBuffer);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the compute command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Compute);
        }

        public void ExecuteComputeCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Compute)
            {
                throw new InvalidOperationException("The specified command list is not a compute command list.");
            }

            this.graphicsService.ExecuteComputeCommandList(commandList.Id);
        }

        public void DispatchThreadGroups(CommandList commandList, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            if (commandList.Type != CommandListType.Compute)
            {
                throw new InvalidOperationException("The specified command list is not a compute command list.");
            }

            this.graphicsService.DispatchThreadGroups(commandList.Id, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
            this.dispatchCount++;
        }

        public CommandList CreateRenderCommandList(RenderPassDescriptor renderPassDescriptor, string? debugName = null, bool createNewCommandBuffer = false)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateRenderCommandList(commandListId, new GraphicsRenderPassDescriptor(renderPassDescriptor), debugName, createNewCommandBuffer);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the render command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Render);
        }

        public void ExecuteRenderCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.ExecuteRenderCommandList(commandList.Id);
        }

        public CommandList CreateIndirectCommandList(int maxCommandCount, string? debugName = null)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateIndirectCommandList(commandListId, maxCommandCount, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the indirect command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Indirect);
        }

        public void SetShader(CommandList commandList, Shader shader)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            this.graphicsService.SetShader(commandList.Id, shader.ShaderId);
        }

        public void SetShaderBuffer(CommandList commandList, GraphicsBuffer graphicsBuffer, int slot, int index = 0)
        {
            this.graphicsService.SetShaderBuffer(commandList.Id, graphicsBuffer.GraphicsResourceId, slot, index);
        }

        public void SetShaderBuffers(CommandList commandList, ReadOnlySpan<GraphicsBuffer> graphicsBuffers, int slot, int index = 0)
        {
            if (graphicsBuffers == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffers));
            }

            var graphicsBufferIdsList = new uint[graphicsBuffers.Length];

            for (var i = 0; i < graphicsBuffers.Length; i++)
            {
                graphicsBufferIdsList[i] = graphicsBuffers[i].GraphicsResourceId;
            }

            this.graphicsService.SetShaderBuffers(commandList.Id, graphicsBufferIdsList.AsSpan(), slot, index);
        }

        public void SetShaderTexture(CommandList commandList, Texture texture, int slot, int index = 0)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.graphicsService.SetShaderTexture(commandList.Id, texture.GraphicsResourceId, slot, index);
        }

        public void SetShaderTextures(CommandList commandList, ReadOnlySpan<Texture> textures, int slot, int index = 0)
        {
            if (textures == null)
            {
                throw new ArgumentNullException(nameof(textures));
            }

            var textureIdsList = new uint[textures.Length];

            for (var i = 0; i < textures.Length; i++)
            {
                textureIdsList[i] = textures[i].GraphicsResourceId;
            }

            this.graphicsService.SetShaderTextures(commandList.Id, textureIdsList.AsSpan(), slot, index);
        }

        public void SetShaderIndirectCommandList(CommandList commandList, CommandList indirectCommandList, int slot, int index = 0)
        {
            if (indirectCommandList.Type != CommandListType.Indirect)
            {
                throw new InvalidOperationException("The specified command list is not an indirect command list.");
            }

            this.graphicsService.SetShaderIndirectCommandList(commandList.Id, indirectCommandList.Id, slot, index);
        }

        public void ExecuteIndirectCommandList(CommandList commandList, CommandList indirectCommandList, int maxCommandCount)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            if (indirectCommandList.Type != CommandListType.Indirect)
            {
                throw new InvalidOperationException("The specified command list is not an indirect command list.");
            }

            this.graphicsService.ExecuteIndirectCommandList(commandList.Id, indirectCommandList.Id, maxCommandCount);
        }

        public void SetIndexBuffer(CommandList commandList, GraphicsBuffer indexBuffer)
        {
            this.graphicsService.SetIndexBuffer(commandList.Id, indexBuffer.GraphicsResourceId);
        }

        public void DrawGeometryInstances(CommandList commandList, GeometryInstance geometryInstance, int instanceCount, int baseInstanceId)
        {
            if (geometryInstance.IndexCount == 0)
            {
                throw new InvalidOperationException("Index count must non-zero.");
            }

            this.SetShaderBuffer(commandList, geometryInstance.GeometryPacket.VertexBuffer, 0);
            this.SetIndexBuffer(commandList, geometryInstance.GeometryPacket.IndexBuffer);

            this.DrawIndexedPrimitives(commandList, 
                                        geometryInstance.PrimitiveType, 
                                        geometryInstance.StartIndex, 
                                        geometryInstance.IndexCount, 
                                        instanceCount,
                                        baseInstanceId);
        }

        public void DrawIndexedPrimitives(CommandList commandList, GeometryPrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.DrawIndexedPrimitives(commandList.Id, 
                                                (GraphicsPrimitiveType)(int)primitiveType, 
                                                startIndex, 
                                                indexCount,
                                                instanceCount,
                                                baseInstanceId);

            this.drawCount++;
        }

        public void PresentScreenBuffer()
        {
            // TODO: Is there a way to load the final render target texture and to store it directly in the hardware?
            this.internal2DRenderer.PreUpdate();
            this.internal2DRenderer.DrawRectangleSurface(Vector2.Zero, this.GetRenderSize(), this.MainRenderTargetTexture);
            this.internal2DRenderer.CopyDataToGpu();

            var renderPassDescriptor = new GraphicsRenderPassDescriptor(null, null, null, false, false, true);

            var commandListId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateRenderCommandList(commandListId, renderPassDescriptor, "PresentRenderCommandList", false);

            this.internal2DRenderer.Render(new CommandList(commandListId, CommandListType.Render));
            this.graphicsService.ExecuteRenderCommandList(commandListId);

            this.graphicsService.PresentScreenBuffer();

            // TODO: A modulo here with Int.MaxValue
            this.CurrentFrameNumber++;
            this.drawCount = 0;
            this.dispatchCount = 0;
        }

        internal void Render()
        {
            var frameSize = graphicsService.GetRenderSize();

            if (frameSize != this.currentFrameSize)
            {
                Logger.WriteMessage("Recreating final render target");
                this.currentFrameSize = frameSize;
                
                RemoveTexture(this.MainRenderTargetTexture);
                this.MainRenderTargetTexture = CreateTexture(TextureFormat.Bgra8UnormSrgb, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, true, GraphicsResourceType.Dynamic, "MainRenderTarget");
            }

            this.GraphicsSceneRenderer.CopyDataToGpuAndRender();

            DrawDebugMessages();
            this.Graphics2DRenderer.CopyDataToGpu();
            this.Graphics2DRenderer.Render();

            this.PresentScreenBuffer();
            this.stopwatch.Restart();
        }

        private void DrawDebugMessages()
        {
            var frameDuration = (float)this.stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
            
            if (this.globalStopwatch.ElapsedMilliseconds > 1000)
            {
                framePerSeconds = (int)(this.CurrentFrameNumber - startMeasureFrameNumber - 1);
                this.globalStopwatch.Restart();
                this.startMeasureFrameNumber = this.CurrentFrameNumber;
            }

            this.Graphics2DRenderer.DrawText($"Cpu Frame duration: {frameDuration.ToString("0.00")} ms - FPS: {framePerSeconds}", new Vector2(10, 10));
            this.Graphics2DRenderer.DrawText($"Gpu Dispatch Count: {this.dispatchCount}", new Vector2(10, 50));
            this.Graphics2DRenderer.DrawText($"Gpu Draw Count: {this.drawCount}", new Vector2(10, 90));
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            resourcesManager.AddResourceLoader(new TextureResourceLoader(resourcesManager, this));
            resourcesManager.AddResourceLoader(new FontResourceLoader(resourcesManager, this));
            resourcesManager.AddResourceLoader(new ShaderResourceLoader(resourcesManager, this));
            resourcesManager.AddResourceLoader(new MaterialResourceLoader(resourcesManager, this));
            resourcesManager.AddResourceLoader(new MeshResourceLoader(resourcesManager, this));
        }

        private uint GetNextGraphicsResourceId()
        {
            uint result = 0;

            lock (syncObject)
            {
                result = ++this.currentGraphicsResourceId;
            }

            return result;
        }
    }
}
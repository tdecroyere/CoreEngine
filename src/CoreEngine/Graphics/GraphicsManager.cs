using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class GraphicsManager : SystemManager
    {
        private readonly IGraphicsService graphicsService;

        private static object syncObject = new object();
        private uint currentGraphicsResourceId;
        private Vector2 currentFrameSize;
        private Stopwatch stopwatch;
        private int cpuDrawCount;
        private int cpuDispatchCount;
        private Stopwatch globalStopwatch;
        private uint startMeasureFrameNumber;
        private int framePerSeconds = 0;
        private ulong allocatedGpuMemory = 0;
        private int gpuMemoryUploaded = 0;
        private int gpuMemoryUploadedPerSeconds = 0;
        private string graphicsAdapterName;

        private Shader computeDirectTransferShader;
        private Dictionary<uint, GraphicsRenderPassDescriptor> renderPassDescriptors;

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

            this.graphicsService = graphicsService;
            this.currentGraphicsResourceId = 0;
            this.cpuDrawCount = 0;
            this.cpuDispatchCount = 0;
            this.renderPassDescriptors = new Dictionary<uint, GraphicsRenderPassDescriptor>();
            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
            this.globalStopwatch = new Stopwatch();
            this.globalStopwatch.Start();

            var graphicsAdapterName = this.graphicsService.GetGraphicsAdapterName();
            this.graphicsAdapterName = (graphicsAdapterName != null) ? graphicsAdapterName : "Unknow Graphics Adapter";

            InitResourceLoaders(resourcesManager);

            this.computeDirectTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeDirectTransfer.shader");

            this.GraphicsSceneRenderer = new GraphicsSceneRenderer(this, graphicsSceneQueue, resourcesManager);
            this.Graphics2DRenderer = new Graphics2DRenderer(this, resourcesManager);

            this.currentFrameSize = graphicsService.GetRenderSize();
            this.MainRenderTargetTexture = CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, true, GraphicsResourceType.Dynamic, "MainRenderTarget");
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
        internal int GeometryInstancesCount { get; set; }

        public Vector2 GetRenderSize()
        {
            return this.graphicsService.GetRenderSize();
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(int length, GraphicsResourceType resourceType = GraphicsResourceType.Static, string? debugName = null) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId, sizeInBytes, debugName);
            this.allocatedGpuMemory += (ulong)sizeInBytes;

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            uint? graphicsBufferId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                graphicsBufferId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId2.Value, sizeInBytes, debugName);
                this.allocatedGpuMemory += (ulong)sizeInBytes;

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
                this.allocatedGpuMemory += (ulong)sizeInBytes;

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, graphicsBufferId3, sizeInBytes, resourceType);
        }

        // TODO: Add additional parameters (format, depth, mipLevels, etc.<)
        public Texture CreateTexture(TextureFormat textureFormat, int width, int height, int mipLevels, int multisampleCount = 1, bool isRenderTarget = false, GraphicsResourceType resourceType = GraphicsResourceType.Static, string? debugName = null)
        {
            var textureId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateTexture(textureId, (GraphicsTextureFormat)(int)textureFormat, width, height, mipLevels, multisampleCount, isRenderTarget, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            uint? textureId2 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                textureId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateTexture(textureId2.Value, (GraphicsTextureFormat)(int)textureFormat, width, height, mipLevels, multisampleCount, isRenderTarget, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            uint? textureId3 = null;

            if (resourceType == GraphicsResourceType.Dynamic)
            {
                textureId3 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateTexture(textureId3.Value, (GraphicsTextureFormat)(int)textureFormat, width, height, mipLevels, multisampleCount, isRenderTarget, debugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            var texture = new Texture(this, textureId, textureId2, textureId3, textureFormat, width, height, mipLevels, multisampleCount, resourceType);
            var textureSizeInBytes = ComputeTextureSizeInBytes(texture);
            
            if (resourceType == GraphicsResourceType.Static)
            {
                this.allocatedGpuMemory += (ulong)textureSizeInBytes;
            }
        
            else
            {
                this.allocatedGpuMemory += (ulong)textureSizeInBytes * 3;
            }
            
            return texture;
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

            var textureSizeInBytes = ComputeTextureSizeInBytes(texture);
            
            if (texture.ResourceType == GraphicsResourceType.Static)
            {
                this.allocatedGpuMemory -= (ulong)textureSizeInBytes;
            }
        
            else
            {
                this.allocatedGpuMemory -= (ulong)textureSizeInBytes * 3;
            }
        }

        internal Shader CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string? debugName = null)
        {
            var shaderId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateShader(shaderId, computeShaderFunction, shaderByteCode, debugName);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the shader resource.");
            }

            return new Shader(debugName, shaderId);
        }

        internal void RemoveShader(Shader shader)
        {
            foreach (var pipelineState in shader.PipelineStates.Values)
            {
                this.graphicsService.RemovePipelineState(pipelineState.PipelineStateId);
            }

            shader.PipelineStates.Clear();
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
            if (data.Length == 0)
            {
                return;
            }

            // TODO: Do something for memory alignement of data in the shaders?
            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToGraphicsBuffer(commandList.Id, graphicsBuffer.GraphicsResourceId, rawData);
            this.gpuMemoryUploaded += rawData.Length;
        }

        public void UploadDataToTexture<T>(CommandList commandList, Texture texture, int width, int height, int mipLevel, ReadOnlySpan<T> data) where T : struct
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (data.Length == 0)
            {
                return;
            }

            var rawData = MemoryMarshal.Cast<T, byte>(data);
            this.graphicsService.UploadDataToTexture(commandList.Id, texture.GraphicsResourceId, (GraphicsTextureFormat)texture.TextureFormat, width, height, mipLevel, rawData);
            this.gpuMemoryUploaded += rawData.Length;
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

        public void DispatchThreads(CommandList commandList, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            if (commandList.Type != CommandListType.Compute)
            {
                throw new InvalidOperationException("The specified command list is not a compute command list.");
            }

            this.graphicsService.DispatchThreads(commandList.Id, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
            this.cpuDispatchCount++;
        }

        public CommandList CreateRenderCommandList(RenderPassDescriptor renderPassDescriptor, string? debugName = null, bool createNewCommandBuffer = false)
        {
            var graphicsRenderPassDescriptor = new GraphicsRenderPassDescriptor(renderPassDescriptor);
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateRenderCommandList(commandListId, graphicsRenderPassDescriptor, debugName, createNewCommandBuffer);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the render command list resource.");
            }

            this.renderPassDescriptors.Add(commandListId, graphicsRenderPassDescriptor);

            return new CommandList(commandListId, CommandListType.Render);
        }

        public void ExecuteRenderCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.ExecuteRenderCommandList(commandList.Id);
            this.renderPassDescriptors.Remove(commandList.Id);
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

            if (!shader.IsLoaded)
            {
                return;
            }

            this.graphicsService.SetShader(commandList.Id, shader.ShaderId);

            var renderPassDescriptor = new GraphicsRenderPassDescriptor();

            if (this.renderPassDescriptors.ContainsKey(commandList.Id))
            {
                renderPassDescriptor = this.renderPassDescriptors[commandList.Id];
            }

            if (!shader.PipelineStates.ContainsKey(renderPassDescriptor))
            {
                Logger.WriteMessage($"Create Pipeline State for shader {shader.ShaderId}...");

                var pipelineStateId = GetNextGraphicsResourceId();
                var result = this.graphicsService.CreatePipelineState(pipelineStateId, shader.ShaderId, renderPassDescriptor, shader.DebugName);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the pipelinestate object.");
                }

                shader.PipelineStates.Add(renderPassDescriptor, new PipelineState(pipelineStateId));
            }

            this.graphicsService.SetPipelineState(commandList.Id, shader.PipelineStates[renderPassDescriptor].PipelineStateId);
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

        public void SetShaderTexture(CommandList commandList, Texture texture, int slot, bool isReadOnly = true, int index = 0)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.graphicsService.SetShaderTexture(commandList.Id, texture.GraphicsResourceId, slot, isReadOnly, index);
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

            this.cpuDrawCount++;
        }

        public void DrawPrimitives(CommandList commandList, GeometryPrimitiveType primitiveType, int startVertex, int vertexCount)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.DrawPrimitives(commandList.Id, 
                                                (GraphicsPrimitiveType)(int)primitiveType, 
                                                startVertex, 
                                                vertexCount);

            this.cpuDrawCount++;
        }

        public void PresentScreenBuffer()
        {
            // TODO: Use a compute shader
            var renderPassDescriptor = new RenderPassDescriptor(null, null, DepthBufferOperation.None, true);
            var renderCommandList = CreateRenderCommandList(renderPassDescriptor, "PresentRenderCommandList", false);

            SetShader(renderCommandList, this.computeDirectTransferShader);
            SetShaderTexture(renderCommandList, this.MainRenderTargetTexture, 0);
            DrawPrimitives(renderCommandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            ExecuteRenderCommandList(renderCommandList);
            this.graphicsService.PresentScreenBuffer();

            // TODO: A modulo here with Int.MaxValue
            this.CurrentFrameNumber++;
            this.cpuDrawCount = 0;
            this.cpuDispatchCount = 0;
        }

        internal void Render()
        {
            var frameSize = graphicsService.GetRenderSize();

            if (frameSize != this.currentFrameSize)
            {
                Logger.WriteMessage("Recreating final render target");
                this.currentFrameSize = frameSize;
                
                RemoveTexture(this.MainRenderTargetTexture);
                this.MainRenderTargetTexture = CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, true, GraphicsResourceType.Dynamic, "MainRenderTarget");
            }

            this.GraphicsSceneRenderer.Render();

            DrawDebugMessages();
            this.Graphics2DRenderer.Render();

            this.PresentScreenBuffer();
            this.stopwatch.Restart();
        }

        private void DrawDebugMessages()
        {
            // TODO: Verify all timings !

            var frameDuration = (float)this.stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
            
            if (this.globalStopwatch.ElapsedMilliseconds > 1000)
            {
                this.framePerSeconds = (int)(this.CurrentFrameNumber - startMeasureFrameNumber - 1);
                this.globalStopwatch.Restart();
                this.startMeasureFrameNumber = this.CurrentFrameNumber;

                this.gpuMemoryUploadedPerSeconds = this.gpuMemoryUploaded;
                this.gpuMemoryUploaded = 0;
            }

            var renderSize = GetRenderSize();
            var gpuExecutionTime = this.graphicsService.GetGpuExecutionTime(this.CurrentFrameNumber - 1);

            this.Graphics2DRenderer.DrawText($"{this.graphicsAdapterName} - {renderSize.X}x{renderSize.Y} - FPS: {framePerSeconds}", new Vector2(10, 10));
            this.Graphics2DRenderer.DrawText($"Gpu Frame Duration: {gpuExecutionTime.ToString("0.00", CultureInfo.InvariantCulture)} ms", new Vector2(10, 50));
            this.Graphics2DRenderer.DrawText($"    Allocated Memory: {BytesToMegaBytes(this.allocatedGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)} MB", new Vector2(10, 90));
            this.Graphics2DRenderer.DrawText($"    Memory Bandwidth: {BytesToMegaBytes((ulong)this.gpuMemoryUploadedPerSeconds).ToString("0.00", CultureInfo.InvariantCulture)} MB/s", new Vector2(10, 130));
            this.Graphics2DRenderer.DrawText($"Cpu Frame Duration: {frameDuration.ToString("0.00", CultureInfo.InvariantCulture)} ms", new Vector2(10, 170));
            this.Graphics2DRenderer.DrawText($"    Active GeometryInstances: {this.GeometryInstancesCount}", new Vector2(10, 210));
            this.Graphics2DRenderer.DrawText($"    DispatchThreads: {this.cpuDispatchCount}", new Vector2(10, 250));
            this.Graphics2DRenderer.DrawText($"    DrawIndexedPrimitives: {this.cpuDrawCount + 1}", new Vector2(10, 290));
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

        private static int ComputeTextureSizeInBytes(Texture texture)
        {
            var pixelSizeInBytes = 4;

            if (texture.TextureFormat == TextureFormat.Rgba16Float)
            {
                pixelSizeInBytes = 8;
            }

            var textureMemory = texture.Width * texture.Height * pixelSizeInBytes * texture.MultiSampleCount;
            var textureWidth = texture.Width;
            var textureHeight = texture.Height;

            for (var i = 1; i < texture.MipLevels; i++)
            {
                textureWidth = (textureWidth > 1) ? textureWidth / 2 : 1;
                textureHeight = (textureHeight > 1) ? textureHeight / 2 : 1;

                // TODO: Change the calculation based on the texture format
                textureMemory += textureWidth * textureHeight * pixelSizeInBytes * texture.MultiSampleCount;
            }

            return textureMemory;
        }

        private static float BytesToMegaBytes(ulong value)
        {
            return (float)value / 1024 / 1024;
        }
    }
}
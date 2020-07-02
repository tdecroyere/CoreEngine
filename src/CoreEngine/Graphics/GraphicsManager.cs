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
    public readonly struct GpuTiming
    {
        public GpuTiming(string name, double startTiming, double endTiming)
        {
            this.Name = name;
            this.StartTiming = startTiming;
            this.EndTiming = endTiming;
        }

        public string Name { get; }
        public double StartTiming { get; }
        public double EndTiming { get; }
    }

    public class GraphicsManager : SystemManager
    {
        internal readonly IGraphicsService graphicsService;

        private static object syncObject = new object();
        private uint currentGraphicsResourceId;
        
        // TODO: Remove internal
        internal string graphicsAdapterName;
        internal ulong allocatedGpuMemory = 0;
        internal int gpuMemoryUploaded = 0;
        internal int cpuDrawCount;
        internal int cpuDispatchCount;

        private Dictionary<uint, GraphicsRenderPassDescriptor> renderPassDescriptors;
        internal List<GpuTiming> gpuTimings = new List<GpuTiming>();

        public GraphicsManager(IGraphicsService graphicsService, ResourcesManager resourcesManager)
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

            var graphicsAdapterName = this.graphicsService.GetGraphicsAdapterName();
            this.graphicsAdapterName = (graphicsAdapterName != null) ? graphicsAdapterName : "Unknown Graphics Adapter";

            this.currentGraphicsResourceId = 0;
            
            this.renderPassDescriptors = new Dictionary<uint, GraphicsRenderPassDescriptor>();

            InitResourceLoaders(resourcesManager);
        }

        // TODO: Move that method
        public uint CurrentFrameNumber
        {
            get;
            set;
        }

        public Vector2 GetRenderSize()
        {
            return this.graphicsService.GetRenderSize();
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(int length, bool isStatic, bool isWriteOnly, string label) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId, sizeInBytes, isWriteOnly, label);
            this.allocatedGpuMemory += (ulong)sizeInBytes;

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            uint? graphicsBufferId2 = null;

            if (!isStatic)
            {
                graphicsBufferId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId2.Value, sizeInBytes, isWriteOnly, label);
                this.allocatedGpuMemory += (ulong)sizeInBytes;

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }
            }

            return new GraphicsBuffer(this, graphicsBufferId, graphicsBufferId2, sizeInBytes, isStatic, label);
        }

        public Texture CreateTexture(TextureFormat textureFormat, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isRenderTarget, bool isStatic, string label)
        {
            var textureId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateTexture(textureId, (GraphicsTextureFormat)(int)textureFormat, width, height, faceCount, mipLevels, multisampleCount, isRenderTarget, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            uint? textureId2 = null;

            if (!isStatic)
            {
                textureId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateTexture(textureId2.Value, (GraphicsTextureFormat)(int)textureFormat, width, height, faceCount, mipLevels, multisampleCount, isRenderTarget, label);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }
            }

            var texture = new Texture(this, textureId, textureId2, textureFormat, width, height, faceCount, mipLevels, multisampleCount, isStatic, label);
            var textureSizeInBytes = ComputeTextureSizeInBytes(texture);
            
            if (isStatic)
            {
                this.allocatedGpuMemory += (ulong)textureSizeInBytes;
            }
        
            else
            {
                this.allocatedGpuMemory += (ulong)textureSizeInBytes * 2;
            }
            
            return texture;
        }

        public void DeleteTexture(Texture texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.graphicsService.DeleteTexture(texture.GraphicsResourceSystemId);

            if (!texture.IsStatic)
            {
                if (texture.GraphicsResourceSystemId2 != null)
                {
                    this.graphicsService.DeleteTexture(texture.GraphicsResourceSystemId2.Value);
                }
            }

            var textureSizeInBytes = ComputeTextureSizeInBytes(texture);
            
            if (texture.IsStatic)
            {
                this.allocatedGpuMemory -= (ulong)textureSizeInBytes;
            }
        
            else
            {
                this.allocatedGpuMemory -= (ulong)textureSizeInBytes * 2;
            }
        }

        public void ResizeTexture(Texture texture, int width, int height)
        {
            // TODO: Take into account all parameters

            this.DeleteTexture(texture);
            var newTexture = this.CreateTexture(texture.TextureFormat, width, height, 1, 1, texture.MultiSampleCount, true, texture.IsStatic, texture.Label);
            
            texture.GraphicsResourceSystemId = newTexture.GraphicsResourceSystemId;
            texture.GraphicsResourceSystemId2 = newTexture.GraphicsResourceSystemId2;
            texture.Width = width;
            texture.Height = height;
        }

        public IndirectCommandBuffer CreateIndirectCommandBuffer(int maxCommandCount, bool isStatic, string label)
        {
            var graphicsResourceId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateIndirectCommandBuffer(graphicsResourceId, maxCommandCount, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the indirect command buffer resource.");
            }

            uint? graphicsResourceId2 = null;

            if (!isStatic)
            {
                graphicsResourceId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateIndirectCommandBuffer(graphicsResourceId2.Value, maxCommandCount, label);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the indirect command buffer resource.");
                }
            }

            return new IndirectCommandBuffer(this, graphicsResourceId, graphicsResourceId2, maxCommandCount, isStatic, label);
        }

        internal Shader CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string label)
        {
            var shaderId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateShader(shaderId, computeShaderFunction, shaderByteCode, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the shader resource.");
            }

            return new Shader(label, shaderId);
        }

        internal void DeleteShader(Shader shader)
        {
            foreach (var pipelineState in shader.PipelineStates.Values)
            {
                this.graphicsService.DeletePipelineState(pipelineState.PipelineStateId);
            }

            shader.PipelineStates.Clear();
            this.graphicsService.DeleteShader(shader.ShaderId);
        }

        public CommandBuffer CreateCommandBuffer(string label)
        {
            var graphicsResourceId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateCommandBuffer(graphicsResourceId, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the command buffer resource.");
            }


            var graphicsResourceId2 = GetNextGraphicsResourceId();
            result = this.graphicsService.CreateCommandBuffer(graphicsResourceId2, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the command buffer resource.");
            }

            return new CommandBuffer(this, graphicsResourceId, graphicsResourceId2, label);
        }

        public void DeleteCommandBuffer(CommandBuffer commandBuffer)
        {
            this.graphicsService.DeleteCommandBuffer(commandBuffer.GraphicsResourceSystemId);

            if (commandBuffer.GraphicsResourceSystemId2 != null)
            {
                this.graphicsService.DeleteCommandBuffer(commandBuffer.GraphicsResourceSystemId2.Value);
            }
        }

        public void ExecuteCommandBuffer(CommandBuffer commandBuffer)
        {
            graphicsService.ExecuteCommandBuffer(commandBuffer.GraphicsResourceId);
        }

        // TODO: Make it private and automatically call it when changing frames
        public void ResetCommandBuffer(CommandBuffer commandBuffer)
        {
            var commandBufferStatus = this.graphicsService.GetCommandBufferStatus(commandBuffer.GraphicsResourceId);

            if (commandBufferStatus == null || (commandBufferStatus != null && commandBufferStatus.Value.State != GraphicsCommandBufferState.Created))
            {
                if (commandBufferStatus != null)
                {
                    if (commandBufferStatus.Value.State == GraphicsCommandBufferState.Completed)
                    {
                        this.gpuTimings.Add(new GpuTiming(commandBuffer.Label, commandBufferStatus.Value.ExecutionStartTime, commandBufferStatus.Value.ExecutionEndTime));
                    }

                    else
                    {
                        Logger.WriteMessage($"CommandBuffer '{commandBuffer.Label}' has not completed.");
                    }
                }
            }
            
            this.graphicsService.ResetCommandBuffer(commandBuffer.GraphicsResourceId);
        }

        public CommandList CreateCopyCommandList(CommandBuffer commandBuffer, string label)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateCopyCommandList(commandListId, commandBuffer.GraphicsResourceId, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the copy command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Copy);
        }

        public void CommitCopyCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Copy)
            {
                throw new InvalidOperationException("The specified command list is not a copy command list.");
            }

            this.graphicsService.CommitCopyCommandList(commandList.Id);
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

        public void CopyGraphicsBufferDataToCpu(CommandList commandList, GraphicsBuffer graphicsBuffer, int length)
        {
            this.graphicsService.CopyGraphicsBufferDataToCpu(commandList.Id, graphicsBuffer.GraphicsResourceId, length);
        }

        public ReadOnlySpan<T> ReadGraphicsBufferData<T>(GraphicsBuffer graphicsBuffer) where T : struct
        {
            var rawData = new byte[graphicsBuffer.Length].AsSpan();
            this.graphicsService.ReadGraphicsBufferData(graphicsBuffer.GraphicsResourceId, rawData);
            return MemoryMarshal.Cast<byte, T>(rawData);
        }

        public void UploadDataToTexture<T>(CommandList commandList, Texture texture, int width, int height, int slice, int mipLevel, ReadOnlySpan<T> data) where T : struct
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
            this.graphicsService.UploadDataToTexture(commandList.Id, texture.GraphicsResourceId, (GraphicsTextureFormat)texture.TextureFormat, width, height, slice, mipLevel, rawData);
            this.gpuMemoryUploaded += rawData.Length;
        }

        public void ResetIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandList, int maxCommandCount)
        {
            this.graphicsService.ResetIndirectCommandList(commandList.Id, indirectCommandList.GraphicsResourceId, maxCommandCount);
        }

        public void OptimizeIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandList, int maxCommandCount)
        {
            this.graphicsService.OptimizeIndirectCommandList(commandList.Id, indirectCommandList.GraphicsResourceId, maxCommandCount);
        }

        public CommandList CreateComputeCommandList(CommandBuffer commandBuffer, string label)
        {
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateComputeCommandList(commandListId, commandBuffer.GraphicsResourceId, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the compute command list resource.");
            }

            return new CommandList(commandListId, CommandListType.Compute);
        }

        public void CommitComputeCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Compute)
            {
                throw new InvalidOperationException("The specified command list is not a compute command list.");
            }

            this.graphicsService.CommitComputeCommandList(commandList.Id);
        }

        public Vector3 DispatchThreads(CommandList commandList, uint threadCountX, uint threadCountY, uint threadCountZ)
        {
            if (commandList.Type != CommandListType.Compute)
            {
                throw new InvalidOperationException("The specified command list is not a compute command list.");
            }

            if (threadCountX == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCountX));
            }

            if (threadCountY == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCountY));
            }

            if (threadCountZ == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadCountZ));
            }

            this.cpuDispatchCount++;
            return this.graphicsService.DispatchThreads(commandList.Id, threadCountX, threadCountY, threadCountZ);
        }

        public CommandList CreateRenderCommandList(CommandBuffer commandBuffer, RenderPassDescriptor renderPassDescriptor, string label)
        {
            var graphicsRenderPassDescriptor = new GraphicsRenderPassDescriptor(renderPassDescriptor);
            var commandListId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateRenderCommandList(commandListId, commandBuffer.GraphicsResourceId, graphicsRenderPassDescriptor, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the render command list resource.");
            }

            this.renderPassDescriptors.Add(commandListId, graphicsRenderPassDescriptor);

            return new CommandList(commandListId, CommandListType.Render);
        }

        public void CommitRenderCommandList(CommandList commandList)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.CommitRenderCommandList(commandList.Id);
            this.renderPassDescriptors.Remove(commandList.Id);
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
                var result = this.graphicsService.CreatePipelineState(pipelineStateId, shader.ShaderId, renderPassDescriptor, shader.Label);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the pipelinestate object.");
                }

                shader.PipelineStates.Add(renderPassDescriptor, new PipelineState(pipelineStateId));
            }

            this.graphicsService.SetPipelineState(commandList.Id, shader.PipelineStates[renderPassDescriptor].PipelineStateId);
        }

        public void SetShaderBuffer(CommandList commandList, GraphicsBuffer graphicsBuffer, int slot, bool isReadOnly = true, int index = 0)
        {
            this.graphicsService.SetShaderBuffer(commandList.Id, graphicsBuffer.GraphicsResourceId, slot, isReadOnly, index);
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

        public void SetShaderIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandBuffer, int slot, int index = 0)
        {
            this.graphicsService.SetShaderIndirectCommandList(commandList.Id, indirectCommandBuffer.GraphicsResourceId, slot, index);
        }

        public void SetShaderIndirectCommandBuffers(CommandList commandList, ReadOnlySpan<IndirectCommandBuffer> indirectCommandBuffers, int slot, int index = 0)
        {
            if (indirectCommandBuffers == null)
            {
                throw new ArgumentNullException(nameof(indirectCommandBuffers));
            }

            var list = new uint[indirectCommandBuffers.Length];

            for (var i = 0; i < indirectCommandBuffers.Length; i++)
            {
                list[i] = indirectCommandBuffers[i].GraphicsResourceId;
            }

            this.graphicsService.SetShaderIndirectCommandLists(commandList.Id, list.AsSpan(), slot, index);
        }

        public void ExecuteIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandBuffer, int maxCommandCount)
        {
            if (commandList.Type != CommandListType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.ExecuteIndirectCommandBuffer(commandList.Id, indirectCommandBuffer.GraphicsResourceId, maxCommandCount);
        }

        public void SetIndexBuffer(CommandList commandList, GraphicsBuffer indexBuffer)
        {
            this.graphicsService.SetIndexBuffer(commandList.Id, indexBuffer.GraphicsResourceId);
        }

        public void DrawIndexedPrimitives(CommandList commandList, PrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
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

        public void DrawPrimitives(CommandList commandList, PrimitiveType primitiveType, int startVertex, int vertexCount)
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

        public void WaitForCommandList(CommandList commandList, CommandList commandListToWait)
        {
            this.graphicsService.WaitForCommandList(commandList.Id, commandListToWait.Id);
        }

        public void WaitForCommandLists(CommandList commandList, ReadOnlySpan<CommandList> commandListsToWait)
        {
            for (var i = 0; i < commandListsToWait.Length; i++)
            {
                this.graphicsService.WaitForCommandList(commandList.Id, commandListsToWait[i].Id);
            }
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            resourcesManager.AddResourceLoader(new TextureResourceLoader(resourcesManager, this));
            resourcesManager.AddResourceLoader(new ShaderResourceLoader(resourcesManager, this));
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
            var isBlockCompression = false;

            if (texture.TextureFormat == TextureFormat.Rgba16Float || texture.TextureFormat == TextureFormat.Rgba16Unorm)
            {
                pixelSizeInBytes = 8;
            }

            else if (texture.TextureFormat == TextureFormat.Rgba32Float)
            {
                pixelSizeInBytes = 16;
            }

            else if (texture.TextureFormat == TextureFormat.BC1Srgb || texture.TextureFormat == TextureFormat.BC4)
            {
                pixelSizeInBytes = 8;
                isBlockCompression = true;
            }

            else if (texture.TextureFormat == TextureFormat.BC2Srgb || texture.TextureFormat == TextureFormat.BC3Srgb || texture.TextureFormat == TextureFormat.BC5 || texture.TextureFormat == TextureFormat.BC6 || texture.TextureFormat == TextureFormat.BC7Srgb)
            {
                pixelSizeInBytes = 16;
                isBlockCompression = true;
            }

            var textureMemory = 0;
            var textureWidth = texture.Width;
            var textureHeight = texture.Height;

            for (var i = 0; i < texture.MipLevels; i++)
            {
                if (isBlockCompression)
                {
                    textureMemory += pixelSizeInBytes * (int)MathF.Ceiling((float)textureWidth / 4.0f) * (int)MathF.Ceiling((float)textureHeight / 4.0f);
                }
                
                else
                {
                    textureMemory += textureWidth * textureHeight * pixelSizeInBytes * texture.MultiSampleCount;
                }

                textureWidth = (textureWidth > 1) ? textureWidth / 2 : 1;
                textureHeight = (textureHeight > 1) ? textureHeight / 2 : 1;
            }

            return textureMemory;
        }
    }
}
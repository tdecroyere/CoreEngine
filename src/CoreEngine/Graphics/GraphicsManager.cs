using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class GraphicsManager : SystemManager
    {
        // TODO: Remove internal
        internal readonly IGraphicsService graphicsService;
        private readonly GraphicsMemoryManager graphicsMemoryManager;

        private static object syncObject = new object();
        private uint currentGraphicsResourceId;
        
        // TODO: Remove internal
        internal string graphicsAdapterName;
        internal int gpuMemoryUploaded;
        internal int cpuDrawCount;
        internal int cpuDispatchCount;

        private Dictionary<uint, GraphicsRenderPassDescriptor> renderPassDescriptors;
        private List<uint> aliasableResources = new List<uint>();

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
            this.graphicsMemoryManager = new GraphicsMemoryManager(graphicsService);

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

        public ulong AllocatedGpuMemory 
        { 
            get
            {
                return this.graphicsMemoryManager.AllocatedGpuMemory;
            }
        }

        public ulong AllocatedTransientGpuMemory 
        { 
            get
            {
                return this.graphicsMemoryManager.AllocatedTransientGpuMemory;
            }
        }

        public Vector2 GetRenderSize()
        {
            return this.graphicsService.GetRenderSize();
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(GraphicsHeapType heapType, int length, bool isStatic, string label) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var allocation = this.graphicsMemoryManager.AllocateBuffer(heapType, sizeInBytes);
            var graphicsBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId, allocation.GraphicsHeap.Id, allocation.Offset, allocation.IsAliasable, sizeInBytes);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            this.graphicsService.SetGraphicsBufferLabel(graphicsBufferId, $"{label}{(isStatic ? string.Empty : "0") }");

            uint? graphicsBufferId2 = null;
            GraphicsMemoryAllocation? allocation2 = null;

            if (!isStatic)
            {
                allocation2 = this.graphicsMemoryManager.AllocateBuffer(heapType, sizeInBytes);
                graphicsBufferId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateGraphicsBuffer(graphicsBufferId2.Value, allocation2.Value.GraphicsHeap.Id, allocation2.Value.Offset, allocation2.Value.IsAliasable, sizeInBytes);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }

                this.graphicsService.SetGraphicsBufferLabel(graphicsBufferId2.Value, $"{label}1");
            }

            return new GraphicsBuffer(this, allocation, allocation2, graphicsBufferId, graphicsBufferId2, sizeInBytes, isStatic, label);
        }

        public unsafe Span<T> GetCpuGraphicsBufferPointer<T>(GraphicsBuffer graphicsBuffer) where T : struct
        {
            // TODO: Check if the buffer is allocated in a CPU Heap

            var cpuPointer = this.graphicsService.GetGraphicsBufferCpuPointer(graphicsBuffer.GraphicsResourceId);
            return new Span<T>(cpuPointer.ToPointer(), graphicsBuffer.Length / Marshal.SizeOf(typeof(T)));
        }

        public void DeleteGraphicsBuffer(GraphicsBuffer graphicsBuffer)
        {
            this.graphicsService.DeleteGraphicsBuffer(graphicsBuffer.GraphicsResourceSystemId);
            this.graphicsMemoryManager.FreeAllocation(graphicsBuffer.GraphicsMemoryAllocation);

            if (!graphicsBuffer.IsStatic)
            {
                if (graphicsBuffer.GraphicsResourceSystemId2 != null)
                {
                    this.graphicsService.DeleteGraphicsBuffer(graphicsBuffer.GraphicsResourceSystemId2.Value);
                }

                if (graphicsBuffer.GraphicsMemoryAllocation2 != null)
                {
                    this.graphicsMemoryManager.FreeAllocation(graphicsBuffer.GraphicsMemoryAllocation2.Value);
                }
            }
        }

        // TODO: Do not forget to find a way to delete the transient resource
        public Texture CreateTexture(GraphicsHeapType heapType, TextureFormat textureFormat, TextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isStatic, string label)
        {
            var textureId = GetNextGraphicsResourceId();
            var allocation = this.graphicsMemoryManager.AllocateTexture(heapType, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
            var result = this.graphicsService.CreateTexture(textureId, allocation.GraphicsHeap.Id, allocation.Offset, allocation.IsAliasable, (GraphicsTextureFormat)(int)textureFormat, (GraphicsTextureUsage)usage, width, height, faceCount, mipLevels, multisampleCount);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            this.graphicsService.SetTextureLabel(textureId, $"{label}{(isStatic ? string.Empty : "0") }");

            if (allocation.IsAliasable)
            {
                aliasableResources.Add(textureId);
            }

            uint? textureId2 = null;
            GraphicsMemoryAllocation? allocation2 = null;

            if (!isStatic)
            {
                textureId2 = GetNextGraphicsResourceId();
                allocation2 = this.graphicsMemoryManager.AllocateTexture(heapType, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);

                if (allocation2.Value.IsAliasable)
                {
                    aliasableResources.Add(textureId2.Value);
                }

                result = this.graphicsService.CreateTexture(textureId2.Value, allocation2.Value.GraphicsHeap.Id, allocation2.Value.Offset, allocation2.Value.IsAliasable, (GraphicsTextureFormat)(int)textureFormat, (GraphicsTextureUsage)usage, width, height, faceCount, mipLevels, multisampleCount);
                
                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }

                this.graphicsService.SetTextureLabel(textureId2.Value, $"{label}1");
            }

            return new Texture(this, allocation, allocation2, textureId, textureId2, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount, isStatic, label);
        }

        public void DeleteTexture(Texture texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            this.graphicsService.DeleteTexture(texture.GraphicsResourceSystemId);
            this.graphicsMemoryManager.FreeAllocation(texture.GraphicsMemoryAllocation);

            if (!texture.IsStatic)
            {
                if (texture.GraphicsResourceSystemId2 != null)
                {
                    this.graphicsService.DeleteTexture(texture.GraphicsResourceSystemId2.Value);
                }

                if (texture.GraphicsMemoryAllocation2 != null)
                {
                    this.graphicsMemoryManager.FreeAllocation(texture.GraphicsMemoryAllocation2.Value);
                }
            }
        }

        public IndirectCommandBuffer CreateIndirectCommandBuffer(int maxCommandCount, bool isStatic, string label)
        {
            var graphicsResourceId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateIndirectCommandBuffer(graphicsResourceId, maxCommandCount);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the indirect command buffer resource.");
            }

            this.graphicsService.SetIndirectCommandBufferLabel(graphicsResourceId, $"{label}{(isStatic ? string.Empty : "0") }");

            uint? graphicsResourceId2 = null;

            if (!isStatic)
            {
                graphicsResourceId2 = GetNextGraphicsResourceId();
                result = this.graphicsService.CreateIndirectCommandBuffer(graphicsResourceId2.Value, maxCommandCount);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the indirect command buffer resource.");
                }

                this.graphicsService.SetIndirectCommandBufferLabel(graphicsResourceId2.Value, $"{label}1");
            }

            return new IndirectCommandBuffer(this, graphicsResourceId, graphicsResourceId2, maxCommandCount, isStatic, label);
        }

        public QueryBuffer CreateQueryBuffer(GraphicsQueryBufferType queryBufferType, int length, string label)
        {
            var queryBufferId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateQueryBuffer(queryBufferId, (GraphicsQueryBufferType)queryBufferType, length);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the query buffer resource.");
            }

            this.graphicsService.SetQueryBufferLabel(queryBufferId, label);

            uint queryBufferId2 = GetNextGraphicsResourceId();
            result = this.graphicsService.CreateQueryBuffer(queryBufferId2, (GraphicsQueryBufferType)queryBufferType, length);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the query buffer resource.");
            }

            this.graphicsService.SetQueryBufferLabel(queryBufferId2, label);

            return new QueryBuffer(this, queryBufferId, queryBufferId2, length, label);
        }

        public void DeleteQueryBuffer(QueryBuffer queryBuffer)
        {
            this.graphicsService.DeleteQueryBuffer(queryBuffer.GraphicsResourceSystemId);

            if (queryBuffer.GraphicsResourceSystemId2 != null)
            {
                this.graphicsService.DeleteQueryBuffer(queryBuffer.GraphicsResourceSystemId2.Value);
            }
        }

        internal Shader CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string label)
        {
            var shaderId = GetNextGraphicsResourceId();
            var result = this.graphicsService.CreateShader(shaderId, computeShaderFunction, shaderByteCode);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the shader resource.");
            }

            this.graphicsService.SetShaderLabel(shaderId, label);

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

        public CommandBuffer CreateCommandBuffer(CommandListType commandBufferType, string label)
        {
            var graphicsResourceId = GetNextGraphicsResourceId();
            var result = graphicsService.CreateCommandBuffer(graphicsResourceId, (GraphicsCommandBufferType)(int)commandBufferType, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the command buffer resource.");
            }


            var graphicsResourceId2 = GetNextGraphicsResourceId();
            result = this.graphicsService.CreateCommandBuffer(graphicsResourceId2, (GraphicsCommandBufferType)(int)commandBufferType, label);

            if (!result)
            {
                throw new InvalidOperationException("There was an error while creating the command buffer resource.");
            }

            return new CommandBuffer(this, graphicsResourceId, graphicsResourceId2, commandBufferType, label);
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

        public void CopyDataToGraphicsBuffer<T>(CommandList commandList, GraphicsBuffer destination, GraphicsBuffer source, int length) where T : struct
        {
            // TODO: Check that the source was allocated in a cpu heap

            var sizeInBytes = length * Marshal.SizeOf(typeof(T));

            this.graphicsService.CopyDataToGraphicsBuffer(commandList.Id, destination.GraphicsResourceId, source.GraphicsResourceId, sizeInBytes);
            this.gpuMemoryUploaded += sizeInBytes;
        }

        public void CopyDataToTexture<T>(CommandList commandList, Texture destination, GraphicsBuffer source, int width, int height, int slice, int mipLevel) where T : struct
        {
            // TODO: Check that the source was allocated in a cpu heap
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            this.graphicsService.CopyDataToTexture(commandList.Id, destination.GraphicsResourceId, source.GraphicsResourceId, (GraphicsTextureFormat)destination.TextureFormat, width, height, slice, mipLevel);
            this.gpuMemoryUploaded += source.Length;
        }

        public void CopyTexture(CommandList commandList, Texture destination, Texture source)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.graphicsService.CopyTexture(commandList.Id, destination.GraphicsResourceId, source.GraphicsResourceId);
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

            var commandList = new CommandList(commandListId, CommandListType.Compute);
            return commandList;
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

            var commandList = new CommandList(commandListId, CommandListType.Render);
            return commandList;
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
                var result = this.graphicsService.CreatePipelineState(pipelineStateId, shader.ShaderId, renderPassDescriptor);

                if (!result)
                {
                    throw new InvalidOperationException("There was an error while creating the pipelinestate object.");
                }

                this.graphicsService.SetPipelineStateLabel(pipelineStateId, $"{shader.Label}PSO");
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

            if (texture.Usage == TextureUsage.RenderTarget && !isReadOnly)
            {
                throw new InvalidOperationException("A Render Target cannot be set as a write shader resource.");
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
                var texture = textures[i];
                textureIdsList[i] = texture.GraphicsResourceId;
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

        public void QueryTimestamp(CommandList commandList, QueryBuffer queryBuffer, int index)
        {
            this.graphicsService.QueryTimestamp(commandList.Id, queryBuffer.GraphicsResourceId, index);
        }

        public void ResolveQueryData(CommandList commandList, QueryBuffer queryBuffer, GraphicsBuffer destinationBuffer, Range range)
        {
            var offsetAndLength = range.GetOffsetAndLength(queryBuffer.Length);
            this.graphicsService.ResolveQueryData(commandList.Id, queryBuffer.GraphicsResourceId, destinationBuffer.GraphicsResourceId, offsetAndLength.Offset, offsetAndLength.Length);
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

        public void WaitForAvailableScreenBuffer()
        {
            this.graphicsService.WaitForAvailableScreenBuffer();

            // TODO: A modulo here with Int.MaxValue
            this.CurrentFrameNumber++;
            this.cpuDrawCount = 0;
            this.cpuDispatchCount = 0;

            this.graphicsMemoryManager.Reset(this.CurrentFrameNumber);

            // TODO: We can have an issue here because the D3D resource can be released while still being used
            // We should do a kind of soft delete
            for (var i = 0; i < this.aliasableResources.Count; i++)
            {
                this.graphicsService.DeleteTexture(this.aliasableResources[i]);
            }

            this.aliasableResources.Clear();
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            Logger.BeginAction("Init Resource Loaders");
            resourcesManager.AddResourceLoader(new TextureResourceLoader(resourcesManager, this));
            resourcesManager.AddResourceLoader(new ShaderResourceLoader(resourcesManager, this));
            Logger.EndAction();
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
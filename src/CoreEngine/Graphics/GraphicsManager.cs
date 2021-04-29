using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;
using CoreEngine.UI.Native;

namespace CoreEngine.Graphics
{
    public class GraphicsManager : SystemManager, IDisposable
    {
        private readonly IGraphicsService graphicsService;
        private readonly GraphicsMemoryManager graphicsMemoryManager;
        private readonly ShaderResourceManager shaderResourceManager;

        private readonly bool logResourceAllocationInfos;

        // TODO: Remove internal
        internal string graphicsAdapterName;
        internal int gpuMemoryUploaded;
        internal int cpuDrawCount;
        internal int cpuDispatchCount;

        private Dictionary<IntPtr, GraphicsRenderPassDescriptor> renderPassDescriptors;
        private List<Texture> aliasableTextures = new List<Texture>();

        private List<GraphicsBuffer> graphicsBuffers = new List<GraphicsBuffer>();
        private List<Texture> textures = new List<Texture>();
        private List<PipelineState> pipelineStates = new List<PipelineState>();
        private List<Shader> shaders = new List<Shader>();

        private List<GraphicsBuffer>[] graphicsBuffersToDelete = new List<GraphicsBuffer>[2];
        private List<Texture>[] texturesToDelete = new List<Texture>[2];
        private List<PipelineState>[] pipelineStatesToDelete = new List<PipelineState>[2];
        private List<Shader>[] shadersToDelete = new List<Shader>[2];

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
            this.shaderResourceManager = new ShaderResourceManager(graphicsService);

            var graphicsAdapterName = this.graphicsService.GetGraphicsAdapterName();
            this.graphicsAdapterName = (graphicsAdapterName != null) ? graphicsAdapterName : "Unknown Graphics Adapter";

            this.renderPassDescriptors = new Dictionary<IntPtr, GraphicsRenderPassDescriptor>();

            graphicsBuffersToDelete[0] = new List<GraphicsBuffer>();
            graphicsBuffersToDelete[1] = new List<GraphicsBuffer>();

            texturesToDelete[0] = new List<Texture>();
            texturesToDelete[1] = new List<Texture>();

            pipelineStatesToDelete[0] = new List<PipelineState>();
            pipelineStatesToDelete[1] = new List<PipelineState>();

            shadersToDelete[0] = new List<Shader>();
            shadersToDelete[1] = new List<Shader>();

            InitResourceLoaders(resourcesManager);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                for (var i = 0; i < this.graphicsBuffers.Count; i++)
                {
                    DeleteGraphicsBuffer(this.graphicsBuffers[i]);
                }

                for (var i = 0; i < this.textures.Count; i++)
                {
                    DeleteTexture(this.textures[i]);
                }

                for (var i = 0; i < this.pipelineStates.Count; i++)
                {
                    DeletePipelineState(this.pipelineStates[i]);
                }

                for (var i = 0; i < this.shaders.Count; i++)
                {
                    DeleteShader(this.shaders[i]);
                }
            }

            
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

        public CommandQueue CreateCommandQueue(CommandType queueType, string label)
        {
            var nativePointer = this.graphicsService.CreateCommandQueue((GraphicsServiceCommandType)queueType);

            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the render queue.");
            }

            this.graphicsService.SetCommandQueueLabel(nativePointer, label);

            return new CommandQueue(this, nativePointer, queueType, label);
        }

        internal void DeleteCommandQueue(CommandQueue commandQueue)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Command Queue {commandQueue.Label}...");
            }

            this.graphicsService.DeleteCommandQueue(commandQueue.NativePointer);
        }

        public void ResetCommandQueue(CommandQueue commandQueue)
        {
            this.graphicsService.ResetCommandQueue(commandQueue.NativePointer);
        }

        public ulong GetCommandQueueTimestampFrequency(CommandQueue commandQueue)
        {
            return this.graphicsService.GetCommandQueueTimestampFrequency(commandQueue.NativePointer);
        }

        public Fence ExecuteCommandLists(CommandQueue commandQueue, ReadOnlySpan<CommandList> commandLists, bool isAwaitable)
        {
            // TODO: This code is not thread safe!

            var freeList = commandQueue.commandListFreeList;

            var commandListsPointers = ArrayPool<IntPtr>.Shared.Rent(commandLists.Length);

            for (var i = 0; i < commandLists.Length; i++)
            {
                commandListsPointers[i] = commandLists[i].NativePointer;
                freeList.Push(commandLists[i]);
            }

            var fenceValue = this.graphicsService.ExecuteCommandLists(commandQueue.NativePointer, commandListsPointers.AsSpan(0..commandLists.Length), isAwaitable);
            ArrayPool<IntPtr>.Shared.Return(commandListsPointers);

            return new Fence(commandQueue, fenceValue);
        }

        public void WaitForCommandQueue(CommandQueue commandQueue, Fence fence)
        {
            this.graphicsService.WaitForCommandQueue(commandQueue.NativePointer, fence.CommandQueue.NativePointer, fence.Value);
        }

        public void WaitForCommandQueueOnCpu(Fence fence)
        {
            this.graphicsService.WaitForCommandQueueOnCpu(fence.CommandQueue.NativePointer, fence.Value);
        }

        public CommandList CreateCommandList(CommandQueue commandQueue, string label)
        {
            // TODO: This code is not thread safe!
            var freeList = commandQueue.commandListFreeList;

            if (freeList.Count > 0)
            {
                var commandList = freeList.Pop();
                this.graphicsService.ResetCommandList(commandList.NativePointer);
                this.graphicsService.SetCommandListLabel(commandList.NativePointer, label);

                return new CommandList(commandList.NativePointer, commandQueue.Type, commandQueue, label);
            }

            Logger.WriteMessage("Creating Command List");

            var nativePointer = graphicsService.CreateCommandList(commandQueue.NativePointer);

            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the command list resource.");
            }

            graphicsService.SetCommandListLabel(nativePointer, label);

            return new CommandList(nativePointer, commandQueue.Type, commandQueue, label);
        }

        internal void DeleteCommandList(CommandList commandList)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Command List {commandList.Label}...");
            }

            this.graphicsService.DeleteCommandList(commandList.NativePointer);
        }

        public void CommitCommandList(CommandList commandList)
        {
            this.graphicsService.CommitCommandList(commandList.NativePointer);            
        }

        public GraphicsBuffer CreateGraphicsBuffer<T>(GraphicsHeapType heapType, int length, bool isStatic, string label) where T : struct
        {
            var sizeInBytes = Marshal.SizeOf(typeof(T)) * length;

            var allocation = this.graphicsMemoryManager.AllocateBuffer(heapType, sizeInBytes);
            var nativePointer1 = this.graphicsService.CreateGraphicsBuffer(allocation.GraphicsHeap.NativePointer, allocation.Offset, allocation.IsAliasable, sizeInBytes);

            if (nativePointer1 == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            this.graphicsService.SetGraphicsBufferLabel(nativePointer1, $"{label}{(isStatic ? string.Empty : "0") }");

            IntPtr cpuPointer = IntPtr.Zero;

            if (heapType == GraphicsHeapType.Upload)
            {
                cpuPointer = this.graphicsService.GetGraphicsBufferCpuPointer(nativePointer1);
            }

            IntPtr? nativePointer2 = null;
            GraphicsMemoryAllocation? allocation2 = null;
            IntPtr cpuPointer2 = IntPtr.Zero;

            if (!isStatic)
            {
                allocation2 = this.graphicsMemoryManager.AllocateBuffer(heapType, sizeInBytes);
                nativePointer2 = this.graphicsService.CreateGraphicsBuffer(allocation2.Value.GraphicsHeap.NativePointer, allocation2.Value.Offset, allocation2.Value.IsAliasable, sizeInBytes);

                if (nativePointer2.Value == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }

                this.graphicsService.SetGraphicsBufferLabel(nativePointer2.Value, $"{label}1");

                if (heapType == GraphicsHeapType.Upload)
                {
                    cpuPointer2 = this.graphicsService.GetGraphicsBufferCpuPointer(nativePointer2.Value);
                }
            }

            var graphicsBuffer = new GraphicsBuffer(this, allocation, allocation2, nativePointer1, nativePointer2, cpuPointer, cpuPointer2, sizeInBytes, isStatic, label);
            this.graphicsBuffers.Add(graphicsBuffer);

            if (heapType == GraphicsHeapType.Gpu)
            {
                this.shaderResourceManager.CreateShaderResourceBuffer(graphicsBuffer);
            }

            return graphicsBuffer;
        }

        public unsafe Span<T> GetCpuGraphicsBufferPointer<T>(GraphicsBuffer graphicsBuffer) where T : struct
        {
            if (graphicsBuffer == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffer));
            }

            // TODO: Check if the buffer is allocated in a CPU Heap
            var cpuPointer = graphicsBuffer.CpuPointer;

            if (cpuPointer == IntPtr.Zero)
            {
                cpuPointer = this.graphicsService.GetGraphicsBufferCpuPointer(graphicsBuffer.NativePointer);
            }

            return new Span<T>(cpuPointer.ToPointer(), graphicsBuffer.Length / Marshal.SizeOf(typeof(T)));
        }

        internal void ScheduleDeleteGraphicsBuffer(GraphicsBuffer graphicsBuffer)
        {
            this.graphicsBuffersToDelete[this.CurrentFrameNumber % 2].Add(graphicsBuffer);
        }

        private void DeleteGraphicsBuffer(GraphicsBuffer graphicsBuffer)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Graphics buffer {graphicsBuffer.Label}...");
            }

            this.graphicsService.DeleteGraphicsBuffer(graphicsBuffer.NativePointer1);
            this.graphicsMemoryManager.FreeAllocation(graphicsBuffer.GraphicsMemoryAllocation);

            if (!graphicsBuffer.IsStatic)
            {
                if (graphicsBuffer.NativePointer2 != null)
                {
                    this.graphicsService.DeleteGraphicsBuffer(graphicsBuffer.NativePointer2.Value);
                }

                if (graphicsBuffer.GraphicsMemoryAllocation2 != null)
                {
                    this.graphicsMemoryManager.FreeAllocation(graphicsBuffer.GraphicsMemoryAllocation2.Value);
                }
            }

            // TODO: Use something faster here
            this.graphicsBuffers.Remove(graphicsBuffer);
            this.shaderResourceManager.DeleteShaderResourceBuffer(graphicsBuffer);
        }

        // TODO: Do not forget to find a way to delete the transient resource
        public Texture CreateTexture(GraphicsHeapType heapType, TextureFormat textureFormat, TextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount, bool isStatic, string label)
        {
            var allocation = this.graphicsMemoryManager.AllocateTexture(heapType, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
            var nativePointer1 = this.graphicsService.CreateTexture(allocation.GraphicsHeap.NativePointer, allocation.Offset, allocation.IsAliasable, (GraphicsTextureFormat)(int)textureFormat, (GraphicsTextureUsage)usage, width, height, faceCount, mipLevels, multisampleCount);

            if (nativePointer1 == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the texture resource.");
            }

            this.graphicsService.SetTextureLabel(nativePointer1, $"{label}{(isStatic ? string.Empty : "0") }");

            IntPtr? nativePointer2 = null;
            GraphicsMemoryAllocation? allocation2 = null;

            if (!isStatic)
            {
                allocation2 = this.graphicsMemoryManager.AllocateTexture(heapType, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount);
                nativePointer2 = this.graphicsService.CreateTexture(allocation2.Value.GraphicsHeap.NativePointer, allocation2.Value.Offset, allocation2.Value.IsAliasable, (GraphicsTextureFormat)(int)textureFormat, (GraphicsTextureUsage)usage, width, height, faceCount, mipLevels, multisampleCount);
                
                if (nativePointer2.Value == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the texture resource.");
                }

                this.graphicsService.SetTextureLabel(nativePointer2.Value, $"{label}1");
            }

            var texture = new Texture(this, allocation, allocation2, nativePointer1, nativePointer2, textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount, isStatic, label);
            this.textures.Add(texture);

            if (heapType == GraphicsHeapType.Gpu)
            {
                this.shaderResourceManager.CreateShaderResourceTexture(texture);
            }
            
            if (allocation.IsAliasable)
            {
                aliasableTextures.Add(texture);
            }

            return texture;
        }

        internal void ScheduleDeleteTexture(Texture texture)
        {
            this.texturesToDelete[this.CurrentFrameNumber % 2].Add(texture);
        }

        private void DeleteTexture(Texture texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Texture {texture.Label}...");
            }

            this.graphicsService.DeleteTexture(texture.NativePointer1);
            this.graphicsMemoryManager.FreeAllocation(texture.GraphicsMemoryAllocation);

            if (!texture.IsStatic)
            {
                if (texture.NativePointer2 != null)
                {
                    this.graphicsService.DeleteTexture(texture.NativePointer2.Value);
                }

                if (texture.GraphicsMemoryAllocation2 != null)
                {
                    this.graphicsMemoryManager.FreeAllocation(texture.GraphicsMemoryAllocation2.Value);
                }
            }

            this.shaderResourceManager.DeleteShaderResourceTexture(texture);

            // TODO: Use something faster here
            this.textures.Remove(texture);
        }

        public SwapChain CreateSwapChain(Window window, CommandQueue commandQueue, int width, int height, TextureFormat textureFormat)
        {
            var nativePointer = this.graphicsService.CreateSwapChain(window.NativePointer, commandQueue.NativePointer, width, height, (GraphicsTextureFormat)textureFormat);

            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the swap-chain.");
            }

            return new SwapChain(nativePointer, commandQueue, width, height, textureFormat);
        }

        public void ResizeSwapChain(SwapChain swapChain, int width, int height)
        {
            if (swapChain == null)
            {
                throw new ArgumentNullException(nameof(swapChain));
            }

            swapChain.Width = width;
            swapChain.Height = height;
            
            this.graphicsService.ResizeSwapChain(swapChain.NativePointer, width, height);
        }

        public Texture GetSwapChainBackBufferTexture(SwapChain swapChain)
        {
            if (swapChain == null)
            {
                throw new ArgumentNullException(nameof(swapChain));
            }

            var textureNativePointer = this.graphicsService.GetSwapChainBackBufferTexture(swapChain.NativePointer);
            return new Texture(this, new GraphicsMemoryAllocation(), null, textureNativePointer, null, swapChain.TextureFormat, TextureUsage.RenderTarget, swapChain.Width, swapChain.Height, 1, 1, 1, isStatic: true, "BackBuffer");
        }

        public Fence PresentSwapChain(SwapChain swapChain)
        {
            if (swapChain == null)
            {
                throw new ArgumentNullException(nameof(swapChain));
            }
            
            var fenceValue = this.graphicsService.PresentSwapChain(swapChain.NativePointer);
            return new Fence(swapChain.CommandQueue, fenceValue);
        }

        public void WaitForSwapChainOnCpu(SwapChain swapChain)
        {
            if (swapChain == null)
            {
                throw new ArgumentNullException(nameof(swapChain));
            }

            this.graphicsService.WaitForSwapChainOnCpu(swapChain.NativePointer);
        }

        public IndirectCommandBuffer CreateIndirectCommandBuffer(int maxCommandCount, bool isStatic, string label)
        {
            var nativePointer1 = this.graphicsService.CreateIndirectCommandBuffer(maxCommandCount);

            if (nativePointer1 == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the indirect command buffer resource.");
            }

            this.graphicsService.SetIndirectCommandBufferLabel(nativePointer1, $"{label}{(isStatic ? string.Empty : "0") }");

            IntPtr? nativePointer2 = null;

            if (!isStatic)
            {
                nativePointer2 = this.graphicsService.CreateIndirectCommandBuffer(maxCommandCount);

                if (nativePointer2.Value == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the indirect command buffer resource.");
                }

                this.graphicsService.SetIndirectCommandBufferLabel(nativePointer2.Value, $"{label}1");
            }

            return new IndirectCommandBuffer(this, nativePointer1, nativePointer2, maxCommandCount, isStatic, label);
        }

        public QueryBuffer CreateQueryBuffer(GraphicsQueryBufferType queryBufferType, int length, string label)
        {
            var nativePointer1 = this.graphicsService.CreateQueryBuffer((GraphicsQueryBufferType)queryBufferType, length);

            if (nativePointer1 == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the query buffer resource.");
            }

            this.graphicsService.SetQueryBufferLabel(nativePointer1, label);

            IntPtr nativePointer2 = this.graphicsService.CreateQueryBuffer((GraphicsQueryBufferType)queryBufferType, length);

            if (nativePointer2 == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the query buffer resource.");
            }

            this.graphicsService.SetQueryBufferLabel(nativePointer2, label);

            return new QueryBuffer(this, nativePointer1, nativePointer2, length, label);
        }

        public void DeleteQueryBuffer(QueryBuffer queryBuffer)
        {
            if (queryBuffer == null)
            {
                throw new ArgumentNullException(nameof(queryBuffer));
            }

            this.graphicsService.DeleteQueryBuffer(queryBuffer.NativePointer1);

            if (queryBuffer.NativePointer2 != null)
            {
                this.graphicsService.DeleteQueryBuffer(queryBuffer.NativePointer2.Value);
            }
        }

        internal Shader CreateShader(string? computeShaderFunction, ReadOnlySpan<byte> shaderByteCode, string label)
        {
            var nativePointer = this.graphicsService.CreateShader(computeShaderFunction, shaderByteCode);

            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the shader resource.");
            }

            this.graphicsService.SetShaderLabel(nativePointer, label);

            var shader = new Shader(this, nativePointer, label);
            this.shaders.Add(shader);

            return shader;
        }

        internal void ScheduleDeletePipelineState(PipelineState pipelineState)
        {
            this.pipelineStatesToDelete[this.CurrentFrameNumber % 2].Add(pipelineState);
        }

        private void DeletePipelineState(PipelineState pipelineState)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting PipelineState {pipelineState.Label}...");
            }

            this.graphicsService.DeletePipelineState(pipelineState.NativePointer);

            // TODO: Use something faster here
            this.pipelineStates.Remove(pipelineState);
        }

        internal void ScheduleDeleteShader(Shader shader)
        {
            foreach (var pipelineState in shader.PipelineStates.Values)
            {
                this.ScheduleDeletePipelineState(pipelineState);
            }

            shader.PipelineStates.Clear();
            this.shadersToDelete[this.CurrentFrameNumber % 2].Add(shader);
        }

        private void DeleteShader(Shader shader)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Shader {shader.Label}...");
            }

            this.graphicsService.DeleteShader(shader.NativePointer);

            // TODO: Use something faster here
            this.shaders.Remove(shader);
        }

        public void CopyDataToGraphicsBuffer<T>(CommandList commandList, GraphicsBuffer destination, GraphicsBuffer source, int length) where T : struct
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // TODO: Check that the source was allocated in a cpu heap

            var sizeInBytes = length * Marshal.SizeOf(typeof(T));

            this.graphicsService.CopyDataToGraphicsBuffer(commandList.NativePointer, destination.NativePointer, source.NativePointer, sizeInBytes);
            this.gpuMemoryUploaded += sizeInBytes;
        }

        public void CopyDataToTexture<T>(CommandList commandList, Texture destination, GraphicsBuffer source, int width, int height, int slice, int mipLevel) where T : struct
        {
            // TODO: Check that the source was allocated in a cpu heap
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            this.graphicsService.CopyDataToTexture(commandList.NativePointer, destination.NativePointer, source.NativePointer, (GraphicsTextureFormat)destination.TextureFormat, width, height, slice, mipLevel);
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

            this.graphicsService.CopyTexture(commandList.NativePointer, destination.NativePointer, source.NativePointer);
        }

        public void ResetIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandList, int maxCommandCount)
        {
            this.graphicsService.ResetIndirectCommandList(commandList.NativePointer, indirectCommandList.NativePointer, maxCommandCount);
        }

        public void OptimizeIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandList, int maxCommandCount)
        {
            this.graphicsService.OptimizeIndirectCommandList(commandList.NativePointer, indirectCommandList.NativePointer, maxCommandCount);
        }

        public Vector3 DispatchThreads(CommandList commandList, uint threadCountX, uint threadCountY, uint threadCountZ)
        {
            if (commandList.Type != CommandType.Compute)
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
            return this.graphicsService.DispatchThreads(commandList.NativePointer, threadCountX, threadCountY, threadCountZ);
        }

        // TODO: Add checks to all render functins to see if a render pass has been started
        public void BeginRenderPass(CommandList commandList, RenderPassDescriptor renderPassDescriptor)
        {
            var graphicsRenderPassDescriptor = new GraphicsRenderPassDescriptor(renderPassDescriptor);
            graphicsService.BeginRenderPass(commandList.NativePointer, graphicsRenderPassDescriptor);

            this.renderPassDescriptors.Add(commandList.NativePointer, graphicsRenderPassDescriptor);
        }

        public void EndRenderPass(CommandList commandList)
        {
            if (commandList.Type != CommandType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.EndRenderPass(commandList.NativePointer);
            this.renderPassDescriptors.Remove(commandList.NativePointer);
        }

        public void SetShader(CommandList commandList, Shader shader, bool newShader = false)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            if (!shader.IsLoaded || shader.NativePointer == IntPtr.Zero)
            {
                return;
            }

            if (newShader)
                this.shaderResourceManager.SetShaderResourceHeap(commandList);

            this.graphicsService.SetShader(commandList.NativePointer, shader.NativePointer);

            var renderPassDescriptor = new GraphicsRenderPassDescriptor();

            if (this.renderPassDescriptors.ContainsKey(commandList.NativePointer))
            {
                renderPassDescriptor = this.renderPassDescriptors[commandList.NativePointer];
            }

            if (!shader.PipelineStates.ContainsKey(renderPassDescriptor))
            {
                Logger.WriteMessage($"Create Pipeline State for shader {shader.NativePointer}...");

                var nativePointer = this.graphicsService.CreatePipelineState(shader.NativePointer, renderPassDescriptor);

                if (nativePointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the pipelinestate object.");
                }

                this.graphicsService.SetPipelineStateLabel(nativePointer, $"{shader.Label}PSO");

                var pipelineState = new PipelineState(this, nativePointer, $"{shader.Label}PSO");
                this.pipelineStates.Add(pipelineState);
                shader.PipelineStates.Add(renderPassDescriptor, pipelineState);
            }

            this.graphicsService.SetPipelineState(commandList.NativePointer, shader.PipelineStates[renderPassDescriptor].NativePointer);
        }

        public void SetShaderParameterValues(CommandList commandList, uint slot, ReadOnlySpan<uint> values)
        {
            this.graphicsService.SetShaderParameterValues(commandList.NativePointer, slot, values);
        }

        public void SetShaderBuffer(CommandList commandList, GraphicsBuffer graphicsBuffer, int slot, bool isReadOnly = true, int index = 0)
        {
            if (graphicsBuffer == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffer));
            }

            this.graphicsService.SetShaderBuffer(commandList.NativePointer, graphicsBuffer.NativePointer, slot, isReadOnly, index);
        }

        public void SetShaderBuffers(CommandList commandList, ReadOnlySpan<GraphicsBuffer> graphicsBuffers, int slot, int index = 0)
        {
            if (graphicsBuffers == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffers));
            }

            var graphicsBufferIdsList = new IntPtr[graphicsBuffers.Length];

            for (var i = 0; i < graphicsBuffers.Length; i++)
            {
                graphicsBufferIdsList[i] = graphicsBuffers[i].NativePointer;
            }

            this.graphicsService.SetShaderBuffers(commandList.NativePointer, graphicsBufferIdsList.AsSpan(), slot, index);
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

            this.graphicsService.SetShaderTexture(commandList.NativePointer, texture.NativePointer, slot, isReadOnly, index);
        }

        public void SetShaderTextures(CommandList commandList, ReadOnlySpan<Texture> textures, int slot, int index = 0)
        {
            if (textures == null)
            {
                throw new ArgumentNullException(nameof(textures));
            }

            var textureIdsList = new IntPtr[textures.Length];

            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                textureIdsList[i] = texture.NativePointer;
            }

            this.graphicsService.SetShaderTextures(commandList.NativePointer, textureIdsList.AsSpan(), slot, index);
        }

        public void SetShaderIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandBuffer, int slot, int index = 0)
        {
            this.graphicsService.SetShaderIndirectCommandList(commandList.NativePointer, indirectCommandBuffer.NativePointer, slot, index);
        }

        public void SetShaderIndirectCommandBuffers(CommandList commandList, ReadOnlySpan<IndirectCommandBuffer> indirectCommandBuffers, int slot, int index = 0)
        {
            if (indirectCommandBuffers == null)
            {
                throw new ArgumentNullException(nameof(indirectCommandBuffers));
            }

            var list = new IntPtr[indirectCommandBuffers.Length];

            for (var i = 0; i < indirectCommandBuffers.Length; i++)
            {
                list[i] = indirectCommandBuffers[i].NativePointer;
            }

            this.graphicsService.SetShaderIndirectCommandLists(commandList.NativePointer, list.AsSpan(), slot, index);
        }

        public void ExecuteIndirectCommandBuffer(CommandList commandList, IndirectCommandBuffer indirectCommandBuffer, int maxCommandCount)
        {
            if (commandList.Type != CommandType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.ExecuteIndirectCommandBuffer(commandList.NativePointer, indirectCommandBuffer.NativePointer, maxCommandCount);
        }

        public void DispatchMesh(CommandList commandList, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            if (commandList.Type != CommandType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            if (threadGroupCountX == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadGroupCountX));
            }

            if (threadGroupCountY == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadGroupCountY));
            }

            if (threadGroupCountZ == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threadGroupCountZ));
            }

            this.graphicsService.DispatchMesh(commandList.NativePointer, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
            this.cpuDrawCount++;
        }

        [Obsolete("Use DispatchMesh")]
        public void SetIndexBuffer(CommandList commandList, GraphicsBuffer indexBuffer)
        {
            if (indexBuffer == null)
            {
                throw new ArgumentNullException(nameof(indexBuffer));
            }

            this.graphicsService.SetIndexBuffer(commandList.NativePointer, indexBuffer.NativePointer);
        }

        [Obsolete("Use DispatchMesh")]
        public void DrawIndexedPrimitives(CommandList commandList, PrimitiveType primitiveType, int startIndex, int indexCount, int instanceCount, int baseInstanceId)
        {
            if (commandList.Type != CommandType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.DrawIndexedPrimitives(commandList.NativePointer, 
                                                (GraphicsPrimitiveType)(int)primitiveType, 
                                                startIndex, 
                                                indexCount,
                                                instanceCount,
                                                baseInstanceId);

            this.cpuDrawCount++;
        }

        [Obsolete("Use DispatchMesh")]
        public void DrawPrimitives(CommandList commandList, PrimitiveType primitiveType, int startVertex, int vertexCount)
        {
            if (commandList.Type != CommandType.Render)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.DrawPrimitives(commandList.NativePointer, 
                                                (GraphicsPrimitiveType)(int)primitiveType, 
                                                startVertex, 
                                                vertexCount);

            this.cpuDrawCount++;
        }

        public void QueryTimestamp(CommandList commandList, QueryBuffer queryBuffer, int index)
        {
            if (queryBuffer == null)
            {
                throw new ArgumentNullException(nameof(queryBuffer));
            }

            this.graphicsService.QueryTimestamp(commandList.NativePointer, queryBuffer.NativePointer, index);
        }

        public void ResolveQueryData(CommandList commandList, QueryBuffer queryBuffer, GraphicsBuffer destinationBuffer, Range range)
        {
            if (queryBuffer == null)
            {
                throw new ArgumentNullException(nameof(queryBuffer));
            }

            if (destinationBuffer == null)
            {
                throw new ArgumentNullException(nameof(destinationBuffer));
            }

            var offsetAndLength = range.GetOffsetAndLength(queryBuffer.Length);
            this.graphicsService.ResolveQueryData(commandList.NativePointer, queryBuffer.NativePointer, destinationBuffer.NativePointer, offsetAndLength.Offset, offsetAndLength.Length);
        }

        public void MoveToNextFrame()
        {
            for (var i = 0; i < this.aliasableTextures.Count; i++)
            {
                this.aliasableTextures[i].Dispose();
            }

            this.aliasableTextures.Clear();

            // this.graphicsService.WaitForAvailableScreenBuffer();

            // TODO: A modulo here with Int.MaxValue
            this.CurrentFrameNumber++;
            this.cpuDrawCount = 0;
            this.cpuDispatchCount = 0;

            this.graphicsMemoryManager.Reset(this.CurrentFrameNumber);

            for (var i = 0; i < this.graphicsBuffersToDelete[this.CurrentFrameNumber % 2].Count; i++)
            {
                this.DeleteGraphicsBuffer(this.graphicsBuffersToDelete[this.CurrentFrameNumber % 2][i]);
            }

            this.graphicsBuffersToDelete[this.CurrentFrameNumber % 2].Clear();

            for (var i = 0; i < this.texturesToDelete[this.CurrentFrameNumber % 2].Count; i++)
            {
                this.DeleteTexture(this.texturesToDelete[this.CurrentFrameNumber % 2][i]);
            }

            this.texturesToDelete[this.CurrentFrameNumber % 2].Clear();

            for (var i = 0; i < this.pipelineStatesToDelete[this.CurrentFrameNumber % 2].Count; i++)
            {
                this.DeletePipelineState(this.pipelineStatesToDelete[this.CurrentFrameNumber % 2][i]);
            }

            this.pipelineStatesToDelete[this.CurrentFrameNumber % 2].Clear();

            for (var i = 0; i < this.shadersToDelete[this.CurrentFrameNumber % 2].Count; i++)
            {
                this.DeleteShader(this.shadersToDelete[this.CurrentFrameNumber % 2][i]);
            }

            this.shadersToDelete[this.CurrentFrameNumber % 2].Clear();
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            resourcesManager.AddResourceLoader(new ShaderResourceLoader(resourcesManager, this));
        }
    }
}
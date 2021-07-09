using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        private readonly GraphicsBuffer resetCounterBuffer;

        private List<Texture> aliasableTextures = new List<Texture>();

        private List<GraphicsBuffer> graphicsBuffers = new List<GraphicsBuffer>();
        private List<Texture> textures = new List<Texture>();
        private List<PipelineState> pipelineStates = new List<PipelineState>();
        private List<Shader> shaders = new List<Shader>();
        private List<QueryBuffer> queryBuffers = new List<QueryBuffer>();
        private List<SwapChain> swapChains = new List<SwapChain>();
        private List<GraphicsHeap> graphicsHeaps = new List<GraphicsHeap>();

        private List<GraphicsBuffer>[] graphicsBuffersToDelete = new List<GraphicsBuffer>[2];
        private List<Texture>[] texturesToDelete = new List<Texture>[2];
        private List<PipelineState>[] pipelineStatesToDelete = new List<PipelineState>[2];
        private List<Shader>[] shadersToDelete = new List<Shader>[2];
        private List<QueryBuffer>[] queryBuffersToDelete = new List<QueryBuffer>[2];
        private List<SwapChain>[] swapChainsToDelete = new List<SwapChain>[2];
        private List<GraphicsHeap>[] graphicsHeapsToDelete = new List<GraphicsHeap>[2];

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
            this.graphicsMemoryManager = new GraphicsMemoryManager(this, graphicsService);
            this.shaderResourceManager = new ShaderResourceManager(graphicsService);

            var graphicsAdapterName = this.graphicsService.GetGraphicsAdapterName().Replace("\0", "");
            this.graphicsAdapterName = (graphicsAdapterName != null) ? graphicsAdapterName : "Unknown Graphics Adapter";

            graphicsBuffersToDelete[0] = new List<GraphicsBuffer>();
            graphicsBuffersToDelete[1] = new List<GraphicsBuffer>();

            texturesToDelete[0] = new List<Texture>();
            texturesToDelete[1] = new List<Texture>();

            pipelineStatesToDelete[0] = new List<PipelineState>();
            pipelineStatesToDelete[1] = new List<PipelineState>();

            shadersToDelete[0] = new List<Shader>();
            shadersToDelete[1] = new List<Shader>();

            queryBuffersToDelete[0] = new List<QueryBuffer>();
            queryBuffersToDelete[1] = new List<QueryBuffer>();

            swapChainsToDelete[0] = new List<SwapChain>();
            swapChainsToDelete[1] = new List<SwapChain>();

            graphicsHeapsToDelete[0] = new List<GraphicsHeap>();
            graphicsHeapsToDelete[1] = new List<GraphicsHeap>();

            this.resetCounterBuffer = CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 1, isStatic: true, "ResetCounterBuffer");
            this.CopyDataToGraphicsBuffer<uint>(this.resetCounterBuffer, 0, new uint[] { 0 });

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
                // TODO: Do something better here!
                this.graphicsMemoryManager.Dispose();
                this.shaderResourceManager.Dispose();
                this.resetCounterBuffer.Dispose();

                var tmpGraphicsBuffers = new GraphicsBuffer[this.graphicsBuffers.Count];
                this.graphicsBuffers.CopyTo(tmpGraphicsBuffers);

                for (var i = 0; i < tmpGraphicsBuffers.Length; i++)
                {
                    DeleteGraphicsBuffer(tmpGraphicsBuffers[i]);
                }

                var tmpTextures = new Texture[this.textures.Count];
                this.textures.CopyTo(tmpTextures);

                for (var i = 0; i < tmpTextures.Length; i++)
                {
                    DeleteTexture(tmpTextures[i]);
                }

                var tmpPipelineStates = new PipelineState[this.pipelineStates.Count];
                this.pipelineStates.CopyTo(tmpPipelineStates);

                for (var i = 0; i < tmpPipelineStates.Length; i++)
                {
                    DeletePipelineState(tmpPipelineStates[i]);
                }

                var tmpShaders = new Shader[this.shaders.Count];
                this.shaders.CopyTo(tmpShaders);

                for (var i = 0; i < tmpShaders.Length; i++)
                {
                    DeleteShader(tmpShaders[i]);
                }

                var tmpQueryBuffers = new QueryBuffer[this.queryBuffers.Count];
                this.queryBuffers.CopyTo(tmpQueryBuffers);

                for (var i = 0; i < tmpQueryBuffers.Length; i++)
                {
                    DeleteQueryBuffer(tmpQueryBuffers[i]);
                }

                var tmpSwapChains = new SwapChain[this.swapChains.Count];
                this.swapChains.CopyTo(tmpSwapChains);

                for (var i = 0; i < tmpSwapChains.Length; i++)
                {
                    DeleteSwapChain(tmpSwapChains[i]);
                }

                var tmpGraphicsHeaps = new GraphicsHeap[this.graphicsHeaps.Count];
                this.graphicsHeaps.CopyTo(tmpGraphicsHeaps);

                for (var i = 0; i < tmpGraphicsHeaps.Length; i++)
                {
                    DeleteGraphicsHeap(tmpGraphicsHeaps[i]);
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

        public ulong TotalGpuMemory 
        { 
            get
            {
                return this.graphicsMemoryManager.TotalGpuMemory;
            }
        }

        public ulong AllocatedTransientGpuMemory 
        { 
            get
            {
                return this.graphicsMemoryManager.AllocatedTransientGpuMemory;
            }
        }

        public ulong TotalTransientGpuMemory 
        { 
            get
            {
                return this.graphicsMemoryManager.TotalTransientGpuMemory;
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

        public Fence ExecuteCommandLists(CommandQueue commandQueue, ReadOnlySpan<CommandList> commandLists)
        {
            return ExecuteCommandLists(commandQueue, commandLists, Array.Empty<Fence>());
        }

        public Fence ExecuteCommandLists(CommandQueue commandQueue, ReadOnlySpan<CommandList> commandLists, ReadOnlySpan<Fence> fencesToWait)
        {
            // TODO: This code is not thread safe!
            // TODO: Refactor that code!

            var fencesToWaitArray = ArrayPool<GraphicsFence>.Shared.Rent(fencesToWait.Length);

            CommandList? transitionCommandListBefore = null;
            CommandList? transitionCommandListAfter = null;

            for (var i = 0; i < fencesToWait.Length; i++)
            {
                fencesToWaitArray[i] = new GraphicsFence(fencesToWait[i]);

                var commandQueueToWait = fencesToWait[i].CommandQueue;

                for (var j = 0; j < commandQueueToWait.CurrentCopyBuffers.Count; j++)
                {
                    var buffer = commandQueueToWait.CurrentCopyBuffers[j];

                    if (buffer.GraphicsMemoryAllocation.GraphicsHeap.Type == GraphicsHeapType.Gpu)
                    {
                        if (transitionCommandListBefore == null)
                        {
                            transitionCommandListBefore = CreateCommandList(commandQueue, "TransitionCommandListBefore");
                        }

                        this.graphicsService.TransitionGraphicsBufferToState(transitionCommandListBefore.Value.NativePointer, buffer.NativePointer, GraphicsResourceState.StateShaderRead);
                    }
                }
            }

            var transitionCommandListCount = transitionCommandListBefore != null ? 2 : 0;
            var transitionCommandListBeforeCount = transitionCommandListBefore != null ? 1 : 0;

            var freeList = commandQueue.commandListFreeList;
            var commandListsPointers = ArrayPool<IntPtr>.Shared.Rent(commandLists.Length + transitionCommandListCount);

            if (transitionCommandListBefore != null)
            {
                this.CommitCommandList(transitionCommandListBefore.Value);
                commandListsPointers[0] = transitionCommandListBefore.Value.NativePointer;
            }
            
            for (var i = 0; i < commandLists.Length; i++)
            {
                commandListsPointers[i + transitionCommandListBeforeCount] = commandLists[i].NativePointer;
            }

            for (var i = 0; i < fencesToWait.Length; i++)
            {
                var commandQueueToWait = fencesToWait[i].CommandQueue;

                for (var j = 0; j < commandQueueToWait.CurrentCopyBuffers.Count; j++)
                {
                    var buffer = commandQueueToWait.CurrentCopyBuffers[j];

                    if (buffer.GraphicsMemoryAllocation.GraphicsHeap.Type == GraphicsHeapType.Gpu)
                    {
                        if (transitionCommandListAfter == null)
                        {
                            transitionCommandListAfter = CreateCommandList(commandQueue, "TransitionCommandListAfter");
                        }

                        this.graphicsService.TransitionGraphicsBufferToState(transitionCommandListAfter.Value.NativePointer, buffer.NativePointer, GraphicsResourceState.StateCommon);
                    }
                }

                commandQueueToWait.CurrentCopyBuffers.Clear();
            }

            if (transitionCommandListAfter != null)
            {
                this.CommitCommandList(transitionCommandListAfter.Value);
                commandListsPointers[commandLists.Length + transitionCommandListCount - 1] = transitionCommandListAfter.Value.NativePointer;
            }

            var fenceValue = this.graphicsService.ExecuteCommandLists(commandQueue.NativePointer, commandListsPointers.AsSpan(0..(commandLists.Length + transitionCommandListCount)), fencesToWaitArray.AsSpan(0..fencesToWait.Length));
            ArrayPool<IntPtr>.Shared.Return(commandListsPointers);
            ArrayPool<GraphicsFence>.Shared.Return(fencesToWaitArray);

            if (transitionCommandListBefore != null)
            {
                freeList.Push(transitionCommandListBefore.Value);
            }
            
            for (var i = 0; i < commandLists.Length; i++)
            {
                freeList.Push(commandLists[i]);
            }

            if (transitionCommandListAfter != null)
            {
                freeList.Push(transitionCommandListAfter.Value);
            }

            return new Fence(commandQueue, fenceValue);
        }

        public void WaitForCommandQueueOnCpu(Fence fenceToWait)
        {
            this.graphicsService.WaitForCommandQueueOnCpu(new GraphicsFence(fenceToWait));
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

        public GraphicsBuffer CreateGraphicsBuffer<T>(GraphicsHeapType heapType, GraphicsBufferUsage usage, int length, bool isStatic, string label) where T : struct
        {
            var sizeInBytes = (uint)Marshal.SizeOf(typeof(T)) * (uint)length;

            if (usage == GraphicsBufferUsage.IndirectCommands)
            {
                sizeInBytes += sizeof(uint);
            }

            var allocation = this.graphicsMemoryManager.AllocateBuffer(heapType, (int)sizeInBytes);
            var nativePointer1 = this.graphicsService.CreateGraphicsBuffer(allocation.GraphicsHeap.NativePointer, allocation.Offset, (HostServices.GraphicsBufferUsage)usage, (int)sizeInBytes);

            if (nativePointer1 == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
            }

            this.graphicsService.SetGraphicsBufferLabel(nativePointer1, $"{label}{(isStatic ? string.Empty : "0") }");

            IntPtr? nativePointer2 = null;
            GraphicsMemoryAllocation? allocation2 = null;

            if (!isStatic)
            {
                allocation2 = this.graphicsMemoryManager.AllocateBuffer(heapType, (int)sizeInBytes);
                nativePointer2 = this.graphicsService.CreateGraphicsBuffer(allocation2.Value.GraphicsHeap.NativePointer, allocation2.Value.Offset, (HostServices.GraphicsBufferUsage)usage, (int)sizeInBytes);

                if (nativePointer2.Value == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the graphics buffer resource.");
                }

                this.graphicsService.SetGraphicsBufferLabel(nativePointer2.Value, $"{label}1");
            }

            var graphicsBuffer = new GraphicsBuffer(this, allocation, allocation2, nativePointer1, nativePointer2, sizeInBytes, usage, isStatic, label);
            this.graphicsBuffers.Add(graphicsBuffer);

            if (heapType == GraphicsHeapType.Gpu)
            {
                this.shaderResourceManager.CreateShaderResourceBuffer(graphicsBuffer, isWriteable: false);

                if (usage == GraphicsBufferUsage.WriteableStorage || usage == GraphicsBufferUsage.IndirectCommands)
                {
                    // TODO: For the moment we create an UAV view for each buffer, is it needed?
                    this.shaderResourceManager.CreateShaderResourceBuffer(graphicsBuffer, isWriteable: true);
                }
            }

            return graphicsBuffer;
        }

        public unsafe void CopyDataToGraphicsBuffer<T>(GraphicsBuffer graphicsBuffer, int destinationOffset, ReadOnlySpan<T> data) where T : struct
        {
            if (graphicsBuffer == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffer));
            }

            if (graphicsBuffer.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.Upload)
            {
                throw new InvalidOperationException($"Graphics buffer '{graphicsBuffer.Label}' is not an upload buffer.");
            }

            var cpuPointer = this.graphicsService.GetGraphicsBufferCpuPointer(graphicsBuffer.NativePointer);

            var elementCount = graphicsBuffer.SizeInBytes / Marshal.SizeOf(typeof(T));
            var cpuSpan = new Span<T>(cpuPointer.ToPointer(), (int)elementCount).Slice(destinationOffset);
            data.CopyTo(cpuSpan);

            this.graphicsService.ReleaseGraphicsBufferCpuPointer(graphicsBuffer.NativePointer);
        }

        public unsafe ReadOnlySpan<T> CopyDataFromGraphicsBuffer<T>(GraphicsBuffer graphicsBuffer) where T : struct
        {
            if (graphicsBuffer == null)
            {
                throw new ArgumentNullException(nameof(graphicsBuffer));
            }

            if (graphicsBuffer.GraphicsMemoryAllocation.GraphicsHeap.Type != GraphicsHeapType.ReadBack)
            {
                throw new InvalidOperationException($"Graphics buffer '{graphicsBuffer.Label}' is not a readback buffer.");
            }
            
            var cpuPointer = this.graphicsService.GetGraphicsBufferCpuPointer(graphicsBuffer.NativePointer);

            var elementCount = graphicsBuffer.SizeInBytes / Marshal.SizeOf(typeof(T));

            var cpuSpan = new Span<T>(cpuPointer.ToPointer(), (int)elementCount);
            var outputData = new T[elementCount];
            cpuSpan.CopyTo(outputData);

            this.graphicsService.ReleaseGraphicsBufferCpuPointer(graphicsBuffer.NativePointer);

            return outputData;
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

            if (heapType == GraphicsHeapType.Gpu || heapType == GraphicsHeapType.TransientGpu)
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
            if (commandQueue.Type != CommandType.Present)
            {
                throw new ArgumentException("Command queue used by the swap-chain should be a present queue.", nameof(commandQueue));
            }

            var nativePointer = this.graphicsService.CreateSwapChain(window.NativePointer, commandQueue.NativePointer, width, height, (GraphicsTextureFormat)textureFormat);

            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("There was an error while creating the swap-chain.");
            }

            var swapChain = new SwapChain(this, nativePointer, commandQueue, width, height, textureFormat);
            this.swapChains.Add(swapChain);
            
            return swapChain;
        }

        public void DeleteSwapChain(SwapChain swapChain)
        {
            if (swapChain == null)
            {
                throw new ArgumentNullException(nameof(swapChain));
            }

            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting SwapChain...");
            }

            this.graphicsService.DeleteSwapChain(swapChain.NativePointer);

            // TODO: Use something faster here
            this.swapChains.Remove(swapChain);
        }

        internal void ScheduleDeleteSwapChain(SwapChain swapChain)
        {
            this.swapChainsToDelete[this.CurrentFrameNumber % 2].Add(swapChain);
        }

        internal GraphicsHeap CreateGraphicsHeap(GraphicsHeapType heapType, ulong sizeInBytes, string label)
        {
            var nativePointer = this.graphicsService.CreateGraphicsHeap((GraphicsServiceHeapType)heapType, sizeInBytes);
            
            if (nativePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Cannot create {label}.");
            }

            this.graphicsService.SetGraphicsHeapLabel(nativePointer, label);
            var graphicsHeap = new GraphicsHeap(this, nativePointer, heapType, sizeInBytes, label);
            this.graphicsHeaps.Add(graphicsHeap);

            return graphicsHeap;
        }

        public void DeleteGraphicsHeap(GraphicsHeap graphicsHeap)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Graphics Heap...");
            }

            this.graphicsService.DeleteGraphicsHeap(graphicsHeap.NativePointer);

            // TODO: Use something faster here
            this.graphicsHeaps.Remove(graphicsHeap);
        }

        internal void ScheduleDeleteGraphicsHeap(GraphicsHeap graphicsHeap)
        {
            this.graphicsHeapsToDelete[this.CurrentFrameNumber % 2].Add(graphicsHeap);
        }

        public void ResizeSwapChain(SwapChain swapChain, int width, int height)
        {
            if (swapChain == null)
            {
                throw new ArgumentNullException(nameof(swapChain));
            }

            if (width == 0 || height == 0)
            {
                return;
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

        public QueryBuffer CreateQueryBuffer(QueryBufferType queryBufferType, int length, string label)
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

            var queryBuffer = new QueryBuffer(this, nativePointer1, nativePointer2, length, label);
            this.queryBuffers.Add(queryBuffer);

            return queryBuffer;
        }

        public void ResetQueryBuffer(QueryBuffer queryBuffer)
        {
            if (queryBuffer is null)
            {
                throw new ArgumentNullException(nameof(queryBuffer));
            }

            this.graphicsService.ResetQueryBuffer(queryBuffer.NativePointer);
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

            // TODO: Use something faster here
            this.queryBuffers.Remove(queryBuffer);
        }

        internal void ScheduleDeleteQueryBuffer(QueryBuffer queryBuffer)
        {
            this.queryBuffersToDelete[this.CurrentFrameNumber % 2].Add(queryBuffer);
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

            if (shader.ComputePipelineState != null)
            {
                this.ScheduleDeletePipelineState(shader.ComputePipelineState.Value);
            }

            this.shadersToDelete[this.CurrentFrameNumber % 2].Add(shader);
        }

        private void DeleteShader(Shader shader)
        {
            if (logResourceAllocationInfos)
            {
                Logger.WriteMessage($"Deleting Shader {shader.Label}...");
            }

            shader.PipelineStates.Clear();
            this.graphicsService.DeleteShader(shader.NativePointer);

            // TODO: Use something faster here
            this.shaders.Remove(shader);
        }

        public void ResetIndirectCommandBuffer(CommandList commandList, GraphicsBuffer indirectCommandBuffer)
        {
            if (indirectCommandBuffer is null)
            {
                throw new ArgumentNullException(nameof(indirectCommandBuffer));
            }

            if (indirectCommandBuffer.Usage != GraphicsBufferUsage.IndirectCommands)
            {
                throw new InvalidOperationException("Graphics buffer is not an indirect command buffer");
            }

            this.CopyDataToGraphicsBuffer<uint>(commandList, indirectCommandBuffer, this.resetCounterBuffer, 1, indirectCommandBuffer.SizeInBytes - sizeof(uint));
        }

        public void CopyDataToGraphicsBuffer<T>(CommandList commandList, GraphicsBuffer destination, GraphicsBuffer source, uint length, uint destinationOffsetInBytes = 0) where T : struct
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

            var sizeInBytes = length * (uint)Marshal.SizeOf(typeof(T));

            if (sizeInBytes == 0)
            {
                throw new InvalidOperationException("Size In Bytes cannot be zero.");
            }

            // if (destination.GraphicsMemoryAllocation.GraphicsHeap.Type == GraphicsHeapType.Gpu)
            // {
            //     this.graphicsService.TransitionGraphicsBufferToState(commandList.NativePointer, destination.NativePointer, GraphicsResourceState.StateDestinationCopy);
                
            //     commandList.CommandQueue.CurrentCopyBuffers.Add(destination);
            // }

            this.graphicsService.CopyDataToGraphicsBuffer(commandList.NativePointer, destination.NativePointer, source.NativePointer, sizeInBytes, destinationOffsetInBytes);
            this.gpuMemoryUploaded += (int)sizeInBytes;

            // if (destination.GraphicsMemoryAllocation.GraphicsHeap.Type == GraphicsHeapType.Gpu)
            // {
            //     this.graphicsService.TransitionGraphicsBufferToState(commandList.NativePointer, destination.NativePointer, GraphicsResourceState.StateCommon);
            // }
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
            this.gpuMemoryUploaded += (int)source.SizeInBytes;
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

        public void SetShader(CommandList commandList, Shader shader)
        {
            SetShader(commandList, shader, null);
        }

        public void DispatchCompute(CommandList commandList, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            if (commandList.Type != CommandType.Compute)
            {
                throw new InvalidOperationException("The specified command list is not a compute command list.");
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

            this.cpuDispatchCount++;
            this.graphicsService.DispatchThreads(commandList.NativePointer, threadGroupCountX, threadGroupCountY, threadGroupCountZ);
        }

        // TODO: Add checks to all render functins to see if a render pass has been started
        public void BeginRenderPass(CommandList commandList, RenderPassDescriptor renderPassDescriptor, Shader shader)
        {
            if (commandList.Type != CommandType.Render && commandList.Type != CommandType.Present)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            var graphicsRenderPassDescriptor = new GraphicsRenderPassDescriptor(renderPassDescriptor);
            SetShader(commandList, shader, graphicsRenderPassDescriptor);
            graphicsService.BeginRenderPass(commandList.NativePointer, graphicsRenderPassDescriptor);
        }

        public void EndRenderPass(CommandList commandList)
        {
            if (commandList.Type != CommandType.Render && commandList.Type != CommandType.Present)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            this.graphicsService.EndRenderPass(commandList.NativePointer);
        }

        private void SetShader(CommandList commandList, Shader shader, GraphicsRenderPassDescriptor? renderPassDescriptor)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            if (!shader.IsLoaded || shader.NativePointer == IntPtr.Zero)
            {
                return;
            }

            this.shaderResourceManager.SetShaderResourceHeap(commandList);
            this.graphicsService.SetShader(commandList.NativePointer, shader.NativePointer);

            if (renderPassDescriptor != null && !shader.PipelineStates.ContainsKey(renderPassDescriptor.Value))
            {
                Logger.WriteMessage($"Create Pipeline State for shader {shader.Label}...");

                var nativePointer = this.graphicsService.CreatePipelineState(shader.NativePointer, renderPassDescriptor.Value);

                if (nativePointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the pipelinestate object.");
                }

                this.graphicsService.SetPipelineStateLabel(nativePointer, $"{shader.Label}PSO");

                var pipelineState = new PipelineState(this, nativePointer, $"{shader.Label}PSO");
                this.pipelineStates.Add(pipelineState);
                shader.PipelineStates.Add(renderPassDescriptor.Value, pipelineState);
            }

            else if (commandList.Type == CommandType.Compute && shader.ComputePipelineState == null)
            {
                Logger.WriteMessage($"Create Pipeline State for shader {shader.Label}...");

                var nativePointer = this.graphicsService.CreateComputePipelineState(shader.NativePointer);

                if (nativePointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException("There was an error while creating the pipelinestate object.");
                }

                this.graphicsService.SetPipelineStateLabel(nativePointer, $"{shader.Label}PSO");

                var pipelineState = new PipelineState(this, nativePointer, $"{shader.Label}PSO");

                this.pipelineStates.Add(pipelineState);
                shader.ComputePipelineState = pipelineState;
            }

            if (renderPassDescriptor != null)
            {
                this.graphicsService.SetPipelineState(commandList.NativePointer, shader.PipelineStates[renderPassDescriptor.Value].NativePointer);
            }

            else if (shader.ComputePipelineState != null)
            {
                this.graphicsService.SetPipelineState(commandList.NativePointer, shader.ComputePipelineState.Value.NativePointer);
            }
        }

        // TODO: Do another overload to be able to specify a struct of uint instead?
        public void SetShaderParameterValues(CommandList commandList, uint slot, ReadOnlySpan<uint> values)
        {
            this.graphicsService.SetShaderParameterValues(commandList.NativePointer, slot, values);
        }

        public void DispatchMesh(CommandList commandList, uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            if (commandList.Type != CommandType.Render && commandList.Type != CommandType.Present)
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

        public void ExecuteIndirect(CommandList commandList, uint maxCommandCount, GraphicsBuffer commandGraphicsBuffer, uint commandBufferOffset)
        {
            if (commandList.Type != CommandType.Render && commandList.Type != CommandType.Present)
            {
                throw new InvalidOperationException("The specified command list is not a render command list.");
            }

            if (commandGraphicsBuffer is null)
            {
                throw new ArgumentNullException(nameof(commandGraphicsBuffer));
            }

            this.graphicsService.ExecuteIndirect(commandList.NativePointer, maxCommandCount, commandGraphicsBuffer.NativePointer, commandBufferOffset);
        }

        public void BeginQuery(CommandList commandList, QueryBuffer queryBuffer, int index)
        {
            if (queryBuffer == null)
            {
                throw new ArgumentNullException(nameof(queryBuffer));
            }

            this.graphicsService.BeginQuery(commandList.NativePointer, queryBuffer.NativePointer, index);
        }

        public void EndQuery(CommandList commandList, QueryBuffer queryBuffer, int index)
        {
            if (queryBuffer == null)
            {
                throw new ArgumentNullException(nameof(queryBuffer));
            }

            this.graphicsService.EndQuery(commandList.NativePointer, queryBuffer.NativePointer, index);
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

            for (var i = 0; i < this.queryBuffersToDelete[this.CurrentFrameNumber % 2].Count; i++)
            {
                this.DeleteQueryBuffer(this.queryBuffersToDelete[this.CurrentFrameNumber % 2][i]);
            }

            this.queryBuffersToDelete[this.CurrentFrameNumber % 2].Clear();

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

            for (var i = 0; i < this.graphicsHeapsToDelete[this.CurrentFrameNumber % 2].Count; i++)
            {
                this.DeleteGraphicsHeap(this.graphicsHeapsToDelete[this.CurrentFrameNumber % 2][i]);
            }

            this.graphicsHeapsToDelete[this.CurrentFrameNumber % 2].Clear();
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            resourcesManager.AddResourceLoader(new ShaderResourceLoader(resourcesManager, this));
        }
    }
}
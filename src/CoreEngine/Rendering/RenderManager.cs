using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;
using CoreEngine.HostServices; // TODO: Remove that using
using CoreEngine.UI.Native;
using CoreEngine.Inputs;

namespace CoreEngine.Rendering
{
    public class GpuTiming
    {
        public GpuTiming(string name, QueryBufferType type, int startIndex, int endIndex)
        {
            this.Name = name;
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
            this.Type = type;
            this.StartTiming = 0.0;
            this.EndTiming = 0.0;
            this.StartTimestamp = 0;
            this.EndTimestamp = 0;
        }

        public string Name { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }
        public QueryBufferType Type { get; }
        public ulong StartTimestamp { get; set; }
        public ulong EndTimestamp { get; set; }
        public double StartTiming { get; set; }
        public double EndTiming { get; set; }
    }

    public class RenderManager : SystemManager, IDisposable
    {
        private readonly GraphicsManager graphicsManager;
        private readonly NativeUIManager nativeUIManager;

        private Vector2 currentFrameSize;
        private Stopwatch stopwatch;
        
        private Stopwatch globalStopwatch;
        private uint startMeasureFrameNumber;
        private int framePerSeconds;
        private int unlockedFramePerSeconds;
        private int gpuMemoryUploadedPerSeconds;
        private double lastGpuDuration;

        private Shader computeDirectTransferShader;

        private QueryBuffer globalQueryBuffer;
        private GraphicsBuffer globalCpuQueryBuffer;
        private int currentQueryIndex;

        private QueryBuffer globalCopyQueryBuffer;
        private GraphicsBuffer globalCpuCopyQueryBuffer;
        private int currentCopyQueryIndex;

        private List<GpuTiming> gpuTimings;
        private List<GpuTiming>[] gpuTimingsList = new List<GpuTiming>[2];
        private List<GpuTiming> currentGpuTimings;

        private Window window;
        private Vector2 currentWindowRenderSize;
        private SwapChain swapChain;
        private CommandQueue presentQueue;

        private Fence? presentFence;

        // TODO: Each Render Manager should use their own Graphics Manager
        public RenderManager(Window window, NativeUIManager nativeUIManager, GraphicsManager graphicsManager, ResourcesManager resourcesManager, GraphicsSceneQueue graphicsSceneQueue)
        {
            this.nativeUIManager = nativeUIManager ?? throw new ArgumentNullException(nameof(nativeUIManager));
            this.graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.window = window;
            this.lastGpuDuration = 0.0;
            this.unlockedFramePerSeconds = 0;

            this.CopyCommandQueue = this.graphicsManager.CreateCommandQueue(CommandType.Copy, "CopyCommandQueue");
            this.ComputeCommandQueue = this.graphicsManager.CreateCommandQueue(CommandType.Compute, "ComputeCommandQueue");
            this.RenderCommandQueue = this.graphicsManager.CreateCommandQueue(CommandType.Render, "RenderCommandQueue");
            this.presentQueue = this.graphicsManager.CreateCommandQueue(CommandType.Present, "PresentCommandQueue");

            // TODO: To remove, TESTS
            this.currentWindowRenderSize = nativeUIManager.GetWindowRenderSize(this.window);

            this.swapChain = graphicsManager.CreateSwapChain(window, this.presentQueue, (int)this.currentWindowRenderSize.X, (int)this.currentWindowRenderSize.Y, TextureFormat.Bgra8UnormSrgb);

            InitResourceLoaders(resourcesManager);

            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
            this.globalStopwatch = new Stopwatch();
            this.globalStopwatch.Start();

            this.currentFrameSize = GetRenderSize();
            this.computeDirectTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeDirectTransfer.shader");

            this.GraphicsSceneRenderer = new GraphicsSceneRenderer(this, this.graphicsManager, graphicsSceneQueue, resourcesManager);
            this.Graphics2DRenderer = new Graphics2DRenderer(this, this.graphicsManager, resourcesManager);
            
            // TODO: TESTS: TO REMOVE
            this.globalQueryBuffer = this.graphicsManager.CreateQueryBuffer(GraphicsQueryBufferType.Timestamp, 1000, "RendererQueryBuffer");
            this.globalCpuQueryBuffer = this.graphicsManager.CreateGraphicsBuffer<ulong>(GraphicsHeapType.ReadBack, 1000, isStatic: false, "RendererCpuQueryBuffer");

            this.globalCopyQueryBuffer = this.graphicsManager.CreateQueryBuffer(GraphicsQueryBufferType.CopyTimestamp, 1000, "RendererCopyQueryBuffer");
            this.globalCpuCopyQueryBuffer = this.graphicsManager.CreateGraphicsBuffer<ulong>(GraphicsHeapType.ReadBack, 1000, isStatic: false, "RendererCpuCopyQueryBuffer");

            this.gpuTimingsList[0] = new List<GpuTiming>();
            this.gpuTimingsList[1] = new List<GpuTiming>();

            this.gpuTimings = this.gpuTimingsList[0];
            this.currentGpuTimings = new List<GpuTiming>(this.gpuTimings);
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
                if (this.presentFence != null)
                {
                    this.graphicsManager.WaitForCommandQueueOnCpu(this.presentFence.Value);
                }

                this.globalCpuCopyQueryBuffer.Dispose();
                this.globalCpuQueryBuffer.Dispose();

                this.globalCopyQueryBuffer.Dispose();
                this.globalQueryBuffer.Dispose();

                this.globalCpuQueryBuffer.Dispose();

                this.swapChain.Dispose();

                this.computeDirectTransferShader.Dispose();
                this.CopyCommandQueue.Dispose();
                this.ComputeCommandQueue.Dispose();
                this.RenderCommandQueue.Dispose();
                this.presentQueue.Dispose();
            }
        }

        public CommandQueue CopyCommandQueue { get; }
        public CommandQueue ComputeCommandQueue { get; }
        public CommandQueue RenderCommandQueue { get; }

        public GraphicsSceneRenderer GraphicsSceneRenderer { get; }
        public Graphics2DRenderer Graphics2DRenderer { get; }
        internal int MeshletCount { get; set; }
        internal int TriangleCount { get; set; }
        internal int CulledGeometryInstancesCount { get; set; }
        internal Vector2 MainCameraDepth { get; set; }
        internal int MeshCount { get; set; }
        internal int MeshInstanceCount { get; set; }
        internal int MaterialsCount { get; set; }
        internal int TexturesCount { get; set; }
        internal int LightsCount { get; set; }

        public Vector2 GetRenderSize()
        {
            return new Vector2(this.swapChain.Width, this.swapChain.Height);
        }

        public int InsertQueryTimestamp(CommandList commandList)
        {
            if (commandList.Type == CommandType.Copy)
            {
                var queryIndex = this.currentCopyQueryIndex;
                this.graphicsManager.EndQuery(commandList, this.globalCopyQueryBuffer, this.currentCopyQueryIndex++);

                return queryIndex;
            }

            else
            {
                var queryIndex = this.currentQueryIndex;
                this.graphicsManager.EndQuery(commandList, this.globalQueryBuffer, this.currentQueryIndex++);

                return queryIndex;
            }
        }

        public void AddGpuTiming(string name, QueryBufferType type, int startQueryIndex, int endQueryIndex)
        {
            this.gpuTimings.Add(new GpuTiming(name, type, startQueryIndex, endQueryIndex));
        }

        List<GpuTiming> previousGpuTiming = new List<GpuTiming>();
        internal bool logFrameTime;

        internal void WaitForSwapChainOnCpu()
        {
            this.graphicsManager.WaitForSwapChainOnCpu(this.swapChain);
            this.stopwatch.Restart();
        }

        internal void Render()
        {
            // TODO: Rename that to Reset
            this.graphicsManager.MoveToNextFrame();
            ResetGpuTimers();

            if (this.graphicsManager.CurrentFrameNumber > 1)
            {
                this.graphicsManager.ResetCommandQueue(this.RenderCommandQueue);
                this.graphicsManager.ResetCommandQueue(this.ComputeCommandQueue);
                this.graphicsManager.ResetCommandQueue(this.CopyCommandQueue);
                this.graphicsManager.ResetCommandQueue(this.presentQueue);
            }

            var windowRenderSize = this.nativeUIManager.GetWindowRenderSize(this.window);

            if (windowRenderSize != this.currentWindowRenderSize)
            {
                this.currentWindowRenderSize = windowRenderSize;
                graphicsManager.ResizeSwapChain(this.swapChain, (int)windowRenderSize.X, (int)windowRenderSize.Y);
            }
            
            this.currentFrameSize = GetRenderSize();

            // TODO: Resize the swap chain
            using var mainRenderTargetTexture = this.graphicsManager.CreateTexture(GraphicsHeapType.TransientGpu, TextureFormat.Rgba16Float, TextureUsage.RenderTarget, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, 1, isStatic: true, label: "MainRenderTarget");

            if (logFrameTime)
            {
                Logger.BeginAction($"SceneRenderer (FrameSize: {this.currentFrameSize})");
            }

            this.GraphicsSceneRenderer.Render(mainRenderTargetTexture);
            
            if (logFrameTime)
            {
                Logger.EndAction();
            }

            DrawDebugMessages();
            var fence = this.Graphics2DRenderer.Render(mainRenderTargetTexture);
            PresentScreenBuffer(mainRenderTargetTexture, fence);
        }

        private void PresentScreenBuffer(Texture mainRenderTargetTexture, Fence? fenceToWait)
        {
            var resolveCopyCountersCommandList = this.graphicsManager.CreateCommandList(this.CopyCommandQueue, "ResolveCopyCounters");
            this.graphicsManager.ResolveQueryData(resolveCopyCountersCommandList, this.globalCopyQueryBuffer, this.globalCpuCopyQueryBuffer, 0..this.currentCopyQueryIndex);
            this.graphicsManager.CommitCommandList(resolveCopyCountersCommandList);
            this.graphicsManager.ExecuteCommandLists(this.CopyCommandQueue, new CommandList[] { resolveCopyCountersCommandList });

            var presentCommandList = this.graphicsManager.CreateCommandList(this.presentQueue, "PresentScreenBuffer");

            var backBufferTexture = this.graphicsManager.GetSwapChainBackBufferTexture(this.swapChain);
            var renderTarget = new RenderTargetDescriptor(backBufferTexture, null, BlendOperation.None);
            var renderPassDescriptor2 = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true, PrimitiveType.Triangle);
            this.graphicsManager.BeginRenderPass(presentCommandList, renderPassDescriptor2, this.computeDirectTransferShader);
            var startQueryIndex = InsertQueryTimestamp(presentCommandList);
            this.graphicsManager.SetShaderParameterValues(presentCommandList, 0, new uint[] { mainRenderTargetTexture.ShaderResourceIndex });
            this.graphicsManager.DispatchMesh(presentCommandList, 1, 1, 1);
            var endQueryIndex = InsertQueryTimestamp(presentCommandList);
            this.graphicsManager.EndRenderPass(presentCommandList);
            this.graphicsManager.ResolveQueryData(presentCommandList, this.globalQueryBuffer, this.globalCpuQueryBuffer, 0..this.currentQueryIndex);
            this.graphicsManager.CommitCommandList(presentCommandList);

            AddGpuTiming("PresentScreenBuffer", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            this.presentFence = this.graphicsManager.ExecuteCommandLists(this.presentQueue, new CommandList[] { presentCommandList }, fenceToWait.HasValue ? new Fence[] { fenceToWait.Value } : Array.Empty<Fence>());
            this.graphicsManager.PresentSwapChain(this.swapChain);
        }

        private void ResetGpuTimers()
        {
            this.currentQueryIndex = 0;
            this.currentCopyQueryIndex = 0;
            this.gpuTimings = this.gpuTimingsList[(int)this.graphicsManager.CurrentFrameNumber % 2];
            
            this.currentGpuTimings.Clear();
            this.currentGpuTimings.AddRange(this.gpuTimings);
            this.gpuTimings.Clear();

            // TODO: Get the frequency only once
            var copyQueueFrequency = this.graphicsManager.GetCommandQueueTimestampFrequency(this.CopyCommandQueue);
            var renderQueueFrequency = this.graphicsManager.GetCommandQueueTimestampFrequency(this.RenderCommandQueue);

            this.graphicsManager.ResetQueryBuffer(this.globalQueryBuffer);
            this.graphicsManager.ResetQueryBuffer(this.globalCopyQueryBuffer);

            var queryData = this.graphicsManager.CopyDataFromGraphicsBuffer<ulong>(this.globalCpuQueryBuffer);
            var queryCopyData = this.graphicsManager.CopyDataFromGraphicsBuffer<ulong>(this.globalCpuCopyQueryBuffer);

            for (var i = 0; i < this.currentGpuTimings.Count; i++)
            {
                var gpuTiming = this.currentGpuTimings[i];

                var frequency = renderQueueFrequency;

                if (gpuTiming.Type == QueryBufferType.CopyTimestamp)
                {
                    gpuTiming.StartTimestamp = queryCopyData[gpuTiming.StartIndex];
                    gpuTiming.EndTimestamp = queryCopyData[gpuTiming.EndIndex];

                    frequency = copyQueueFrequency;
                }

                else
                {
                    gpuTiming.StartTimestamp = queryData[gpuTiming.StartIndex];
                    gpuTiming.EndTimestamp = queryData[gpuTiming.EndIndex];
                }

                gpuTiming.StartTiming = gpuTiming.StartTimestamp / (double)frequency * 1000.0;
                gpuTiming.EndTiming = gpuTiming.EndTimestamp / (double)frequency * 1000.0;
            }
        }

        #if DEBUG
        private readonly string compilationConfiguration = "Debug";
        #else
        private readonly string compilationConfiguration = "Release";
        #endif

        private void DrawDebugMessages()
        {
            // TODO: Seperate timing calculations from debug display
            var frameDuration = (float)this.stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;

            if (this.globalStopwatch.ElapsedMilliseconds > 1000)
            {
                this.framePerSeconds = (int)(this.graphicsManager.CurrentFrameNumber - startMeasureFrameNumber - 1);
                this.unlockedFramePerSeconds = (int)(1000.0 / (this.lastGpuDuration / this.framePerSeconds));
                this.globalStopwatch.Restart();
                this.startMeasureFrameNumber = this.graphicsManager.CurrentFrameNumber;

                this.gpuMemoryUploadedPerSeconds = this.graphicsManager.gpuMemoryUploaded;
                this.graphicsManager.gpuMemoryUploaded = 0;
                this.lastGpuDuration = 0.0;
            }

            this.Graphics2DRenderer.DrawText($"{this.graphicsManager.graphicsAdapterName} - {this.currentFrameSize.X}x{this.currentFrameSize.Y} - FPS: {framePerSeconds} (Unlocked: {unlockedFramePerSeconds}) - {compilationConfiguration}", new Vector2(10, 10));
            this.Graphics2DRenderer.DrawText($"    Allocated Memory: {Utils.BytesToMegaBytes(this.graphicsManager.AllocatedGpuMemory + this.graphicsManager.AllocatedTransientGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)} MB (Static: {Utils.BytesToMegaBytes(this.graphicsManager.AllocatedGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)}/{Utils.BytesToMegaBytes(this.graphicsManager.TotalGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)}, Transient: {Utils.BytesToMegaBytes(this.graphicsManager.AllocatedTransientGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)}/{Utils.BytesToMegaBytes(this.graphicsManager.TotalTransientGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)})", new Vector2(10, 90));
            this.Graphics2DRenderer.DrawText($"    Memory Bandwidth: {Utils.BytesToMegaBytes((ulong)this.gpuMemoryUploadedPerSeconds).ToString("0.00", CultureInfo.InvariantCulture)} MB/s", new Vector2(10, 130));
            this.Graphics2DRenderer.DrawText($"Cpu Frame Duration: {frameDuration.ToString("0.00", CultureInfo.InvariantCulture)} ms", new Vector2(10, 170));
            this.Graphics2DRenderer.DrawText($"    Meshlets: {this.MeshletCount.ToString("#,###", CultureInfo.InvariantCulture)} (Mesh Count: {this.MeshCount}, Instance Count: {this.MeshInstanceCount}, Triangle Count: {this.TriangleCount.ToString("#,###", CultureInfo.InvariantCulture)})", new Vector2(10, 210));
            this.Graphics2DRenderer.DrawText($"    Materials: {this.MaterialsCount}", new Vector2(10, 250));
            this.Graphics2DRenderer.DrawText($"    Textures: {this.TexturesCount}", new Vector2(10, 290));
            this.Graphics2DRenderer.DrawText($"    Lights: {this.LightsCount}", new Vector2(10, 330));
            this.Graphics2DRenderer.DrawText($"Gpu Pipeline: (Depth: {this.MainCameraDepth})", new Vector2(10, 370));

            // this.currentGpuTimings.Sort((a, b) => 
            // {
            //     // var result = Math.Round(a.StartTiming, 2).CompareTo(Math.Round(b.StartTiming, 2));
            //     var result = a.StartTiming.CompareTo(b.StartTiming);

            //     if (result == 0)
            //     {
            //         return a.Name.CompareTo(b.Name);
            //     }

            //     return result;
            // });

            var startGpuTiming = 0.0;
            var endGpuTiming = 0.0;
            var previousEndGpuTiming = 0.0;
            var gpuExecutionTime = 0.0;

            for (var i = 0; i < this.currentGpuTimings.Count; i++)
            {
                var gpuTiming = this.currentGpuTimings[i];

                if (startGpuTiming == 0.0)
                {
                    startGpuTiming = gpuTiming.StartTiming;
                }

                if (gpuTiming.EndTiming > endGpuTiming)
                {
                    endGpuTiming = gpuTiming.EndTiming;
                }

                var duration = gpuTiming.EndTiming - gpuTiming.StartTiming;

                this.Graphics2DRenderer.DrawText($"    {gpuTiming.Name}: {duration.ToString("0.00", CultureInfo.InvariantCulture)} ms ({(gpuTiming.StartTiming != 0 ? gpuTiming.StartTiming - startGpuTiming : 0).ToString("0.00", CultureInfo.InvariantCulture)} ms)", new Vector2(10, 410 + i * 40));

                gpuExecutionTime += duration;

                var interval = gpuTiming.StartTiming - previousEndGpuTiming;

                // if (interval < 0.0)
                // {
                //     gpuExecutionTime += interval;
                // }

                previousEndGpuTiming = gpuTiming.EndTiming;
            }

            gpuExecutionTime = endGpuTiming - startGpuTiming;
            this.lastGpuDuration += gpuExecutionTime;

            this.Graphics2DRenderer.DrawText($"Gpu Frame Duration: {gpuExecutionTime.ToString("0.00", CultureInfo.InvariantCulture)} ms", new Vector2(10, 50));

            this.previousGpuTiming.Clear();
            this.previousGpuTiming.AddRange(this.currentGpuTimings);
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            resourcesManager.AddResourceLoader(new TextureResourceLoader(resourcesManager, this, this.graphicsManager));
            resourcesManager.AddResourceLoader(new FontResourceLoader(resourcesManager, this, this.graphicsManager));
            resourcesManager.AddResourceLoader(new MaterialResourceLoader(resourcesManager, this, this.graphicsManager));
            resourcesManager.AddResourceLoader(new MeshResourceLoader(resourcesManager));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;
using CoreEngine.HostServices; // TODO: Remove that using

namespace CoreEngine.Rendering
{
    public class RenderManager : SystemManager
    {
        private readonly GraphicsManager graphicsManager;

        private Vector2 currentFrameSize;
        private Stopwatch stopwatch;
        
        private Stopwatch globalStopwatch;
        private uint startMeasureFrameNumber;
        private int framePerSeconds;
        private int gpuMemoryUploadedPerSeconds;

        private CommandBuffer presentCommandBuffer;
        private Shader computeDirectTransferShader;

        private QueryBuffer globalQueryBuffer;
        private int currentQueryIndex;

        public RenderManager(GraphicsManager graphicsManager, ResourcesManager resourcesManager, GraphicsSceneQueue graphicsSceneQueue)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager;

            InitResourceLoaders(resourcesManager);
            
            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
            this.globalStopwatch = new Stopwatch();
            this.globalStopwatch.Start();

            this.currentFrameSize = this.graphicsManager.graphicsService.GetRenderSize();
            this.computeDirectTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeDirectTransfer.shader");

            this.GraphicsSceneRenderer = new GraphicsSceneRenderer(this, this.graphicsManager, graphicsSceneQueue, resourcesManager);
            this.Graphics2DRenderer = new Graphics2DRenderer(this, this.graphicsManager, resourcesManager);
            
            this.presentCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Render, "PresentScreenBuffer");

            // TODO: TESTS: TO REMOVE
            this.globalQueryBuffer = this.graphicsManager.CreateQueryBuffer(GraphicsQueryBufferType.Timestamp, 1000, "RendererQueryBuffer");
        }

        public GraphicsSceneRenderer GraphicsSceneRenderer { get; }
        public Graphics2DRenderer Graphics2DRenderer { get; }
        internal int GeometryInstancesCount { get; set; }
        internal int CulledGeometryInstancesCount { get; set; }
        internal Vector2 MainCameraDepth { get; set; }
        internal int MaterialsCount { get; set; }
        internal int TexturesCount { get; set; }
        internal int LightsCount { get; set; }

        List<GpuTiming> previousGpuTiming = new List<GpuTiming>();

        internal void Render()
        {
            this.currentFrameSize = this.graphicsManager.graphicsService.GetRenderSize();
            var mainRenderTargetTexture = this.graphicsManager.CreateTexture(GraphicsHeapType.TransientGpu, TextureFormat.Rgba16Float, TextureUsage.RenderTarget, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, 1, isStatic: true, label: "MainRenderTarget");

            var renderCommandList = this.GraphicsSceneRenderer.Render(mainRenderTargetTexture);

            DrawDebugMessages();
            var graphics2DCommandList = this.Graphics2DRenderer.Render(mainRenderTargetTexture, renderCommandList);

            this.PresentScreenBuffer(mainRenderTargetTexture, graphics2DCommandList);

            // TODO: If doing restart stopwatch here, the CPU time is more than 10ms
            this.stopwatch.Restart();
        }

        private void PresentScreenBuffer(Texture mainRenderTargetTexture, CommandList previousCommandList)
        {
            // TODO: Use a compute shader
            this.graphicsManager.ResetCommandBuffer(presentCommandBuffer);

            var renderPassDescriptor = new RenderPassDescriptor(null, null, DepthBufferOperation.None, true, PrimitiveType.TriangleStrip);
            var renderCommandList = this.graphicsManager.CreateRenderCommandList(presentCommandBuffer, renderPassDescriptor, "PresentRenderCommandList");

            this.graphicsManager.WaitForCommandList(renderCommandList, previousCommandList);

            this.graphicsManager.QueryTimestamp(renderCommandList, this.globalQueryBuffer, this.currentQueryIndex++);

            this.graphicsManager.SetShader(renderCommandList, this.computeDirectTransferShader);
            this.graphicsManager.SetShaderTexture(renderCommandList, mainRenderTargetTexture, 0);
            this.graphicsManager.DrawPrimitives(renderCommandList, PrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.QueryTimestamp(renderCommandList, this.globalQueryBuffer, this.currentQueryIndex++);
            this.graphicsManager.CommitRenderCommandList(renderCommandList);

            this.graphicsManager.graphicsService.PresentScreenBuffer(presentCommandBuffer.GraphicsResourceId);
            this.graphicsManager.ExecuteCommandBuffer(presentCommandBuffer);

            this.graphicsManager.WaitForAvailableScreenBuffer();
            this.currentQueryIndex = 0;
            
            var queryData = this.graphicsManager.GetCpuQueryBufferPointer<ulong>(this.globalQueryBuffer);

            // TODO: Add a query GPU frequency method, it works for now because AMG Radeon 580 Pro is using nano seconds timestamps
            var startNano = queryData[0];
            var endNano = queryData[1];
            Logger.WriteMessage($"{queryData.Length} - {(double)(endNano - startNano) / 1000000.0}");
        }

        private void DrawDebugMessages()
        {
            // TODO: Seperate timing calculations from debug display

            var frameDuration = (float)this.stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;

            if (this.globalStopwatch.ElapsedMilliseconds > 1000)
            {
                this.framePerSeconds = (int)(this.graphicsManager.CurrentFrameNumber - startMeasureFrameNumber - 1);
                this.globalStopwatch.Restart();
                this.startMeasureFrameNumber = this.graphicsManager.CurrentFrameNumber;

                this.gpuMemoryUploadedPerSeconds = this.graphicsManager.gpuMemoryUploaded;
                this.graphicsManager.gpuMemoryUploaded = 0;
            }

            var renderSize = this.graphicsManager.GetRenderSize();

            var commandBufferStatus = this.graphicsManager.graphicsService.GetCommandBufferStatus(this.Graphics2DRenderer.commandBuffer.GraphicsResourceId);

            if (commandBufferStatus != null && commandBufferStatus.Value.State == GraphicsCommandBufferState.Completed)
            {
                this.graphicsManager.gpuTimings.Add(new GpuTiming(this.Graphics2DRenderer.commandBuffer.Label, commandBufferStatus.Value.ExecutionStartTime, commandBufferStatus.Value.ExecutionEndTime));
            }

            commandBufferStatus = this.graphicsManager.graphicsService.GetCommandBufferStatus(this.Graphics2DRenderer.copyCommandBuffer.GraphicsResourceId);

            if (commandBufferStatus != null && commandBufferStatus.Value.State == GraphicsCommandBufferState.Completed)
            {
                this.graphicsManager.gpuTimings.Add(new GpuTiming(this.Graphics2DRenderer.copyCommandBuffer.Label, commandBufferStatus.Value.ExecutionStartTime, commandBufferStatus.Value.ExecutionEndTime));
            }

            commandBufferStatus = this.graphicsManager.graphicsService.GetCommandBufferStatus(this.presentCommandBuffer.GraphicsResourceId);

            if (commandBufferStatus != null && commandBufferStatus.Value.State == GraphicsCommandBufferState.Completed)
            {
                this.graphicsManager.gpuTimings.Add(new GpuTiming(this.presentCommandBuffer.Label, commandBufferStatus.Value.ExecutionStartTime, commandBufferStatus.Value.ExecutionEndTime));
            }

            this.Graphics2DRenderer.DrawText($"{this.graphicsManager.graphicsAdapterName} - {renderSize.X}x{renderSize.Y} - FPS: {framePerSeconds}", new Vector2(10, 10));
            this.Graphics2DRenderer.DrawText($"    Allocated Memory: {Utils.BytesToMegaBytes(this.graphicsManager.AllocatedGpuMemory + this.graphicsManager.AllocatedTransientGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)} MB (Static: {Utils.BytesToMegaBytes(this.graphicsManager.AllocatedGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)}, Transient: {Utils.BytesToMegaBytes(this.graphicsManager.AllocatedTransientGpuMemory).ToString("0.00", CultureInfo.InvariantCulture)})", new Vector2(10, 90));
            this.Graphics2DRenderer.DrawText($"    Memory Bandwidth: {Utils.BytesToMegaBytes((ulong)this.gpuMemoryUploadedPerSeconds).ToString("0.00", CultureInfo.InvariantCulture)} MB/s", new Vector2(10, 130));
            this.Graphics2DRenderer.DrawText($"Cpu Frame Duration: {frameDuration.ToString("0.00", CultureInfo.InvariantCulture)} ms", new Vector2(10, 170));
            this.Graphics2DRenderer.DrawText($"    GeometryInstances: {this.CulledGeometryInstancesCount}/{this.GeometryInstancesCount}", new Vector2(10, 210));
            this.Graphics2DRenderer.DrawText($"    Materials: {this.MaterialsCount}", new Vector2(10, 250));
            this.Graphics2DRenderer.DrawText($"    Textures: {this.TexturesCount}", new Vector2(10, 290));
            this.Graphics2DRenderer.DrawText($"    Lights: {this.LightsCount}", new Vector2(10, 330));
            this.Graphics2DRenderer.DrawText($"Gpu Pipeline: (Depth: {this.MainCameraDepth})", new Vector2(10, 370));

            this.graphicsManager.gpuTimings.Sort((a, b) => 
            {
                var result = Math.Round(a.StartTiming, 2).CompareTo(Math.Round(b.StartTiming, 2));

                if (result == 0)
                {
                    return a.Name.CompareTo(b.Name);
                }

                return result;
            });

            if (this.graphicsManager.gpuTimings.Count < this.previousGpuTiming.Count)
            {
                Logger.WriteMessage($"Error GPU Timings {this.graphicsManager.gpuTimings.Count}/{this.previousGpuTiming.Count}");

                foreach (var timing in this.previousGpuTiming)
                {
                    if (this.graphicsManager.gpuTimings.Find(x => x.Name == timing.Name).StartTiming == 0.0)
                    {
                        Logger.WriteMessage($"Gpu timing: {timing.Name}");
                    }
                }
            }

            var startGpuTiming = 0.0;
            var previousEndGpuTiming = 0.0;
            var gpuExecutionTime = 0.0;

            for (var i = 0; i < this.graphicsManager.gpuTimings.Count; i++)
            {
                var gpuTiming = this.graphicsManager.gpuTimings[i];

                if (startGpuTiming == 0.0)
                {
                    startGpuTiming = gpuTiming.StartTiming;
                }

                var duration = gpuTiming.EndTiming - gpuTiming.StartTiming;

                this.Graphics2DRenderer.DrawText($"    {gpuTiming.Name}: {duration.ToString("0.00", CultureInfo.InvariantCulture)} ms ({(gpuTiming.StartTiming - startGpuTiming).ToString("0.00", CultureInfo.InvariantCulture)} ms)", new Vector2(10, 410 + i * 40));

                gpuExecutionTime += duration;

                var interval = gpuTiming.StartTiming - previousEndGpuTiming;

                if (interval < 0.0)
                {
                    gpuExecutionTime += interval;
                }

                previousEndGpuTiming = gpuTiming.EndTiming;
            }

            this.Graphics2DRenderer.DrawText($"Gpu Frame Duration: {gpuExecutionTime.ToString("0.00", CultureInfo.InvariantCulture)} ms", new Vector2(10, 50));

            this.previousGpuTiming = new List<GpuTiming>(this.graphicsManager.gpuTimings);
        }

        private void InitResourceLoaders(ResourcesManager resourcesManager)
        {
            resourcesManager.AddResourceLoader(new FontResourceLoader(resourcesManager, this.graphicsManager));
            resourcesManager.AddResourceLoader(new MaterialResourceLoader(resourcesManager, this.graphicsManager));
            resourcesManager.AddResourceLoader(new MeshResourceLoader(resourcesManager, this.graphicsManager));
        }
    }
}
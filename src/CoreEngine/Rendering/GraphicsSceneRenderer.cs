using System;
using System.Collections.Generic;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;
using System.IO;

namespace CoreEngine.Rendering
{
    readonly struct ShaderBoundingFrustum
    {
        public ShaderBoundingFrustum(BoundingFrustum boundingFrustum)
        {
            this.LeftPlane = new Vector4(boundingFrustum.LeftPlane.Normal, boundingFrustum.LeftPlane.D);
            this.RightPlane = new Vector4(boundingFrustum.RightPlane.Normal, boundingFrustum.RightPlane.D);
            this.TopPlane = new Vector4(boundingFrustum.TopPlane.Normal, boundingFrustum.TopPlane.D);
            this.BottomPlane = new Vector4(boundingFrustum.BottomPlane.Normal, boundingFrustum.BottomPlane.D);
            this.NearPlane = new Vector4(boundingFrustum.NearPlane.Normal, boundingFrustum.NearPlane.D);
            this.FarPlane = new Vector4(boundingFrustum.FarPlane.Normal, boundingFrustum.FarPlane.D);
        }

        public Vector4 LeftPlane { get; }
        public Vector4 RightPlane { get; }
        public Vector4 TopPlane { get; }
        public Vector4 BottomPlane { get; }
        public Vector4 NearPlane { get; }
        public Vector4 FarPlane { get; }
    }

    readonly struct ShaderCamera
    {
        public Vector3 WorldPosition { get; init; }
        public Matrix4x4 ViewMatrix { get; init; }
        public Matrix4x4 ViewProjectionMatrix { get; init; }
        public ShaderBoundingFrustum BoundingFrustum { get; init; }
    }

    readonly struct ShaderLight
    {
        public Vector3 WorldSpacePosition { get; init; }
        public Vector3 Color { get; init; }
        public int LightType { get; init; }
    }

    readonly struct ShaderMesh
    {
        public uint MeshletCount { get; init; }
        public uint VerticesBufferIndex { get; init; }
        public uint VertexIndicesBufferIndex { get; init; }
        public uint TriangleIndicesBufferIndex { get; init; }
        public uint MeshletBufferIndex { get; init; }
        public BoundingBox BoundingBox { get; init; }
    }

    readonly struct ShaderMeshInstance
    {
        public uint MeshIndex { get; init; }
        public float Scale { get; init; }
        public ShaderMatrix4x3 WorldMatrix { get; init; }
        public ShaderMatrix3x3 WorldInvTransposeMatrix { get; init; }
    }

    public readonly struct DispatchMeshIndirectParam
    {
        public uint CamerasBuffer { get; init; }
        public uint ShowMeshlets { get; init; }

        public uint MeshBufferIndex { get; init; }
        public uint MeshInstanceBufferIndex { get; init; }
        public uint MeshletCount { get; init; }
        public uint MeshInstanceIndex { get; init; }

        public uint ThreadGroupCount { get; init; }
        public uint Reserved1 { get; init; }
        public uint Reserved2 { get; init; }
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer : SystemManager
    {
        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private readonly Shader computeRenderCommandsShader;
        private readonly Shader computeGenerateDepthPyramid;
        private readonly Shader renderMeshInstanceShader;

        private Texture depthBuffer;
        private Texture depthPyramidBuffer;

        private readonly GraphicsBuffer cpuMeshBuffer;
        private readonly GraphicsBuffer meshBuffer;

        private readonly GraphicsBuffer cpuMeshInstanceBuffer;
        private readonly GraphicsBuffer meshInstanceBuffer;

        private readonly GraphicsBuffer meshInstanceVisibilityBuffer;
        private readonly GraphicsBuffer indirectCommandBuffer;

        private readonly GraphicsBuffer cpuCamerasBuffer;
        private readonly GraphicsBuffer camerasBuffer;

        private readonly GraphicsBuffer cpuLightsBuffer;
        private readonly GraphicsBuffer lightsBuffer;

        private readonly GraphicsBuffer cpuReadBackCounters; 
        private readonly GraphicsBuffer cpuPipelineStatistics;
        private readonly QueryBuffer pipelineStatistics;

        private bool isFirstRun = true;
        private uint currentMeshInstanceCount;

        // private GraphicsPipeline graphicsPipeline;

        public GraphicsSceneRenderer(RenderManager renderManager, GraphicsManager graphicsManager, GraphicsSceneQueue sceneQueue, ResourcesManager resourcesManager, Texture mainRenderTarget)
        {
            if (mainRenderTarget is null)
            {
                throw new ArgumentNullException(nameof(mainRenderTarget));
            }

            this.graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            this.renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));

            this.debugRenderer = new DebugRenderer(graphicsManager, renderManager, resourcesManager);
            this.sceneQueue = sceneQueue;

            this.computeRenderCommandsShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeRenderCommands.shader");
            this.computeGenerateDepthPyramid = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeGenerateDepthPyramid.shader");
            this.renderMeshInstanceShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstance.shader");

            this.depthBuffer = CreateDepthBuffer(mainRenderTarget.Width, mainRenderTarget.Height);
            this.depthPyramidBuffer = CreateDepthPyramidBuffer(mainRenderTarget.Width, mainRenderTarget.Height);

            this.cpuMeshBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMesh>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "CpuMeshBuffer");
            this.meshBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMesh>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "MeshBuffer");

            this.cpuMeshInstanceBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMeshInstance>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 100000, isStatic: false, label: "CpuMeshInstanceBuffer");
            this.meshInstanceBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMeshInstance>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 100000, isStatic: false, label: "MeshInstanceBuffer");

            // TODO: Be careful that the index of the visibility buffer may not be in sync with the mesh instances
            this.meshInstanceVisibilityBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Gpu, GraphicsBufferUsage.WriteableStorage, 100000, isStatic: true, label: "MeshInstanceVisibilityBuffer");

            this.indirectCommandBuffer = this.graphicsManager.CreateGraphicsBuffer<DispatchMeshIndirectParam>(GraphicsHeapType.Gpu, GraphicsBufferUsage.IndirectCommands, 100000, isStatic: false, label: "IndirectCommandBuffer");

            this.cpuCamerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeCameras");
            this.camerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeCameras");

            this.cpuLightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeLights");
            this.lightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeLights");

            this.cpuReadBackCounters = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.ReadBack, GraphicsBufferUsage.Storage, 10, isStatic: false, "CpuReadbackCounters");
            this.cpuPipelineStatistics = this.graphicsManager.CreateGraphicsBuffer<ulong>(GraphicsHeapType.ReadBack, GraphicsBufferUsage.Storage, 2 * 14, isStatic: false, "CpuPipelineStatistics");
            this.pipelineStatistics = this.graphicsManager.CreateQueryBuffer(QueryBufferType.GraphicsPipelineStats, 2, "PipelineStatistics");
        }

        private Texture CreateDepthBuffer(int width, int height)
        {
            return this.graphicsManager.CreateTexture(GraphicsHeapType.Gpu, TextureFormat.Depth32Float, TextureUsage.RenderTarget, width, height, 1, 1, 1, isStatic: true, "DepthBuffer");
        }

        private Texture CreateDepthPyramidBuffer(int width, int height)
        {
            var depthPyramidWidth = MathUtils.FindLowerPowerOf2((uint)width);
            var depthPyramidHeight = MathUtils.FindLowerPowerOf2((uint)height);

            var depthPyramidMipLevels = MathUtils.ComputeTextureMipLevels(depthPyramidWidth, depthPyramidHeight);
            return this.graphicsManager.CreateTexture(GraphicsHeapType.Gpu, TextureFormat.R32Float, TextureUsage.ShaderWrite, (int)depthPyramidWidth, (int)depthPyramidHeight, 1, (int)depthPyramidMipLevels, 1, isStatic: true, "DepthPyramid");
        }

        public override void PreUpdate(CoreEngineContext context)
        {
            for (var i = 0; i < currentCpuGraphicsBuffers.Count; i++)
            {
                this.currentCpuGraphicsBuffers[i].Dispose();
            }

            this.currentCpuGraphicsBuffers.Clear();

            meshMapping.Clear();

            // TODO: Move that to render pipeline
            this.debugRenderer.ClearDebugLines();

            this.meshGraphicsResourcesToLoad.Clear();
        }

        public Fence Render(Texture mainRenderTargetTexture)
        {
            if (mainRenderTargetTexture is null)
            {
                throw new ArgumentNullException(nameof(mainRenderTargetTexture));
            }

            if (this.depthBuffer.Width != mainRenderTargetTexture.Width || this.depthBuffer.Height != mainRenderTargetTexture.Height)
            {
                this.depthBuffer.Dispose();
                this.depthBuffer = CreateDepthBuffer(mainRenderTargetTexture.Width, mainRenderTargetTexture.Height);

                this.depthPyramidBuffer.Dispose();
                this.depthPyramidBuffer = CreateDepthPyramidBuffer(mainRenderTargetTexture.Width, mainRenderTargetTexture.Height);
            }

            var scene = this.sceneQueue.WaitForNextScene();
            this.renderManager.OcclusionEnabled = scene.IsOcclusionCullingEnabled == 1u;

            // TODO: Find a way to do that also on Vulkan
            var pipelineStatsData = this.graphicsManager.CopyDataFromGraphicsBuffer<ulong>(this.cpuPipelineStatistics);
            this.renderManager.MeshletCount = (int)pipelineStatsData[11] + (int)pipelineStatsData[25];
            this.renderManager.CulledMeshletCount = ((int)pipelineStatsData[12] + (int)pipelineStatsData[26]) / 32;
            this.renderManager.CulledTriangleCount = pipelineStatsData[13] + pipelineStatsData[27];

            var counters = this.graphicsManager.CopyDataFromGraphicsBuffer<uint>(this.cpuReadBackCounters);
            this.renderManager.CulledMeshInstanceCount = (int)counters[0] + (int)counters[1];

            return RunRenderPipeline(scene, mainRenderTargetTexture);
        }

        private CommandList CreateCopyGpuDataCommandList(GraphicsScene scene)
        {
            if (renderManager.logFrameTime)
            {
                Logger.BeginAction("CreateCopyCommandList");
            }

            var commandListName = "CopySceneDataToGpu";
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, commandListName);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            if (this.isFirstRun)
            {
                using var cpuMeshInstanceVisibilityBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 100000, isStatic: true, label: "CpuMeshInstanceVisibilityBuffer");
                var visibilityBufferArray = new uint[100000];
                Array.Fill<uint>(visibilityBufferArray, 0);
                this.graphicsManager.CopyDataToGraphicsBuffer<uint>(cpuMeshInstanceVisibilityBuffer, 0, visibilityBufferArray);
                this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.meshInstanceVisibilityBuffer, cpuMeshInstanceVisibilityBuffer, 100000);
                this.isFirstRun = false;
            }

            ProcessGeometry(copyCommandList);
            ProcessCamera(copyCommandList, scene);
            ProcessLights(copyCommandList, scene);

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
            this.graphicsManager.CommitCommandList(copyCommandList);

            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.CopyTimestamp, startQueryIndex, endQueryIndex);

            if (renderManager.logFrameTime)
            {
                Logger.EndAction();
            }

            return copyCommandList;
        }

        // TODO: Do we create separate buffers for each mesh or do we
        // sub-allocate big shared buffers?
        struct MeshGraphicsResources
        {
            public string Path { get; init; }
            public GraphicsBuffer VerticesBuffer { get; init; }
            public ulong VerticesOffset { get; init; }
            public ulong VerticesSizeInBytes { get; init; }
            public GraphicsBuffer VertexIndicesBuffer { get; init; }
            public ulong VertexIndicesOffset { get; init; }
            public ulong VertexIndicesSizeInBytes { get; init; }
            public GraphicsBuffer TriangleIndicesBuffer { get; init; }
            public ulong TriangleIndicesOffset { get; init; }
            public ulong TriangleIndicesSizeInBytes { get; init; }
            public GraphicsBuffer MeshletsBuffer { get; init; }
            public ulong MeshletsOffset { get; init; }
            public ulong MeshletsSizeInBytes { get; init; }
        }

        // TODO: To remove
        private Dictionary<uint, uint> meshMapping = new Dictionary<uint, uint>();
        private Dictionary<uint, MeshGraphicsResources> meshGraphicsResources = new Dictionary<uint, MeshGraphicsResources>();
        private List<MeshGraphicsResources> meshGraphicsResourcesToLoad = new List<MeshGraphicsResources>();
        private List<GraphicsBuffer> currentCpuGraphicsBuffers = new List<GraphicsBuffer>();
        private uint currentMeshInstanceIndex;
        private uint currentMeshIndex;

        private MeshGraphicsResources GetGraphicsMeshResources(Mesh mesh)
        {
            var localMeshGraphicsResources = new MeshGraphicsResources()
            {
                Path = mesh.FullPath,
                VerticesBuffer = CreateMeshGraphicsBuffer(mesh.VerticesSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}Vertices"),
                VerticesOffset = mesh.VerticesOffset,
                VerticesSizeInBytes = mesh.VerticesSizeInBytes,
                VertexIndicesBuffer = CreateMeshGraphicsBuffer(mesh.VertexIndicesSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexIndices"),
                VertexIndicesOffset = mesh.VertexIndicesOffset,
                VertexIndicesSizeInBytes = mesh.VertexIndicesSizeInBytes,
                TriangleIndicesBuffer = CreateMeshGraphicsBuffer(mesh.TriangleIndicesSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}TriangleIndices"),
                TriangleIndicesOffset = mesh.TriangleIndicesOffset,
                TriangleIndicesSizeInBytes = mesh.TriangleIndicesSizeInBytes,
                MeshletsBuffer = CreateMeshGraphicsBuffer(mesh.MeshletsSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}Meshlets"),
                MeshletsOffset = mesh.MeshletsOffset,
                MeshletsSizeInBytes = mesh.MeshletsSizeInBytes
            };

            return localMeshGraphicsResources;
        }

        private GraphicsBuffer CreateMeshGraphicsBuffer(ulong sizeInBytes, string label)
        {
            var graphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, (int)sizeInBytes, isStatic: true, label);
            return graphicsBuffer;
        }

        private void LoadMeshGraphicsBuffers(CommandList commandList, MeshGraphicsResources meshGraphicsResources)
        {
            Logger.BeginAction($"Loading graphics buffers for Mesh '{Path.GetFileName(meshGraphicsResources.Path)}'");
            LoadMeshGraphicsBuffer(commandList, meshGraphicsResources.Path, meshGraphicsResources.VerticesBuffer, meshGraphicsResources.VerticesOffset, meshGraphicsResources.VerticesSizeInBytes);
            LoadMeshGraphicsBuffer(commandList, meshGraphicsResources.Path, meshGraphicsResources.VertexIndicesBuffer, meshGraphicsResources.VertexIndicesOffset, meshGraphicsResources.VertexIndicesSizeInBytes);
            LoadMeshGraphicsBuffer(commandList, meshGraphicsResources.Path, meshGraphicsResources.TriangleIndicesBuffer, meshGraphicsResources.TriangleIndicesOffset, meshGraphicsResources.TriangleIndicesSizeInBytes);
            LoadMeshGraphicsBuffer(commandList, meshGraphicsResources.Path, meshGraphicsResources.MeshletsBuffer, meshGraphicsResources.MeshletsOffset, meshGraphicsResources.MeshletsSizeInBytes);
            Logger.EndAction();
        }

        private void LoadMeshGraphicsBuffer(CommandList commandList, string path, GraphicsBuffer graphicsBuffer, ulong offset, ulong sizeInBytes)
        {
            using var fileStream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(fileStream);

            fileStream.Position = (long)offset;
            var bufferData = reader.ReadBytes((int)sizeInBytes);

            // TODO: Use transient buffers
            var cpuGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, (int)sizeInBytes, isStatic: true, "TransientMeshBufferCPU");
            this.currentCpuGraphicsBuffers.Add(cpuGraphicsBuffer);

            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(cpuGraphicsBuffer, 0, bufferData);
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(commandList, graphicsBuffer, cpuGraphicsBuffer, (uint)sizeInBytes);
        }

        private readonly ShaderMesh[] meshList = new ShaderMesh[10000];
        private readonly ShaderMeshInstance[] meshInstanceList = new ShaderMeshInstance[100000];

        public void RenderMesh(uint meshInstanceId)
        {
            var meshInstance = meshInstanceList[meshInstanceId];

            //var worldBoundingBox = BoundingBox.CreateTransformed(meshList[meshInstance.MeshIndex].BoundingBox, meshInstance.WorldMatrix);
            //this.debugRenderer.DrawBoundingBox(meshInstance.WorldBoundingBox, new Vector3(0, 0.5f, 1.0f));
            // TODO: Mark the mesh active buffer
        }

        public void RenderMesh(uint meshInstanceId, Matrix4x4 worldMatrix, float scale)
        {
            var meshInstance = meshInstanceList[(int)meshInstanceId];

            // TODO: Compute the inverse matrix in the ComputeWorldMatryxSystem
            Matrix4x4.Invert(worldMatrix, out var inverseMatrix);
            inverseMatrix = Matrix4x4.Transpose(inverseMatrix);

            var shaderMeshInstance = new ShaderMeshInstance()
            {
                MeshIndex = meshInstance.MeshIndex,
                Scale = scale, // TODO: How to support uniform scale with bounding sphere tests
                WorldMatrix = worldMatrix,
                WorldInvTransposeMatrix = inverseMatrix,
            };

            meshInstanceList[meshInstanceId] = shaderMeshInstance;

            //this.debugRenderer.DrawBoundingBox(meshInstance.WorldBoundingBox, new Vector3(0, 0.5f, 1.0f));
        }

        public uint RenderMesh(Mesh mesh, Matrix4x4 worldMatrix, float scale)
        {
            if (mesh is null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            // TODO: Compute the inverse matrix in the ComputeWorldMatryxSystem
            Matrix4x4.Invert(worldMatrix, out var inverseMatrix);
            inverseMatrix = Matrix4x4.Transpose(inverseMatrix);

            //this.debugRenderer.DrawBoundingBox(worldBoundingBox, new Vector3(0, 0.5f, 1.0f));

            if (!this.meshGraphicsResources.ContainsKey(mesh.ResourceId))
            {
                var localMeshGraphicsResources = GetGraphicsMeshResources(mesh);
                this.meshGraphicsResources.Add(mesh.ResourceId, localMeshGraphicsResources);
                this.meshGraphicsResourcesToLoad.Add(localMeshGraphicsResources);
            }

            if (!meshMapping.ContainsKey(mesh.ResourceId))
            {
                var meshGraphicsResources = this.meshGraphicsResources[mesh.ResourceId];

                var shaderMesh = new ShaderMesh()
                {
                    MeshletCount = mesh.MeshletCount,
                    VerticesBufferIndex = meshGraphicsResources.VerticesBuffer.ShaderResourceIndex,
                    VertexIndicesBufferIndex = meshGraphicsResources.VertexIndicesBuffer.ShaderResourceIndex,
                    TriangleIndicesBufferIndex = meshGraphicsResources.TriangleIndicesBuffer.ShaderResourceIndex,
                    MeshletBufferIndex = meshGraphicsResources.MeshletsBuffer.ShaderResourceIndex,
                    BoundingBox = mesh.BoundingBox
                };

                meshMapping.Add(mesh.ResourceId, currentMeshIndex);
                meshList[(int)currentMeshIndex++] = shaderMesh;
            }

            var meshIndex = meshMapping[mesh.ResourceId];
            this.renderManager.TriangleCount += mesh.TriangleCount;

            var shaderMeshInstance = new ShaderMeshInstance()
            {
                MeshIndex = meshIndex,
                Scale = scale,
                WorldMatrix = worldMatrix,
                WorldInvTransposeMatrix = inverseMatrix
            };

            meshInstanceList[(int)currentMeshInstanceIndex++] = shaderMeshInstance;
            return currentMeshInstanceIndex - 1;
        }

        private void ProcessGeometry(CommandList copyCommandList)
        {
            for (var i = 0; i < this.meshGraphicsResourcesToLoad.Count; i++)
            {
                LoadMeshGraphicsBuffers(copyCommandList, this.meshGraphicsResourcesToLoad[i]);
            }

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMesh>(this.cpuMeshBuffer, 0, meshList.AsSpan().Slice(0, (int)currentMeshIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMesh>(copyCommandList, this.meshBuffer, this.cpuMeshBuffer, currentMeshIndex);

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMeshInstance>(this.cpuMeshInstanceBuffer, 0, meshInstanceList.AsSpan().Slice(0, (int)currentMeshInstanceIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMeshInstance>(copyCommandList, this.meshInstanceBuffer, this.cpuMeshInstanceBuffer, currentMeshInstanceIndex);

            this.renderManager.MeshCount = (int)currentMeshIndex;
            this.renderManager.MeshInstanceCount = (int)currentMeshInstanceIndex;

            this.currentMeshInstanceCount = currentMeshInstanceIndex;
        }

        private void ProcessCamera(CommandList copyCommandList, GraphicsScene scene)
        {
            var currentCameraIndex = 0u;
            ShaderCamera shaderCamera;

            if (scene.DebugCamera != null)
            {
                shaderCamera = new ShaderCamera()
                {
                    WorldPosition = scene.ActiveCamera.WorldPosition,
                    ViewMatrix = scene.DebugCamera.ViewMatrix,
                    ViewProjectionMatrix = scene.DebugCamera.ViewProjectionMatrix,
                    BoundingFrustum = new ShaderBoundingFrustum(scene.ActiveCamera.BoundingFrustum)
                };

                this.debugRenderer.DrawBoundingFrustum(scene.ActiveCamera.BoundingFrustum, new Vector3(1, 0, 0.5f));
            }

            else
            {
                shaderCamera = new ShaderCamera()
                {
                    WorldPosition = scene.ActiveCamera.WorldPosition,
                    ViewMatrix = scene.ActiveCamera.ViewMatrix,
                    ViewProjectionMatrix = scene.ActiveCamera.ViewProjectionMatrix,
                    BoundingFrustum = new ShaderBoundingFrustum(scene.ActiveCamera.BoundingFrustum)
                };
            }

            var cameraList = ArrayPool<ShaderCamera>.Shared.Rent(1);
            cameraList[currentCameraIndex++] = shaderCamera;

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderCamera>(this.cpuCamerasBuffer, 0, cameraList.AsSpan().Slice(0, (int)currentCameraIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cpuCamerasBuffer, currentCameraIndex);

            ArrayPool<ShaderCamera>.Shared.Return(cameraList);
        }

        private void ProcessLights(CommandList copyCommandList, GraphicsScene scene)
        {
            if (scene.Lights.Count == 0)
            {
                return;
            }

            var currentLightIndex = 0u;
            var lightList = ArrayPool<ShaderLight>.Shared.Rent(10000);

            for (var i = 0; i < scene.Lights.Count; i++)
            {
                var light = scene.Lights[i];

                var shaderLight = new ShaderLight
                {
                    WorldSpacePosition = (light.LightType == LightType.Directional) ? Vector3.Normalize(light.WorldPosition) : light.WorldPosition,
                    Color = light.Color,
                    LightType = (int)light.LightType
                };

                lightList[currentLightIndex++] = shaderLight;

                if (light.LightType == LightType.Point)
                {
                    this.debugRenderer.DrawSphere(light.WorldPosition, 2.0f, new Vector3(1, 0.2f, 0));
                }
            }

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderLight>(this.cpuLightsBuffer, 0, lightList.AsSpan().Slice(0, (int)currentLightIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.cpuLightsBuffer, currentLightIndex);

            this.renderManager.LightsCount = (int)currentLightIndex;
            ArrayPool<ShaderLight>.Shared.Return(lightList);
        }

        private CommandList CreateComputeRenderCommandList(GraphicsScene scene, Texture? depthPyramidBuffer = null)
        {
            var isPostPass = depthPyramidBuffer != null;

            var commandListName = isPostPass ? "PostComputeRenderCommands" : "ComputeRenderCommands";
            var indirectCommandBuffer = this.indirectCommandBuffer;

            var computeRenderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.ComputeCommandQueue, commandListName);
            this.graphicsManager.ResetIndirectCommandBuffer(computeRenderCommandList, indirectCommandBuffer);
            
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(computeRenderCommandList);
            this.graphicsManager.SetShader(computeRenderCommandList, this.computeRenderCommandsShader);

            this.graphicsManager.SetShaderParameterValues(computeRenderCommandList, 0, new uint[]
            {
                this.camerasBuffer.ShaderResourceIndex,
                scene.ShowMeshlets,
                this.meshBuffer.ShaderResourceIndex,
                this.meshInstanceBuffer.ShaderResourceIndex,
                this.meshInstanceVisibilityBuffer.ShaderResourceIndex,
                indirectCommandBuffer.ShaderResourceIndex,
                this.currentMeshInstanceCount,
                isPostPass ? 1u : 0u,
                scene.IsOcclusionCullingEnabled,
                isPostPass ? depthPyramidBuffer!.ShaderResourceIndex : 0u,
                isPostPass ? (uint)depthPyramidBuffer!.Width : 0u,
                isPostPass ? (uint)depthPyramidBuffer!.Height : 0u
            });

            if (this.currentMeshInstanceCount > 0)
            {
                // TODO: Don't hardcode wave size
                var waveSize = 32u;
                this.graphicsManager.DispatchCompute(computeRenderCommandList, MathUtils.ComputeGroupThreads(this.currentMeshInstanceCount, waveSize), 1, 1);
            }

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(computeRenderCommandList);

            this.graphicsManager.SetGraphicsBufferBarrier(computeRenderCommandList, indirectCommandBuffer);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(computeRenderCommandList, this.cpuReadBackCounters, indirectCommandBuffer, 1, isPostPass ? 1u * sizeof(uint) : 0u, indirectCommandBuffer.SizeInBytes - sizeof(uint)); 
            this.graphicsManager.CommitCommandList(computeRenderCommandList);

            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return computeRenderCommandList;
        }

        private CommandList CreateRenderGeometryCommandList(Texture mainRenderTargetTexture, Texture depthBuffer, Texture? depthPyramidBuffer = null)
        {
            var isPostPass = depthPyramidBuffer != null;

            var commandListName = isPostPass ? "PostRenderGeometry" : "RenderGeometry";
            var indirectCommandBuffer = this.indirectCommandBuffer;

            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, commandListName);

            var renderTarget = new RenderTargetDescriptor(mainRenderTargetTexture, isPostPass ? null : Vector4.Zero, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthBuffer, isPostPass ? DepthBufferOperation.Write : DepthBufferOperation.ClearWrite, backfaceCulling: true, PrimitiveType.Triangle);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor, this.renderMeshInstanceShader);
            this.graphicsManager.ResetQueryBuffer(this.pipelineStatistics);
            this.graphicsManager.BeginQuery(renderCommandList, this.pipelineStatistics, isPostPass ? 1 : 0);

            if (this.currentMeshInstanceCount > 0)
            {
                this.graphicsManager.ExecuteIndirect(renderCommandList, this.currentMeshInstanceCount, indirectCommandBuffer, 0);
            }

            this.graphicsManager.EndQuery(renderCommandList, this.pipelineStatistics, isPostPass ? 1 : 0);
            this.graphicsManager.EndRenderPass(renderCommandList);
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            if (isPostPass)
            {
                this.graphicsManager.ResolveQueryData(renderCommandList, this.pipelineStatistics, this.cpuPipelineStatistics, 0..2);
            }

            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return renderCommandList;
        }
        
        private CommandList CreateDepthPyramidCommandList(Texture depthPyramidBuffer, Texture depthBuffer)
        {
            var commandList = this.graphicsManager.CreateCommandList(this.renderManager.ComputeCommandQueue, "GenerateDepthPyramid");
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(commandList);
            this.graphicsManager.SetShader(commandList, this.computeGenerateDepthPyramid);

            var currentWidth = depthPyramidBuffer.Width;
            var currentHeight = depthPyramidBuffer.Height;

            for (var i = 0; i < depthPyramidBuffer.MipLevels; i++)
            {
                if (i > 0)
                {
                    this.graphicsManager.SetTextureBarrier(commandList, depthPyramidBuffer);
                }

                this.graphicsManager.SetShaderParameterValues(commandList, 0, new uint[]
                {
                    i == 0 ? depthBuffer.ShaderResourceIndex : depthPyramidBuffer.GetShaderResourceIndex((uint)i - 1),
                    depthPyramidBuffer.GetWriteableShaderResourceIndex((uint)i),
                    (uint)currentWidth,
                    (uint)currentHeight
                });

                this.graphicsManager.DispatchCompute(commandList, MathUtils.ComputeGroupThreads((uint)currentWidth, 8), MathUtils.ComputeGroupThreads((uint)currentHeight, 8), 1);  

                currentWidth = (int)MathF.Max(1, currentWidth >> 1);
                currentHeight = (int)MathF.Max(1, currentHeight >> 1);
            }

            this.graphicsManager.SetTextureBarrier(commandList, depthPyramidBuffer);
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(commandList);
            this.graphicsManager.CommitCommandList(commandList);

            this.renderManager.AddGpuTiming("GenerateDepthPyramid", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return commandList;
        }

        private Fence RunRenderPipeline(GraphicsScene scene, Texture mainRenderTargetTexture)
        {
            if (mainRenderTargetTexture is null)
            {
                throw new ArgumentNullException(nameof(mainRenderTargetTexture));
            }

            var copyCommandList = CreateCopyGpuDataCommandList(scene);
            
            var computeRenderCommandList = CreateComputeRenderCommandList(scene);
            var renderGeometryCommandList = CreateRenderGeometryCommandList(mainRenderTargetTexture, depthBuffer);

            var depthPyramidCommandList = CreateDepthPyramidCommandList(depthPyramidBuffer, depthBuffer);

            var postComputeRenderCommandList = CreateComputeRenderCommandList(scene, depthPyramidBuffer);
            var postRenderGeometryCommandList = CreateRenderGeometryCommandList(mainRenderTargetTexture, depthBuffer, depthPyramidBuffer);

            // TODO: Try to execute all the command lists except copy to the render command queue because the command lists are chained 
            // and cannot run in parallel
            var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList });

            var computeFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.ComputeCommandQueue, new CommandList[] { computeRenderCommandList }, new Fence[] { copyFence });
            var renderFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { renderGeometryCommandList }, new Fence[] { computeFence });

            var postComputeFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.ComputeCommandQueue, new CommandList[] { depthPyramidCommandList, postComputeRenderCommandList }, new Fence[] { renderFence });
            var postRenderFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { postRenderGeometryCommandList }, new Fence[] { postComputeFence });
            //var postRenderFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { computeRenderCommandList, renderGeometryCommandList, depthPyramidCommandList, postComputeRenderCommandList, postRenderGeometryCommandList }, new Fence[] { copyFence });

            // TODO: Submit render and debug command list at the same time
            this.debugRenderer.Render(this.camerasBuffer, mainRenderTargetTexture, depthBuffer);

            /*
            for (var i = 0; i < this.currentMeshInstanceCount; i++)
            {
                var projectionResult = CreateOptimizedTransformed2D(scene.MeshInstances[i].WorldBoundingBox, scene.Cameras[0].ViewMatrix * scene.Cameras[0].ProjectionMatrix, out var projectedBoundingBox);

                if (projectionResult)
                {
                    var renderTargetSize = new Vector2(mainRenderTargetTexture.Width, mainRenderTargetTexture.Height);

                    var hashResult = HashValue((uint)i);
                    var meshletColor = new Vector3((float)(hashResult & 255), (float)((hashResult >> 8) & 255), (float)((hashResult >> 16) & 255)) / 255.0f;
                    this.renderManager.Graphics2DRenderer.DrawRectangleSurface(projectedBoundingBox.MinPoint * renderTargetSize, projectedBoundingBox.MaxPoint * renderTargetSize, new Vector4(meshletColor, 0.75f));
                }
            }*/

            var surfaceWidth = 512;
            var surfaceHeight = 512;
            var surfacePositionX = mainRenderTargetTexture.Width - surfaceWidth - 10;
            var surfacePositionY = 10;

            this.renderManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(surfacePositionX, surfacePositionY), new Vector2(surfacePositionX + surfaceWidth, surfacePositionY + surfaceHeight), depthPyramidBuffer, isOpaque: true);

            return postRenderFence;
        }
        
        private static uint HashValue(uint a)
        {
            a = (a+0x7ed55d16) + (a<<12);
            a = (a^0xc761c23c) ^ (a>>19);
            a = (a+0x165667b1) + (a<<5);
            a = (a+0xd3a2646c) ^ (a<<9);
            a = (a+0xfd7046c5) + (a<<3);
            a = (a^0xb55a4f09) ^ (a>>16);

            return a;
        }

        private static readonly Vector3[] BoundingBoxOffsets = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1)
        };

        private static bool CreateOptimizedTransformed2D(in BoundingBox boundingBox, Matrix4x4 worldViewProjMatrix, out BoundingBox2D result)
        {
            var offsetMatrix = new Matrix4x4(0.5f, 0.0f, 0.0f, 0.0f,
                                             0.0f, -0.5f, 0.0f, 0.0f,
                                             0.0f, 0.0f, 1.0f, 0.0f,
                                             0.5f, 0.5f, 0.0f, 1.0f);

            var matrix = worldViewProjMatrix * offsetMatrix;
            var boundingBoxSize = boundingBox.MaxPoint - boundingBox.MinPoint;

            var minPoint = new Vector2(float.MaxValue, float.MaxValue);
            var maxPoint = new Vector2(float.MinValue, float.MinValue);

            var depth = 0.0f;

            for (var i = 0; i < 8; i++)
            {
                var sourcePoint = boundingBox.MinPoint + BoundingBoxOffsets[i] * boundingBoxSize;

                var point = Vector4.Transform(new Vector4(sourcePoint, 1.0f), matrix);
                point /= point.W;

                if (point.Z <= 0.0f)
                {
                    result = new BoundingBox2D();
                    return false;
                }

                minPoint = (point.Z > 0.0f) ? Vector2.Min(new Vector2(point.X, point.Y), minPoint) : minPoint;
                maxPoint = (point.Z > 0.0f) ? Vector2.Max(new Vector2(point.X, point.Y), maxPoint) : maxPoint;

                depth = MathF.Max(point.Z, depth);
            }

            result = new BoundingBox2D(minPoint, maxPoint);
            return true;
        }
    }
}

// TODO: OLD CODE
    
            // var graphicsPipelineResourceDeclarations = new GraphicsPipelineResourceDeclaration[]
            // {
            //     new GraphicsPipelineTextureResourceDeclaration("MainCameraDepthBuffer", TextureFormat.Depth32Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
            //     new GraphicsPipelineTextureResourceDeclaration("OpaqueHdrRenderTarget", TextureFormat.Rgba16Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
            //     new GraphicsPipelineTextureResourceDeclaration("TransparentHdrRenderTarget", TextureFormat.Rgba16Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
            //     new GraphicsPipelineTextureResourceDeclaration("TransparentRevealageRenderTarget", TextureFormat.R16Float, TextureUsage.RenderTarget, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
            //     new GraphicsPipelineTextureResourceDeclaration("ResolveRenderTarget", TextureFormat.Rgba16Float, TextureUsage.ShaderWrite, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")),
            //     new GraphicsPipelineTextureResourceDeclaration("ToneMapRenderTarget", TextureFormat.Rgba16Float, TextureUsage.ShaderWrite, new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"), new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height"))
            // };

            // var graphicsPipelineSteps = Array.Empty<GraphicsPipelineStep>();

            // var graphicsPipelineSteps = new GraphicsPipelineStep[]
            // {
            // new ExecutePipelineStep("GenerateDepthBuffer",
            //                         this.depthGraphicsPipeline,
            //                         new GraphicsPipelineParameter[]
            //                         {
            //                             new BindingGraphicsPipelineParameter<IGraphicsResource>("MainCameraDepthBuffer", new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer")),
            //                             new BindingGraphicsPipelineParameter<IGraphicsResource>("MainCameraDepthIndirectCommandBuffer", new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthIndirectCommandBuffer")),
            //                             new BindingGraphicsPipelineParameter<IGraphicsResource>("MainCameraTransparentDepthIndirectCommandBuffer", new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraTransparentDepthIndirectCommandBuffer")),
            //                             new BindingGraphicsPipelineParameter<int>("GeometryInstanceCount", new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
            //                         }),
            // new ComputeMinMaxPipelineStep("ComputeMinMaxDepth",
            //                                 resourcesManager,
            //                                 new GraphicsPipelineResourceBinding[]
            //                                 {
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<int>(0)),
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MinMaxDepthComputeBuffer"), new ConstantPipelineParameterBinding<int>(2))
            //                                 },
            //                                 new GraphicsPipelineResourceBinding[]
            //                                 {
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("CamerasBuffer"), new ConstantPipelineParameterBinding<int>(1))
            //                                 }),
            // new RenderIndirectCommandBufferPipelineStep("RenderOpaqueGeometry",
            //                                             "/System/Shaders/RenderMeshInstance.shader",
            //                                             new GraphicsPipelineResourceBinding[]
            //                                             {
            //                                                 new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.CompareEqual)),
            //                                                 new IndirectCommandBufferGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraIndirectCommandBuffer"), new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
            //                                             },
            //                                             new GraphicsPipelineResourceBinding[]
            //                                             {
            //                                                 new RenderTargetGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("OpaqueHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(0), new GraphicsPipelineParameterBinding<Vector4>("ClearColor"))
            //                                             }),
            // new RenderIndirectCommandBufferPipelineStep("RenderTransparentGeometry",
            //                                             "/System/Shaders/RenderMeshInstanceTransparent.shader",
            //                                             new GraphicsPipelineResourceBinding[]
            //                                             {
            //                                                 new DepthGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraDepthBuffer"), new ConstantPipelineParameterBinding<DepthBufferOperation>(DepthBufferOperation.CompareGreater)),
            //                                                 new IndirectCommandBufferGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("MainCameraTransparentIndirectCommandBuffer"), new GraphicsPipelineParameterBinding<int>("GeometryInstanceCount"))
            //                                             },
            //                                             new GraphicsPipelineResourceBinding[]
            //                                             {
            //                                                 new RenderTargetGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(0), new ConstantPipelineParameterBinding<Vector4>(new Vector4(0, 0, 0, 0)), new ConstantPipelineParameterBinding<BlendOperation>(BlendOperation.AddOneOne)),
            //                                                 new RenderTargetGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentRevealageRenderTarget"), new ConstantPipelineParameterBinding<int>(1), new ConstantPipelineParameterBinding<Vector4>(new Vector4(1, 0, 0, 0)), new ConstantPipelineParameterBinding<BlendOperation>(BlendOperation.AddOneMinusSourceColor))
            //                                             },
            //                                             backfaceCulling: false),
            // new ComputeGraphicsPipelineStep("Resolve", 
            //                                 "/System/Shaders/ResolveCompute.shader@Resolve",
            //                                 new GraphicsPipelineResourceBinding[]
            //                                 {
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("OpaqueHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(0)),
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentHdrRenderTarget"), new ConstantPipelineParameterBinding<int>(1)),
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("TransparentRevealageRenderTarget"), new ConstantPipelineParameterBinding<int>(2))
            //                                 }, 
            //                                 new GraphicsPipelineResourceBinding[]
            //                                 {
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("ResolveRenderTarget"), new ConstantPipelineParameterBinding<int>(3))
            //                                 }, 
            //                                 new GraphicsPipelineParameterBinding<int>[]
            //                                 {  
            //                                     new GraphicsPipelineParameterBinding<int>("ResolveRenderTarget", "Width"),
            //                                     new GraphicsPipelineParameterBinding<int>("ResolveRenderTarget", "Height")
            //                                 }),
            // new ComputeGraphicsPipelineStep("ToneMap", 
            //                                 "/System/Shaders/ToneMapCompute.shader@ToneMap",
            //                                 new GraphicsPipelineResourceBinding[]
            //                                 {
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("ResolveRenderTarget"), new ConstantPipelineParameterBinding<int>(0))
            //                                 }, 
            //                                 new GraphicsPipelineResourceBinding[]
            //                                 {
            //                                     new ShaderGraphicsPipelineResourceBinding(new GraphicsPipelineParameterBinding<IGraphicsResource>("ToneMapRenderTarget"), new ConstantPipelineParameterBinding<int>(1))
            //                                 }, 
            //                                 new GraphicsPipelineParameterBinding<int>[]
            //                                 {  
            //                                     new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Width"),
            //                                     new GraphicsPipelineParameterBinding<int>("MainRenderTarget", "Height")
            //                                 })
            // };

            // this.graphicsPipeline = new GraphicsPipeline(this.renderManager, this.graphicsManager, resourcesManager, graphicsPipelineResourceDeclarations, graphicsPipelineSteps);
// var graphicsPipelineParameters = new GraphicsPipelineParameter[]
            // {
            //     new ResourceGraphicsPipelineParameter("MainRenderTarget", mainRenderTargetTexture),
            //     new ResourceGraphicsPipelineParameter("MainCameraIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.OpaqueCommandListIndex]),
            //     new ResourceGraphicsPipelineParameter("MainCameraDepthIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.OpaqueDepthCommandListIndex]),
            //     new ResourceGraphicsPipelineParameter("MainCameraTransparentIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.TransparentCommandListIndex]),
            //     new ResourceGraphicsPipelineParameter("MainCameraTransparentDepthIndirectCommandBuffer", this.indirectCommandBufferList[mainCamera.TransparentDepthCommandListIndex]),
            //     new Vector4GraphicsPipelineParameter("ClearColor", new Vector4(65 * 50, 135 * 50, 255 * 50, 1.0f) / 255.0f),
            //     new IntGraphicsPipelineParameter("GeometryInstanceCount", this.currentGeometryInstanceIndex),
            //     new ResourceGraphicsPipelineParameter("MinMaxDepthComputeBuffer", this.minMaxDepthComputeBuffer),
            //     new ResourceGraphicsPipelineParameter("CamerasBuffer", this.camerasBuffer)
            // };

            // var pipelineFence = this.graphicsPipeline.Process(graphicsPipelineParameters, optimizeFence);
            // var toneMapRenderTarget = this.graphicsPipeline.ResolveResource("ToneMapRenderTarget") as Texture;

            // var transferCommandList = TransferTextureToRenderTarget(toneMapRenderTarget, mainRenderTargetTexture);

            // // TODO: Skip the wait if the last step of the pipeline was on the render queue
            // this.graphicsManager.WaitForCommandQueue(this.renderManager.RenderCommandQueue, pipelineFence);
            // this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { transferCommandList }, isAwaitable: false);
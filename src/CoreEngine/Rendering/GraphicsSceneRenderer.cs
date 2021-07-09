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
        public ShaderMatrix4x4 ViewMatrix { get; init; }
        public ShaderMatrix4x4 ViewProjectionMatrix { get; init; }
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
    }

    readonly struct ShaderMeshInstance
    {
        public uint MeshIndex { get; init; }
        public float Scale { get; init; }
        public ShaderMatrix4x3 WorldMatrix { get; init; }
        public ShaderMatrix3x3 WorldInvTransposeMatrix { get; init; }
        public BoundingBox WorldBoundingBox { get; init; }
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
    public class GraphicsSceneRenderer
    {
        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private readonly Shader computeRenderCommandsShader;
        private readonly Shader renderMeshInstanceShader;

        private readonly GraphicsBuffer cpuMeshBuffer;
        private readonly GraphicsBuffer meshBuffer;

        private readonly GraphicsBuffer cpuMeshInstanceBuffer;
        private readonly GraphicsBuffer meshInstanceBuffer;

        private readonly GraphicsBuffer indirectCommandBuffer;

        private readonly GraphicsBuffer cpuCamerasBuffer;
        private readonly GraphicsBuffer camerasBuffer;

        private readonly GraphicsBuffer cpuLightsBuffer;
        private readonly GraphicsBuffer lightsBuffer;

        private uint currentMeshInstanceCount;

        // private GraphicsPipeline graphicsPipeline;

        public GraphicsSceneRenderer(RenderManager renderManager, GraphicsManager graphicsManager, GraphicsSceneQueue sceneQueue, ResourcesManager resourcesManager)
        {
            this.graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            this.renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));

            this.debugRenderer = new DebugRenderer(graphicsManager, renderManager, resourcesManager);
            this.sceneQueue = sceneQueue;

            this.computeRenderCommandsShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeRenderCommands.shader");
            this.renderMeshInstanceShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstance.shader");

            this.cpuMeshBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMesh>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "CpuMeshBuffer");
            this.meshBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMesh>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "MeshBuffer");

            this.cpuMeshInstanceBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMeshInstance>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 100000, isStatic: false, label: "CpuMeshInstanceBuffer");
            this.meshInstanceBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderMeshInstance>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 100000, isStatic: false, label: "MeshInstanceBuffer");

            this.indirectCommandBuffer = this.graphicsManager.CreateGraphicsBuffer<DispatchMeshIndirectParam>(GraphicsHeapType.Gpu, GraphicsBufferUsage.IndirectCommands, 100000, isStatic: false, label: "IndirectCommandBuffer");

            this.cpuCamerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeCameras");
            this.camerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeCameras");

            this.cpuLightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeLights");
            this.lightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, 10000, isStatic: false, label: "ComputeLights");
        }

        public void Render(Texture mainRenderTargetTexture)
        {
            var scene = this.sceneQueue.WaitForNextScene();

            // TODO: Move that to render pipeline
            this.debugRenderer.ClearDebugLines();

            if (this.renderManager.logFrameTime)
            {
                Logger.BeginAction("RunPipeline");
            }

            RunRenderPipeline(scene, mainRenderTargetTexture);

            if (this.renderManager.logFrameTime)
            {
                Logger.EndAction();
            }
        }

        private CommandList CopyGpuData(GraphicsScene scene)
        {
            if (renderManager.logFrameTime)
            {
                Logger.BeginAction("CreateCopyCommandList");
            }

            var commandListName = "CopySceneDataToGpu";
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, commandListName);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            ProcessGeometry(copyCommandList, scene);
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
            public GraphicsBuffer VerticesBuffer { get; init; }
            public GraphicsBuffer VertexIndicesBuffer { get; init; }
            public GraphicsBuffer TriangleIndicesBuffer { get; init; }
            public GraphicsBuffer MeshletsBuffer { get; init; }
        }

        // TODO: To remove
        private Dictionary<uint, uint> meshMapping = new Dictionary<uint, uint>();
        private Dictionary<uint, MeshGraphicsResources> meshGraphicsResources = new Dictionary<uint, MeshGraphicsResources>();
        private List<GraphicsBuffer> currentCpuGraphicsBuffers = new List<GraphicsBuffer>();

        // TODO: Refactor that to allow the use of technologies like direct storage
        private GraphicsBuffer LoadGraphicsBuffer(CommandList copyCommandList, string path, ulong offset, ulong sizeInBytes, string label)
        {
            using var fileStream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(fileStream);

            fileStream.Position = (long)offset;
            var bufferData = reader.ReadBytes((int)sizeInBytes);

            // TODO: Use transient buffers
            var cpuGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Upload, GraphicsBufferUsage.Storage, (int)sizeInBytes, isStatic: true, label + "CPU");
            this.currentCpuGraphicsBuffers.Add(cpuGraphicsBuffer);

            var graphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<byte>(GraphicsHeapType.Gpu, GraphicsBufferUsage.Storage, (int)sizeInBytes, isStatic: true, label);
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(cpuGraphicsBuffer, 0, bufferData);
            this.graphicsManager.CopyDataToGraphicsBuffer<byte>(copyCommandList, graphicsBuffer, cpuGraphicsBuffer, (uint)sizeInBytes);

            return graphicsBuffer;
        }

        private void ProcessGeometry(CommandList copyCommandList, GraphicsScene scene)
        {
            if (scene.MeshInstances.Count == 0)
            {
                return;
            }

            for (var i = 0; i < currentCpuGraphicsBuffers.Count; i++)
            {
                this.currentCpuGraphicsBuffers[i].Dispose();
            }

            this.currentCpuGraphicsBuffers.Clear();

            uint currentMeshIndex = 0;
            uint currentMeshInstanceIndex = 0;

            var meshList = ArrayPool<ShaderMesh>.Shared.Rent(10000);
            var meshInstanceList = ArrayPool<ShaderMeshInstance>.Shared.Rent(100000);
            meshMapping.Clear();

            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];
                this.debugRenderer.DrawBoundingBox(meshInstance.WorldBoundingBox, new Vector3(0, 0.5f, 1.0f));

                var mesh = meshInstance.Mesh;

                if (!this.meshGraphicsResources.ContainsKey(mesh.ResourceId))
                {
                    var localMeshGraphicsResources = new MeshGraphicsResources()
                    {
                        VerticesBuffer = LoadGraphicsBuffer(copyCommandList, mesh.FullPath, mesh.VerticesOffset, mesh.VerticesSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}Vertices"),
                        VertexIndicesBuffer = LoadGraphicsBuffer(copyCommandList, mesh.FullPath, mesh.VertexIndicesOffset, mesh.VertexIndicesSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}VertexIndices"),
                        TriangleIndicesBuffer = LoadGraphicsBuffer(copyCommandList, mesh.FullPath, mesh.TriangleIndicesOffset, mesh.TriangleIndicesSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}TriangleIndices"),
                        MeshletsBuffer = LoadGraphicsBuffer(copyCommandList, mesh.FullPath, mesh.MeshletsOffset, mesh.MeshletsSizeInBytes, $"{Path.GetFileNameWithoutExtension(mesh.Path)}Meshlets")
                    };

                    this.renderManager.MeshletCount = (int)mesh.MeshletCount;
                    this.renderManager.TriangleCount = (int)mesh.TriangleCount;

                    this.meshGraphicsResources.Add(mesh.ResourceId, localMeshGraphicsResources);
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
                        MeshletBufferIndex = meshGraphicsResources.MeshletsBuffer.ShaderResourceIndex
                    };

                    meshMapping.Add(mesh.ResourceId, currentMeshIndex);
                    meshList[currentMeshIndex++] = shaderMesh;
                }

                var meshIndex = meshMapping[mesh.ResourceId];

                var shaderMeshInstance = new ShaderMeshInstance()
                {
                    MeshIndex = meshIndex,
                    Scale = meshInstance.Scale,
                    WorldMatrix = meshInstance.WorldMatrix,
                    WorldInvTransposeMatrix = meshInstance.WorldInvTransposeMatrix,
                    WorldBoundingBox = meshInstance.WorldBoundingBox
                };

                meshInstanceList[currentMeshInstanceIndex++] = shaderMeshInstance;
            }

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMesh>(this.cpuMeshBuffer, 0, meshList.AsSpan().Slice(0, (int)currentMeshIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMesh>(copyCommandList, this.meshBuffer, this.cpuMeshBuffer, currentMeshIndex);

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMeshInstance>(this.cpuMeshInstanceBuffer, 0, meshInstanceList.AsSpan().Slice(0, (int)currentMeshInstanceIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderMeshInstance>(copyCommandList, this.meshInstanceBuffer, this.cpuMeshInstanceBuffer, currentMeshInstanceIndex);

            this.graphicsManager.ResetIndirectCommandBuffer(copyCommandList, this.indirectCommandBuffer);

            this.renderManager.MeshCount = (int)currentMeshIndex;
            this.renderManager.MeshInstanceCount = (int)currentMeshInstanceIndex;

            this.currentMeshInstanceCount = currentMeshInstanceIndex;

            ArrayPool<ShaderMesh>.Shared.Return(meshList);
            ArrayPool<ShaderMeshInstance>.Shared.Return(meshInstanceList);
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

        private void RunRenderPipeline(GraphicsScene scene, Texture mainRenderTargetTexture)
        {
            if (mainRenderTargetTexture is null)
            {
                throw new ArgumentNullException(nameof(mainRenderTargetTexture));
            }

            if (renderManager.logFrameTime)
            {
                Logger.BeginAction("CopyGpuData");
            }

            var copyCommandList = CopyGpuData(scene);

            if (renderManager.logFrameTime)
            {
                Logger.EndAction();
            }

            var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList });

            var computeRenderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.ComputeCommandQueue, "ComputeRenderCommands");
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(computeRenderCommandList);
            this.graphicsManager.SetShader(computeRenderCommandList, this.computeRenderCommandsShader);

            this.graphicsManager.SetShaderParameterValues(computeRenderCommandList, 0, new uint[]
            {
                this.camerasBuffer.ShaderResourceIndex,
                scene.ShowMeshlets,
                this.meshBuffer.ShaderResourceIndex,
                this.meshInstanceBuffer.ShaderResourceIndex,
                this.indirectCommandBuffer.ShaderResourceIndex,
                this.currentMeshInstanceCount
            });

            // TODO: Don't hardcode wave size
            var waveSize = 32;
            this.graphicsManager.DispatchCompute(computeRenderCommandList, (uint)MathF.Ceiling((float)this.currentMeshInstanceCount / waveSize), 1, 1); 
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(computeRenderCommandList);
            this.graphicsManager.CommitCommandList(computeRenderCommandList);

            this.renderManager.AddGpuTiming("ComputeRenderCommands", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);
            var computeFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.ComputeCommandQueue, new CommandList[] { computeRenderCommandList }, new Fence[] { copyFence });

            if (renderManager.logFrameTime)
            {
                Logger.BeginAction("Create Depth Buffer");
            }

            var depthBuffer = this.graphicsManager.CreateTexture(GraphicsHeapType.TransientGpu, TextureFormat.Depth32Float, TextureUsage.RenderTarget, mainRenderTargetTexture.Width, mainRenderTargetTexture.Height, 1, 1, 1, isStatic: true, "DepthBuffer");

            if (renderManager.logFrameTime)
            {
                Logger.EndAction();
                Logger.BeginAction("Execute Command List");
            }

            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "RenderGeometry");

            var renderTarget = new RenderTargetDescriptor(mainRenderTargetTexture, Vector4.Zero, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthBuffer, DepthBufferOperation.ClearWrite, backfaceCulling: true, PrimitiveType.Triangle);

            startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor, this.renderMeshInstanceShader);

            // TODO: For the moment the resource states are in COMMON state
            // We need to have an api here or somewhere else to be able to Transition to 
            // Shader Read/write

            if (this.currentMeshInstanceCount > 0)
            {
                this.graphicsManager.ExecuteIndirect(renderCommandList, this.currentMeshInstanceCount, this.indirectCommandBuffer, 0);
            }

            this.graphicsManager.EndRenderPass(renderCommandList);
            endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming("RenderGeometry", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            // TODO: Submit render and debug command list at the same time
            this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { renderCommandList }, new Fence[] { computeFence });

            if (renderManager.logFrameTime)
            {
                Logger.EndAction();
                Logger.BeginAction("DebugRenderer");
            }

            this.debugRenderer.Render(this.camerasBuffer, mainRenderTargetTexture, depthBuffer);

            if (renderManager.logFrameTime)
            {
                Logger.EndAction();
            }
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
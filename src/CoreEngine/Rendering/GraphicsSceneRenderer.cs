using System;
using System.Collections.Generic;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

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

    readonly struct ShaderGeometryPacket
    {
        public int VertexBufferIndex { get; init; }
        public int IndexBufferIndex { get; init; }
    }

    readonly struct ShaderGeometryInstance
    {
        public int GeometryPacketIndex { get; init; }
        public int StartIndex { get; init; }
        public int VertexCount { get; init; }
        public int IndexCount { get; init; }
        public Matrix4x4 WorldMatrix { get; init; }
        public Matrix4x4 WorldInvTransposeMatrix { get; init; }
        public BoundingBox WorldBoundingBox { get; init; }
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer
    {
        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private readonly Shader renderMeshInstanceShader;

        private readonly GraphicsBuffer cpuGeometryPacketsBuffer;
        private readonly GraphicsBuffer geometryPacketsBuffer;
        
        private readonly GraphicsBuffer cpuGeometryInstancesBuffer;
        private readonly GraphicsBuffer geometryInstancesBuffer;

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

            this.renderMeshInstanceShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstance.shader");

            this.cpuGeometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeGeometryPackets");
            this.geometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeGeometryPackets");
            
            this.cpuGeometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(GraphicsHeapType.Upload, 100000, isStatic: false, label: "ComputeGeometryInstances");
            this.geometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(GraphicsHeapType.Gpu, 100000, isStatic: false, label: "ComputeGeometryInstances");

            this.cpuCamerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeCameras");
            this.camerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeCameras");
            
            this.cpuLightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Upload, 10000, isStatic: false, label: "ComputeLights");
            this.lightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(GraphicsHeapType.Gpu, 10000, isStatic: false, label: "ComputeLights");

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

        private void ProcessGeometry(CommandList copyCommandList, GraphicsScene scene)
        {
            var currentGeometryPacketIndex = 0;
            var currentGeometryInstanceIndex = 0;
            var currentVertexBufferId = IntPtr.Zero;

            var geometryPacketList = ArrayPool<ShaderGeometryPacket>.Shared.Rent(10000);
            var geometryInstanceList = ArrayPool<ShaderGeometryInstance>.Shared.Rent(100000);

            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];

                Matrix4x4.Invert(meshInstance.WorldMatrix, out var meshInstanceWorldInvTransposeMatrix);
                meshInstanceWorldInvTransposeMatrix = Matrix4x4.Transpose(meshInstanceWorldInvTransposeMatrix);

                var mesh = meshInstance.Mesh;
                this.debugRenderer.DrawBoundingBox(meshInstance.WorldBoundingBox, new Vector3(0, 0.5f, 1.0f));

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = mesh.GeometryInstances[j];
                    var geometryPacket = geometryInstance.GeometryPacket;

                    if (currentVertexBufferId != geometryPacket.VertexBuffer.NativePointer)
                    {
                        currentVertexBufferId = geometryPacket.VertexBuffer.NativePointer;

                        var shaderGeometryPacket = new ShaderGeometryPacket()
                        {
                            VertexBufferIndex = (int)geometryPacket.VertexBuffer.ShaderResourceIndex,
                            IndexBufferIndex = (int)geometryPacket.IndexBuffer.ShaderResourceIndex
                        };

                        geometryPacketList[currentGeometryPacketIndex++] = shaderGeometryPacket;
                    }

                    // TODO: Split mesh instance from geometry instance
                    var shaderGeometryInstance = new ShaderGeometryInstance()
                    {
                        GeometryPacketIndex = currentGeometryPacketIndex - 1,
                        StartIndex = geometryInstance.StartIndex,
                        IndexCount = geometryInstance.IndexCount,
                        VertexCount = geometryInstance.VertexCount,
                        WorldMatrix = meshInstance.WorldMatrix,
                        WorldInvTransposeMatrix = meshInstanceWorldInvTransposeMatrix,
                        WorldBoundingBox = meshInstance.WorldBoundingBox
                    };

                    geometryInstanceList[currentGeometryInstanceIndex++] = shaderGeometryInstance;
                }
            }

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderGeometryPacket>(this.cpuGeometryPacketsBuffer, 0, geometryPacketList.AsSpan().Slice(0, currentGeometryPacketIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.cpuGeometryPacketsBuffer, currentGeometryPacketIndex);
            
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderGeometryInstance>(this.cpuGeometryInstancesBuffer, 0, geometryInstanceList.AsSpan().Slice(0, currentGeometryInstanceIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, this.cpuGeometryInstancesBuffer, currentGeometryInstanceIndex);
            
            this.renderManager.GeometryInstancesCount = currentGeometryInstanceIndex;
            this.currentMeshInstanceCount = (uint)currentGeometryInstanceIndex;

            ArrayPool<ShaderGeometryPacket>.Shared.Return(geometryPacketList);
            ArrayPool<ShaderGeometryInstance>.Shared.Return(geometryInstanceList);
        }

        private void ProcessCamera(CommandList copyCommandList, GraphicsScene scene)
        {
            var currentCameraIndex = 0;
            ShaderCamera shaderCamera;

            if (scene.DebugCamera != null)
            {
                shaderCamera = new ShaderCamera()
                {
                    WorldPosition = scene.DebugCamera.WorldPosition,
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

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderCamera>(this.cpuCamerasBuffer, 0, cameraList.AsSpan().Slice(0, currentCameraIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cpuCamerasBuffer, currentCameraIndex);

            ArrayPool<ShaderCamera>.Shared.Return(cameraList);
        }

        private void ProcessLights(CommandList copyCommandList, GraphicsScene scene)
        {
            var currentLightIndex = 0;
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

            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderLight>(this.cpuLightsBuffer, 0, lightList.AsSpan().Slice(0, currentLightIndex));
            this.graphicsManager.CopyDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.cpuLightsBuffer, currentLightIndex);
            
            this.renderManager.LightsCount = currentLightIndex;
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

            var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList }, isAwaitable: true);

    
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

            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "TestRender");

            var renderTarget = new RenderTargetDescriptor(mainRenderTargetTexture, Vector4.Zero, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthBuffer, DepthBufferOperation.ClearWrite, backfaceCulling: true);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstanceShader);
            this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { this.geometryInstancesBuffer.ShaderResourceIndex, this.geometryPacketsBuffer.ShaderResourceIndex, this.camerasBuffer.ShaderResourceIndex });
            this.graphicsManager.DispatchMesh(renderCommandList, this.currentMeshInstanceCount, 1, 1);
        
            this.graphicsManager.EndRenderPass(renderCommandList);
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming("TestRender", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            this.graphicsManager.WaitForCommandQueue(this.renderManager.RenderCommandQueue, copyFence);
            this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] {renderCommandList}, isAwaitable: false);

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
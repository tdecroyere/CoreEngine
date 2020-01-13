using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Collections;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public struct ObjectProperties
    {
        public Matrix4x4 WorldMatrix { get; set; }
    }

    internal struct ShaderBoundingBox
    {
        public Vector3 MinPoint;
        public float Reserved1;
        public Vector3 MaxPoint;
        public float Reserved2;
    }

    internal struct ShaderBoundingFrustum
    {
        public Vector4 LeftPlane;
        public Vector4 RightPlane;
        public Vector4 TopPlane;
        public Vector4 BottomPlane;
        public Vector4 NearPlane;
        public Vector4 FarPlane;
    }

    internal struct ShaderCamera
    {
        public Vector3 WorldPosition;
        public float Reserved1;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 ProjectionMatrix;
        public ShaderBoundingFrustum BoundingFrustum;
    }

    internal struct ShaderSceneProperties
    {
        public ShaderCamera ActiveCamera;
        public ShaderCamera DebugCamera;
        public bool IsDebugCameraActive;
    }

    internal struct ShaderGeometryPacket
    {
        public int VertexBufferIndex;
        public int IndexBufferIndex;
    }

    internal struct ShaderGeometryInstance
    {
        public int GeometryPacketIndex;
        public int StartIndex;
        public int IndexCount;
        public int MaterialIndex;
        public int IsTransparent;
        public int Reserved1;
        public int Reserved2;
        public int Reserved3;
        public Matrix4x4 WorldMatrix;
        public ShaderBoundingBox WorldBoundingBox;
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer
    {
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private Shader drawMeshInstancesComputeShader;
        private Shader renderMeshInstancesDepthShader;
        private Shader renderMeshInstancesShader;
        private Shader renderMeshInstancesTransparentShader;
        private Shader computeHdrTransferShader;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants renderPassConstants;

        private Texture opaqueHdrRenderTarget;
        private Texture transparentHdrRenderTarget;
        private Texture transparentRevealageRenderTarget;
        private Texture depthBufferTexture;
        private Vector2 currentFrameSize;

        private readonly int multisampleCount = 4;

        // Compute shaders data structures
        private GraphicsBuffer scenePropertiesBuffer;
        private GraphicsBuffer geometryPacketsBuffer;
        private GraphicsBuffer geometryInstancesBuffer;
        private GraphicsBuffer materialOffsetBuffer;
        private CommandList indirectCommandList;

        public GraphicsSceneRenderer(GraphicsManager graphicsManager, GraphicsSceneQueue sceneQueue, ResourcesManager resourcesManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsManager = graphicsManager;
            this.debugRenderer = new DebugRenderer(graphicsManager, resourcesManager);
            this.sceneQueue = sceneQueue;

            this.drawMeshInstancesComputeShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeDrawMeshInstances.shader", "DrawMeshInstances");
            this.renderMeshInstancesDepthShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceDepth.shader");
            this.renderMeshInstancesShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstance.shader");
            this.renderMeshInstancesTransparentShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceTransparent.shader");
            this.computeHdrTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeHdrTransfer.shader");

            this.currentFrameSize = this.graphicsManager.GetRenderSize();
            this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererOpaqueHdrRenderTarget");
            this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");
            this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentRevealageRenderTarget");
            this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererDepthBuffer");

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(1, GraphicsResourceType.Dynamic);

            // Compute buffers
            this.scenePropertiesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderSceneProperties>(1, GraphicsResourceType.Dynamic, "ComputeSceneProperties");
            this.geometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(10000, GraphicsResourceType.Dynamic, "ComputeGeometryPackets");
            this.geometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(10000, GraphicsResourceType.Dynamic, "ComputeGeometryInstances");
            this.materialOffsetBuffer = this.graphicsManager.CreateGraphicsBuffer<int>(10000, GraphicsResourceType.Dynamic, "MaterialOffsets");

            this.indirectCommandList = this.graphicsManager.CreateIndirectCommandList(65536, "ComputeIndirectCommandList");
        }

        public void Render()
        {
            var frameSize = this.graphicsManager.GetRenderSize();

            if (frameSize != this.currentFrameSize)
            {
                Logger.WriteMessage("Recreating Scene Renderer Render Targets");
                this.currentFrameSize = frameSize;
                
                this.graphicsManager.RemoveTexture(this.opaqueHdrRenderTarget);
                this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererOpaqueHdrRenderTarget");

                this.graphicsManager.RemoveTexture(this.transparentHdrRenderTarget);
                this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");

                this.graphicsManager.RemoveTexture(this.transparentRevealageRenderTarget);
                this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");

                this.graphicsManager.RemoveTexture(this.depthBufferTexture);
                this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererDepthBuffer");
            }

            var scene = this.sceneQueue.WaitForNextScene();
            var camera = scene.DebugCamera;

            if (camera == null)
            {
                camera = scene.ActiveCamera;
            }

            if (camera != null)
            {
                SetupCamera(camera);
            }

            CopyComputeGpuData(scene);
            CopyGpuData();
            RunRenderPipeline(scene);
        }

        GraphicsBuffer[] vertexBuffersList = new GraphicsBuffer[10000];
        int currentVertexBufferIndex = 0;

        GraphicsBuffer[] indexBuffersList = new GraphicsBuffer[10000];
        int currentIndexBufferIndex = 0;

        ShaderGeometryPacket[] geometryPacketList = new ShaderGeometryPacket[10000];
        int currentGeometryPacketIndex = 0;

        ShaderGeometryInstance[] geometryInstanceList = new ShaderGeometryInstance[10000];
        int currentGeometryInstanceIndex = 0;

        GraphicsBuffer[] materialList = new GraphicsBuffer[10000];
        int[] materialOffsetList = new int[10000];
        int currentMaterialIndex = 0;

        Texture[] materialTextureList = new Texture[10000];
        int currentMaterialTextureIndex = 0;

        private void CopyComputeGpuData(GraphicsScene scene)
        {
            var sceneProperties = new ShaderSceneProperties();

            // Fill Data
            if (scene.ActiveCamera != null)
            {
                var camera = scene.ActiveCamera;

                var boundingFrutum = new ShaderBoundingFrustum()
                {
                    LeftPlane = new Vector4(camera.BoundingFrustum.LeftPlane.Normal, camera.BoundingFrustum.LeftPlane.D),
                    RightPlane = new Vector4(camera.BoundingFrustum.RightPlane.Normal, camera.BoundingFrustum.RightPlane.D),
                    TopPlane = new Vector4(camera.BoundingFrustum.TopPlane.Normal, camera.BoundingFrustum.TopPlane.D),
                    BottomPlane = new Vector4(camera.BoundingFrustum.BottomPlane.Normal, camera.BoundingFrustum.BottomPlane.D),
                    NearPlane = new Vector4(camera.BoundingFrustum.NearPlane.Normal, camera.BoundingFrustum.NearPlane.D),
                    FarPlane = new Vector4(camera.BoundingFrustum.FarPlane.Normal, camera.BoundingFrustum.FarPlane.D)
                };

                sceneProperties.ActiveCamera = new ShaderCamera()
                {
                    WorldPosition = scene.ActiveCamera.WorldPosition,
                    ViewMatrix = scene.ActiveCamera.ViewMatrix,
                    ProjectionMatrix = scene.ActiveCamera.ProjectionMatrix,
                    BoundingFrustum = boundingFrutum
                };
            }

            if (scene.DebugCamera != null)
            {
                sceneProperties.DebugCamera = new ShaderCamera()
                {
                    WorldPosition = scene.DebugCamera.WorldPosition,
                    ViewMatrix = scene.DebugCamera.ViewMatrix,
                    ProjectionMatrix = scene.DebugCamera.ProjectionMatrix
                };
            }

            sceneProperties.IsDebugCameraActive = (scene.DebugCamera != null);

            this.currentVertexBufferIndex = 0;
            uint currentVertexBufferId = (uint)0;

            this.currentIndexBufferIndex = 0;
            this.currentGeometryPacketIndex = 0;
            this.currentGeometryInstanceIndex = 0;

            this.currentMaterialIndex = 0;
            uint currentMaterialId = (uint)0;

            this.currentMaterialTextureIndex = 0;
            var currentMeshMaterialIndex = -1;

            // TODO: For the moment it only work if the mesh instances list is sorted by meshes!
            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];
                var mesh = meshInstance.Mesh;

                if (!mesh.IsLoaded || (meshInstance.Material != null && !meshInstance.Material.IsLoaded))
                {
                    continue;
                }

                if (meshInstance.Material != null && meshInstance.Material.MaterialData != null && currentMaterialId != meshInstance.Material.MaterialData.Value.GraphicsResourceId)
                {
                    currentMaterialId = meshInstance.Material.MaterialData.Value.GraphicsResourceId;
                    
                    this.materialList[this.currentMaterialIndex] = meshInstance.Material.MaterialData.Value;
                    this.materialOffsetList[this.currentMaterialIndex] = this.currentMaterialTextureIndex;
                    this.currentMaterialIndex++;

                    var textureList = meshInstance.Material.TextureList.Span;

                    for (var j = 0; j < textureList.Length; j++)
                    {
                        this.materialTextureList[this.currentMaterialTextureIndex++] = textureList[j];
                    }

                    currentMeshMaterialIndex = this.currentMaterialIndex - 1;
                }

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = mesh.GeometryInstances[j];
                    var geometryPacket = geometryInstance.GeometryPacket;

                    if (geometryInstance.Material != null && !geometryInstance.Material.IsLoaded)
                    {
                        continue;
                    }

                    if (geometryInstance.Material != null && geometryInstance.Material.MaterialData != null && currentMaterialId != geometryInstance.Material.MaterialData.Value.GraphicsResourceId)
                    {
                        currentMaterialId = geometryInstance.Material.MaterialData.Value.GraphicsResourceId;
                        
                        this.materialList[this.currentMaterialIndex] = geometryInstance.Material.MaterialData.Value;
                        this.materialOffsetList[this.currentMaterialIndex] = this.currentMaterialTextureIndex;
                        this.currentMaterialIndex++;

                        var textureList = geometryInstance.Material.TextureList.Span;

                        for (var k = 0; k < textureList.Length; k++)
                        {
                            if (textureList[k].IsLoaded)
                            {
                                this.materialTextureList[this.currentMaterialTextureIndex++] = textureList[k];
                            }
                        }
                    }

                    if (currentVertexBufferId != geometryPacket.VertexBuffer.GraphicsResourceId)
                    {
                        this.vertexBuffersList[currentVertexBufferIndex++] = geometryPacket.VertexBuffer;
                        currentVertexBufferId = geometryPacket.VertexBuffer.GraphicsResourceId;

                        this.indexBuffersList[currentIndexBufferIndex++] = geometryPacket.IndexBuffer;

                        var shaderGeometryPacket = new ShaderGeometryPacket()
                        {
                            VertexBufferIndex = currentVertexBufferIndex - 1,
                            IndexBufferIndex = currentIndexBufferIndex - 1
                        };

                        this.geometryPacketList[currentGeometryPacketIndex++] = shaderGeometryPacket;
                    }

                    var worldBoundingBox = new ShaderBoundingBox();

                    if (meshInstance.WorldBoundingBoxList.Count > j)
                    {
                        worldBoundingBox = new ShaderBoundingBox()
                        {
                            MinPoint = meshInstance.WorldBoundingBoxList[j].MinPoint,
                            MaxPoint = meshInstance.WorldBoundingBoxList[j].MaxPoint
                        };
                    }

                    var shaderGeometryInstance = new ShaderGeometryInstance()
                    {
                        GeometryPacketIndex = currentGeometryPacketIndex - 1,
                        StartIndex = geometryInstance.StartIndex,
                        IndexCount = geometryInstance.IndexCount,
                        MaterialIndex = (meshInstance.Material != null) ? currentMeshMaterialIndex : ((geometryInstance.Material != null) ? (currentMaterialIndex - 1) : -1),
                        IsTransparent = (meshInstance.Material != null) ? (meshInstance.Material.IsTransparent ? 1 : 0) : ((geometryInstance.Material != null) ? (geometryInstance.Material.IsTransparent ? 1 : 0) : 0),
                        WorldMatrix = meshInstance.WorldMatrix,
                        WorldBoundingBox = worldBoundingBox
                    };

                    geometryInstanceList[this.currentGeometryInstanceIndex++] = shaderGeometryInstance;
                }
            }

            // Copy buffers
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("SceneComputeCopyCommandList", true);

            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, new ShaderSceneProperties[] { sceneProperties });
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.geometryPacketList.AsSpan().Slice(0, this.currentGeometryPacketIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, geometryInstanceList.AsSpan().Slice(0, this.currentGeometryInstanceIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<int>(copyCommandList, this.materialOffsetBuffer, this.materialOffsetList.AsSpan().Slice(0, this.currentMaterialIndex));
            this.graphicsManager.ResetIndirectCommandList(copyCommandList, this.indirectCommandList, this.currentGeometryInstanceIndex);

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
            this.graphicsManager.GeometryInstancesCount = this.currentGeometryInstanceIndex;
        }

        private void RunRenderPipeline(GraphicsScene scene)
        {
            // Encore indirect command lists
            var computeCommandList = this.graphicsManager.CreateComputeCommandList("ComputeRenderGeometryInstances");
            
            this.graphicsManager.SetShader(computeCommandList, this.drawMeshInstancesComputeShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.scenePropertiesBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryPacketsBuffer, 1);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryInstancesBuffer, 2);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.vertexBuffersList.AsSpan().Slice(0, this.currentVertexBufferIndex), 3);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.indexBuffersList.AsSpan().Slice(0, this.currentIndexBufferIndex), 10003);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.materialList.AsSpan().Slice(0, this.currentMaterialIndex), 20003);
            this.graphicsManager.SetShaderTextures(computeCommandList, this.materialTextureList.AsSpan().Slice(0, this.currentMaterialTextureIndex), 30003);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.materialOffsetBuffer, 40003);
            this.graphicsManager.SetShaderIndirectCommandList(computeCommandList, this.indirectCommandList, 40004);

            this.graphicsManager.DispatchThreads(computeCommandList, (uint)this.currentGeometryInstanceIndex, 1, 1);
            this.graphicsManager.ExecuteComputeCommandList(computeCommandList);

            // Optimize indirect command lists pass
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("ComputeOptimizeRenderCommandList");
            this.graphicsManager.OptimizeIndirectCommandList(copyCommandList, this.indirectCommandList, this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            // Depth Buffer pass
            var renderPassDescriptor = new RenderPassDescriptor(null, this.depthBufferTexture, DepthBufferOperation.Write, false);
            var renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "DepthBufferCommandList");

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesDepthShader);
            this.graphicsManager.ExecuteIndirectCommandList(renderCommandList, this.indirectCommandList, this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            // Render pass
            var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(0.0f, 0.215f, 1.0f, 1), BlendOperation.None);
            renderPassDescriptor = new RenderPassDescriptor(renderTarget1, this.depthBufferTexture, DepthBufferOperation.CompareEqual, false);
            renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "MainRenderCommandList");

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesShader);
            this.graphicsManager.ExecuteIndirectCommandList(renderCommandList, this.indirectCommandList, this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            // Transparent Render pass
            var renderTarget2 = new RenderTargetDescriptor(this.transparentHdrRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneOne);
            var renderTarget3 = new RenderTargetDescriptor(this.transparentRevealageRenderTarget, new Vector4(1.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneMinusSourceColor);
            renderPassDescriptor = new RenderPassDescriptor(renderTarget2, renderTarget3, this.depthBufferTexture, DepthBufferOperation.CompareLess, false);
            renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "TransparentRenderCommandList");

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesTransparentShader);
            this.graphicsManager.ExecuteIndirectCommandList(renderCommandList, this.indirectCommandList, this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            // Debug pass
            var renderTarget = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, null, BlendOperation.None);
            var debugRenderPassDescriptor = new RenderPassDescriptor(renderTarget, this.depthBufferTexture, DepthBufferOperation.CompareLess, true);
            var debugRenderCommandList = this.graphicsManager.CreateRenderCommandList(debugRenderPassDescriptor, "DebugRenderCommandList");

            this.debugRenderer.ClearDebugLines();
            DrawCameraBoundingFrustum(scene);
            DrawGeometryInstancesBoundingBox(scene);

            this.debugRenderer.Render(this.renderPassParametersGraphicsBuffer, debugRenderCommandList);

            this.graphicsManager.ExecuteRenderCommandList(debugRenderCommandList);

            // Hdr Transfer pass
            renderTarget = new RenderTargetDescriptor(this.graphicsManager.MainRenderTargetTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var hdrTransferRenderCommandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "HdrTransfer");
            
            this.graphicsManager.SetShader(hdrTransferRenderCommandList, this.computeHdrTransferShader);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.opaqueHdrRenderTarget, 0);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.transparentHdrRenderTarget, 1);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.transparentRevealageRenderTarget, 2);

            this.graphicsManager.DrawPrimitives(hdrTransferRenderCommandList, GeometryPrimitiveType.TriangleStrip, 0, 4);
            this.graphicsManager.ExecuteRenderCommandList(hdrTransferRenderCommandList);
        }

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }

        private void CopyGpuData()
        {
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("SceneCopyCommandList");

            this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants[] {renderPassConstants});

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
            this.debugRenderer.CopyDataToGpu();
        }

        private void DrawCameraBoundingFrustum(GraphicsScene scene)
        {
            for (var i = 0; i < scene.Cameras.Count; i++)
            {
                var camera = scene.Cameras[i];
                var color = new Vector3(0, 0, 1);

                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftTopNearPoint, camera.BoundingFrustum.LeftTopFarPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftBottomNearPoint, camera.BoundingFrustum.LeftBottomFarPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.RightTopNearPoint, camera.BoundingFrustum.RightTopFarPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.RightBottomNearPoint, camera.BoundingFrustum.RightBottomFarPoint, color);

                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftTopNearPoint, camera.BoundingFrustum.RightTopNearPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftBottomNearPoint, camera.BoundingFrustum.RightBottomNearPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftTopNearPoint, camera.BoundingFrustum.LeftBottomNearPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.RightTopNearPoint, camera.BoundingFrustum.RightBottomNearPoint, color);

                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftTopFarPoint, camera.BoundingFrustum.RightTopFarPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftBottomFarPoint, camera.BoundingFrustum.RightBottomFarPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.LeftTopFarPoint, camera.BoundingFrustum.LeftBottomFarPoint, color);
                this.debugRenderer.DrawLine(camera.BoundingFrustum.RightTopFarPoint, camera.BoundingFrustum.RightBottomFarPoint, color);
            }
        }

        private void DrawGeometryInstancesBoundingBox(GraphicsScene scene)
        {
            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];

                if (meshInstance.Mesh.IsLoaded)
                {
                    for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
                    {
                        var geometryInstance = meshInstance.Mesh.GeometryInstances[j];

                        if (meshInstance.WorldBoundingBoxList.Count > j)
                        {
                            var worldBoundingBox = meshInstance.WorldBoundingBoxList[j];

                            var point1 = worldBoundingBox.MinPoint;
                            var point2 = worldBoundingBox.MinPoint + new Vector3(0, 0, worldBoundingBox.ZSize);
                            var point3 = worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, 0);
                            var point4 = worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, worldBoundingBox.ZSize);
                            var point5 = worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, 0);
                            var point6 = worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, worldBoundingBox.ZSize);
                            var point7 = worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, 0);
                            var point8 = worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, worldBoundingBox.ZSize);

                            this.debugRenderer.DrawLine(point1, point2);
                            this.debugRenderer.DrawLine(point1, point3);
                            this.debugRenderer.DrawLine(point2, point4);
                            this.debugRenderer.DrawLine(point3, point4);
                            
                            this.debugRenderer.DrawLine(point5, point6);
                            this.debugRenderer.DrawLine(point5, point7);
                            this.debugRenderer.DrawLine(point6, point8);
                            this.debugRenderer.DrawLine(point7, point8);
                            
                            this.debugRenderer.DrawLine(point1, point5);
                            this.debugRenderer.DrawLine(point3, point7);
                            this.debugRenderer.DrawLine(point4, point8);
                            this.debugRenderer.DrawLine(point2, point6);
                        }
                    }
                }
            }
        }
    }
}
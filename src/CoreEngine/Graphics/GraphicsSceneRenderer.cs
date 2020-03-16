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
        public Vector3 MaxPoint;
        public float Reserved1;
        public float Reserved2;
    }

    internal struct ShaderBoundingFrustum
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

        public Vector4 LeftPlane;
        public Vector4 RightPlane;
        public Vector4 TopPlane;
        public Vector4 BottomPlane;
        public Vector4 NearPlane;
        public Vector4 FarPlane;
    }

    internal struct ShaderCamera
    {
        public int DepthBufferTextureIndex;
        public Vector3 WorldPosition;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 ViewProjectionMatrix;
        public Matrix4x4 ViewProjectionMatrixInverse;
        public ShaderBoundingFrustum BoundingFrustum;
        public int OpaqueCommandListIndex;
        public int OpaqueDepthCommandListIndex;
        public int TransparentCommandListIndex;
        public int TransparentDepthCommandListIndex;
        public bool DepthOnly;
        public bool AlreadyProcessed;
        public byte Reserved2;
        public byte Reserved3;
        public float MinDepth;
        public float MaxDepth;
        public int MomentShadowMapIndex;
        public int OcclusionDepthTextureIndex;
        public int OcclusionDepthCommandListIndex;
        public int Reserved4;
        public int Reserved5;
    }

    internal struct ShaderLight
    {
        public Vector3 WorldSpacePosition;
        public int Camera1;
        public int Camera2;
        public int Camera3;
        public int Camera4;
        public int Reserved;
    }

    internal struct ShaderMaterial
    {
        public int MaterialBufferIndex;
        public int MaterialTextureOffset;
        public bool IsTransparent;
        public byte Reserved1;
        public byte Reserved2;
        public byte Reserved3;
    }

    internal struct ShaderSceneProperties
    {
        public int ActiveCameraIndex;
        public int DebugCameraIndex;
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
        public Matrix4x4 WorldMatrix;
        public ShaderBoundingBox WorldBoundingBox;
    }

    public enum GraphicsPipelineStepType
    {
        Compute,
        RenderQuad,
        RenderIndirectCommandList
    }

    // public class GraphicsPipelineResource
    // {

    // }

    // public class GraphicsPipelineStep
    // {
    //     public string Name;
    //     public GraphicsPipelineStepType Type;
    //     public Shader Shader;
    //     public IList<GraphicsPipelineResource> Inputs;
    //     public IList<GraphicsPipelineResource> Outputs;
    // }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer
    {
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private Shader drawMeshInstancesComputeShader;
        private Shader computeMinMaxDepthInitialShader;
        private Shader computeMinMaxDepthStepShader;
        private Shader computeLightCamerasShader;
        private Shader renderMeshInstancesDepthShader;
        private Shader renderMeshInstancesShader;
        private Shader renderMeshInstancesTransparentShader;
        private Shader renderMeshInstancesTransparentDepthShader;
        private Shader convertToMomentShadowMapShader;
        private Shader computeHdrTransferShader;
        private Shader gaussianBlurHorizontalShader;
        private Shader gaussianBlurVerticalShader;
        private Shader bloomPassShader;
        private Shader toneMapShader;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants renderPassConstants;

        private Texture opaqueHdrRenderTarget;
        private Texture transparentHdrRenderTarget;
        private Texture transparentRevealageRenderTarget;
        private Texture depthBufferTexture;
        private Texture occlusionDepthTexture;
        private Texture bloomRenderTarget;
        private Vector2 currentFrameSize;

        private Texture cubeMap;
        private Texture irradianceCubeMap;

        private readonly int multisampleCount = 1;

        // Compute shaders data structures
        private GraphicsBuffer scenePropertiesBuffer;
        private GraphicsBuffer camerasBuffer;
        private GraphicsBuffer lightsBuffer;
        private GraphicsBuffer materialsBuffer;
        private GraphicsBuffer geometryPacketsBuffer;
        private GraphicsBuffer geometryInstancesBuffer;
        private GraphicsBuffer indirectCommandBufferCounters;

        private GraphicsBuffer minMaxDepthComputeBuffer;

        public GraphicsSceneRenderer(GraphicsManager graphicsManager, GraphicsSceneQueue sceneQueue, ResourcesManager resourcesManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsManager = graphicsManager;
            this.debugRenderer = new DebugRenderer(graphicsManager, resourcesManager);
            this.sceneQueue = sceneQueue;

            this.drawMeshInstancesComputeShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeGenerateIndirectCommands.shader", "GenerateIndirectCommands");
            this.computeMinMaxDepthInitialShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeMinMaxDepth.shader", "ComputeMinMaxDepthInitial");
            this.computeMinMaxDepthStepShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeMinMaxDepth.shader", "ComputeMinMaxDepthStep");
            this.computeLightCamerasShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeLightCameras.shader", "ComputeLightCameras");
            this.renderMeshInstancesDepthShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceDepth.shader");
            this.renderMeshInstancesShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstance.shader");
            this.renderMeshInstancesTransparentShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceTransparent.shader");
            this.renderMeshInstancesTransparentDepthShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceTransparentDepth.shader");
            this.convertToMomentShadowMapShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ConvertToMomentShadowMap.shader");
            this.computeHdrTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeHdrTransfer.shader");
            this.gaussianBlurHorizontalShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/GaussianBlurHorizontal.shader");
            this.gaussianBlurVerticalShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/GaussianBlurVertical.shader");
            this.bloomPassShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/BloomPass.shader");
            this.toneMapShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ToneMap.shader");

            this.currentFrameSize = this.graphicsManager.GetRenderSize();
            this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererOpaqueHdrRenderTarget");
            this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");
            this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentRevealageRenderTarget");
            this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererDepthBuffer");
            this.occlusionDepthTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, 1, true, GraphicsResourceType.Static, "OcclusionDepthTexture");
            this.bloomRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, 1, true);

            this.minMaxDepthComputeBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector2>(10000, GraphicsResourceType.Static, true, "ComputeMinMaxDepthWorkingBuffer");

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(1, GraphicsResourceType.Dynamic);

            this.cubeMap = resourcesManager.LoadResourceAsync<Texture>("/BistroV4/san_giuseppe_bridge_4k_cubemap.texture");
            this.irradianceCubeMap = resourcesManager.LoadResourceAsync<Texture>("/BistroV4/san_giuseppe_bridge_4k_irradiance_cubemap.texture");

            // Compute buffers
            this.scenePropertiesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderSceneProperties>(1, GraphicsResourceType.Dynamic, true, "ComputeSceneProperties");
            this.camerasBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderCamera>(10000, GraphicsResourceType.Dynamic, true, "ComputeCameras");
            this.lightsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(10000, GraphicsResourceType.Dynamic, true, "ComputeLights");
            this.materialsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderLight>(10000, GraphicsResourceType.Dynamic, true, "ComputeMaterials");
            this.geometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(10000, GraphicsResourceType.Dynamic, true, "ComputeGeometryPackets");
            this.geometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(100000, GraphicsResourceType.Dynamic, true, "ComputeGeometryInstances");
            this.indirectCommandBufferCounters = this.graphicsManager.CreateGraphicsBuffer<uint>(100, GraphicsResourceType.Dynamic, false, "ComputeIndirectCommandBufferCounters");
        }

        public CommandList Render()
        {
            var frameSize = this.graphicsManager.GetRenderSize();

            if (frameSize != this.currentFrameSize)
            {
                Logger.WriteMessage("Recreating Scene Renderer Render Targets");
                this.currentFrameSize = frameSize;
                
                this.graphicsManager.RemoveTexture(this.opaqueHdrRenderTarget);
                this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererOpaqueHdrRenderTarget");

                this.graphicsManager.RemoveTexture(this.transparentHdrRenderTarget);
                this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");

                this.graphicsManager.RemoveTexture(this.transparentRevealageRenderTarget);
                this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");

                this.graphicsManager.RemoveTexture(this.depthBufferTexture);
                this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererDepthBuffer");
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

            // TODO: Move that to render pipeline
            this.debugRenderer.ClearDebugLines();

            var copyCommandList = CopyComputeGpuData(scene);
            // CopyGpuData();
            return RunRenderPipeline(scene, copyCommandList);
        }

        GraphicsBuffer[] graphicsBufferList = new GraphicsBuffer[10000];
        int currentGraphicsBufferIndex = 0;

        ShaderCamera[] cameraList = new ShaderCamera[10000];
        int currentCameraIndex = 0;

        ShaderLight[] lightList = new ShaderLight[10000];
        int currentLightIndex = 0;

        ShaderMaterial[] materialList = new ShaderMaterial[10000];
        Dictionary<uint, int> materialListIndexes = new Dictionary<uint, int>();
        int currentMaterialIndex = 0;

        ShaderGeometryPacket[] geometryPacketList = new ShaderGeometryPacket[10000];
        int currentGeometryPacketIndex = 0;

        ShaderGeometryInstance[] geometryInstanceList = new ShaderGeometryInstance[100000];
        int currentGeometryInstanceIndex = 0;

        Texture[] textureList = new Texture[10000];
        int currentTextureIndex = 0;

        Texture[] cubeTextureList = new Texture[10000];
        int currentCubeTextureIndex = 0;

        Texture[] shadowMaps = new Texture[100];
        int currentShadowMapIndex = 0;

        IndirectCommandBuffer[] indirectCommandBufferList = new IndirectCommandBuffer[100];
        int currentIndirectCommandBufferIndex = 0;

        private int AddIndirectCommandBuffer()
        {
            if (this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex].GraphicsResourceId == 0)
            {
                this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex] = this.graphicsManager.CreateIndirectCommandBuffer(65536, GraphicsResourceType.Dynamic, "ComputeIndirectLightCommandList");
                Logger.WriteMessage("Create Indirect Buffer");
            }

            return this.currentIndirectCommandBufferIndex++;
        }

        private int AddTexture(Texture texture)
        {
            this.textureList[this.currentTextureIndex] = texture;
            return this.currentTextureIndex++;
        }

        private int AddCubeTexture(Texture texture)
        {
            this.cubeTextureList[this.currentCubeTextureIndex] = texture;
            return this.currentCubeTextureIndex++;
        }

        private int AddShadowMap(int shadowMapSize, bool isMomentShadowMap)
        {
            // TODO: Use resource aliasing
            if (this.shadowMaps[this.currentShadowMapIndex] == null)
            {
                if (!isMomentShadowMap)
                {
                    Logger.WriteMessage("Create Shadow map");
                    this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, shadowMapSize, shadowMapSize, 1, 1, 1, true, GraphicsResourceType.Static, "ShadowMapDepth");
                }

                else
                {
                    Logger.WriteMessage("Create Moment Shadow map");
                    this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Unorm, shadowMapSize, shadowMapSize, 1, 1, 1, true, GraphicsResourceType.Static, "ShadowMapMoment");
                }
            }

            return this.currentShadowMapIndex++;
        }

        private int AddCamera(ShaderCamera camera, Texture depthTexture, Texture? momentShadowMap, Texture? occlusionDepthTexture)
        {
            this.cameraList[this.currentCameraIndex] = camera;
            
            if (!Matrix4x4.Invert(this.cameraList[this.currentCameraIndex].ViewProjectionMatrix, out this.cameraList[this.currentCameraIndex].ViewProjectionMatrixInverse))
            {
                //Logger.WriteMessage($"Camera Error");
            }

            this.cameraList[this.currentCameraIndex].DepthBufferTextureIndex = AddTexture(depthTexture);

            if (momentShadowMap != null)
            {
                this.cameraList[this.currentCameraIndex].MomentShadowMapIndex = AddTexture(momentShadowMap);
            }

            if (occlusionDepthTexture != null)
            {
                this.cameraList[this.currentCameraIndex].OcclusionDepthTextureIndex = AddTexture(occlusionDepthTexture);
            }

            this.cameraList[this.currentCameraIndex].OpaqueDepthCommandListIndex = AddIndirectCommandBuffer();
            this.cameraList[this.currentCameraIndex].TransparentDepthCommandListIndex = AddIndirectCommandBuffer();
            this.cameraList[this.currentCameraIndex].OcclusionDepthCommandListIndex = AddIndirectCommandBuffer();
            
            if (!camera.DepthOnly)
            {
                this.cameraList[this.currentCameraIndex].OpaqueCommandListIndex = AddIndirectCommandBuffer();
                this.cameraList[this.currentCameraIndex].TransparentCommandListIndex = AddIndirectCommandBuffer();
            }

            return this.currentCameraIndex++;
        }

        private int AddLight(ShaderLight light)
        {
            this.lightList[this.currentLightIndex] = light;
            return this.currentLightIndex++;
        }

        private int AddMaterial(Material material)
        {
            if (material.MaterialData == null)
            {
                return -1;
            }

            if (this.materialListIndexes.ContainsKey(material.ResourceId))
            {
                return this.materialListIndexes[material.ResourceId];
            }

            this.materialList[this.currentMaterialIndex].MaterialBufferIndex = AddGraphicsBuffer(material.MaterialData.Value);
            this.materialList[this.currentMaterialIndex].MaterialTextureOffset = this.currentTextureIndex;
            this.materialList[this.currentMaterialIndex].IsTransparent = material.IsTransparent;

            var textureList = material.TextureList.Span;

            for (var i = 0; i < textureList.Length; i++)
            {
                AddTexture(textureList[i]);
            }

            this.materialListIndexes.Add(material.ResourceId, this.currentMaterialIndex);
            return this.currentMaterialIndex++;
        }

        private int AddGraphicsBuffer(GraphicsBuffer graphicsBuffer)
        {
            this.graphicsBufferList[this.currentGraphicsBufferIndex] = graphicsBuffer;
            return this.currentGraphicsBufferIndex++;
        }

        private int AddGeometryPacket(GeometryPacket geometryPacket)
        {
            var shaderGeometryPacket = new ShaderGeometryPacket()
            {
                VertexBufferIndex = AddGraphicsBuffer(geometryPacket.VertexBuffer),
                IndexBufferIndex = AddGraphicsBuffer(geometryPacket.IndexBuffer)
            };

            this.geometryPacketList[this.currentGeometryPacketIndex] = shaderGeometryPacket;
            return this.currentGeometryPacketIndex++;
        }

        private CommandList CopyComputeGpuData(GraphicsScene scene)
        {
            this.currentGraphicsBufferIndex = 0;
            this.currentGeometryPacketIndex = 0;
            this.currentCameraIndex = 0;
            this.currentLightIndex = 0;
            this.currentMaterialIndex = 0;
            this.materialListIndexes.Clear();
            this.currentGeometryInstanceIndex = 0;

            this.currentTextureIndex = 0;
            this.currentCubeTextureIndex = 0;
            this.currentShadowMapIndex = 0;

            this.currentIndirectCommandBufferIndex = 0;

            var currentMeshMaterialIndex = -1;
            var currentGeometryInstanceMaterialIndex = -1;
            var currentVertexBufferId = (uint)0;

            var sceneProperties = new ShaderSceneProperties();

            // Fill Data
            if (scene.ActiveCamera != null)
            {
                if (scene.DebugCamera != null)
                {
                    var camera = scene.DebugCamera;

                    var shaderCamera = new ShaderCamera()
                    {
                        WorldPosition = camera.WorldPosition,
                        ViewMatrix = camera.ViewMatrix,
                        ProjectionMatrix = camera.ProjectionMatrix,
                        ViewProjectionMatrix = camera.ViewProjectionMatrix,
                        BoundingFrustum = new ShaderBoundingFrustum(scene.ActiveCamera.BoundingFrustum),
                        MinDepth = camera.NearPlaneDistance,
                        MaxDepth = camera.FarPlaneDistance
                    };

                    sceneProperties.DebugCameraIndex = AddCamera(shaderCamera, this.depthBufferTexture, null, this.occlusionDepthTexture);
                }

                else
                {
                    var camera = scene.ActiveCamera;

                    var shaderCamera = new ShaderCamera()
                    {
                        WorldPosition = camera.WorldPosition,
                        ViewMatrix = camera.ViewMatrix,
                        ProjectionMatrix = camera.ProjectionMatrix,
                        ViewProjectionMatrix = camera.ViewProjectionMatrix,
                        BoundingFrustum = new ShaderBoundingFrustum(camera.BoundingFrustum),
                        MinDepth = camera.NearPlaneDistance,
                        MaxDepth = camera.FarPlaneDistance
                    };

                    sceneProperties.ActiveCameraIndex = AddCamera(shaderCamera, this.depthBufferTexture, null, this.occlusionDepthTexture);
                }
            }

            sceneProperties.IsDebugCameraActive = (scene.DebugCamera != null);

            // TEST LIGHT BUFFER
            AddCubeTexture(this.cubeMap);
            AddCubeTexture(this.irradianceCubeMap);

            var lightDirection = Vector3.Normalize(-new Vector3(0.172f, -0.818f, -0.549f));
            
            // var shadowMapSize = 1024;
            var shadowMapSize = 2048;
            var cascadeCount = 4;

            var shaderLight = new ShaderLight();
            shaderLight.WorldSpacePosition = lightDirection;

            for (var i = 0; i < cascadeCount; i++)
            {
                ref var lightCameraIndex = ref shaderLight.Camera1;

                switch (i)
                {
                    case 1:
                        lightCameraIndex = ref shaderLight.Camera2;
                        break;
                    case 2:
                        lightCameraIndex = ref shaderLight.Camera3;
                        break;
                    case 3:
                        lightCameraIndex = ref shaderLight.Camera4;
                        break;
                }

                var lightCamera = new ShaderCamera();
                lightCamera.DepthOnly = true;
                lightCamera.WorldPosition = lightDirection;

                var shadowMapIndex = AddShadowMap(shadowMapSize, false);
                var momentShadowMapIndex = AddShadowMap(shadowMapSize, true);

                lightCameraIndex = AddCamera(lightCamera, this.shadowMaps[shadowMapIndex], this.shadowMaps[momentShadowMapIndex], null);

                //this.debugRenderer.DrawBoundingFrustum(lightCamera1.BoundingFrustum, new Vector3(0, 1, 0));
            }

            AddLight(shaderLight);

            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];
                var mesh = meshInstance.Mesh;

                if (meshInstance.Material != null)
                {
                    currentMeshMaterialIndex = AddMaterial(meshInstance.Material);
                }

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    if (this.currentGeometryInstanceIndex > 2000)
                    {
                        break;
                    }

                    var geometryInstance = mesh.GeometryInstances[j];
                    var geometryPacket = geometryInstance.GeometryPacket;

                    if (geometryInstance.Material != null)
                    {
                        currentGeometryInstanceMaterialIndex = AddMaterial(geometryInstance.Material);
                    }

                    if (currentVertexBufferId != geometryPacket.VertexBuffer.GraphicsResourceId)
                    {
                        currentVertexBufferId = geometryPacket.VertexBuffer.GraphicsResourceId;
                        AddGeometryPacket(geometryPacket);
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
                        GeometryPacketIndex = this.currentGeometryPacketIndex - 1,
                        StartIndex = geometryInstance.StartIndex,
                        IndexCount = geometryInstance.IndexCount,
                        MaterialIndex = (meshInstance.Material != null) ? currentMeshMaterialIndex : ((geometryInstance.Material != null) ? (currentGeometryInstanceMaterialIndex) : -1),
                        WorldMatrix = meshInstance.WorldMatrix,
                        WorldBoundingBox = worldBoundingBox
                    };

                    geometryInstanceList[this.currentGeometryInstanceIndex++] = shaderGeometryInstance;
                }
            }

            // Copy buffers
            var counters = this.graphicsManager.ReadGraphicsBufferData<uint>(this.indirectCommandBufferCounters);
            var opaqueCounterIndex = this.cameraList[0].OpaqueCommandListIndex;
            var transparentCounterIndex = this.cameraList[0].TransparentCommandListIndex;

            if (counters[opaqueCounterIndex] > 0)
            {
                this.graphicsManager.CulledGeometryInstancesCount = (int)counters[opaqueCounterIndex];
            }

            if (this.previousCopyGpuDataIds.Count > 0)
            {
                var previousId = this.previousCopyGpuDataIds.Peek();
                var timing = this.graphicsManager.graphicsService.GetGpuExecutionTime(previousId);

                if (timing > 0)
                {
                    this.graphicsManager.GpuCopyTiming = timing;
                    this.previousCopyGpuDataIds.Dequeue();
                }
            }

            var copyCommandList = this.graphicsManager.CreateCopyCommandList("SceneComputeCopyCommandList");
            this.previousCopyGpuDataIds.Enqueue(copyCommandList.Id);

            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cameraList.AsSpan().Slice(0, this.currentCameraIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, new ShaderSceneProperties[] { sceneProperties });
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.lightList.AsSpan().Slice(0, this.currentLightIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderMaterial>(copyCommandList, this.materialsBuffer, this.materialList.AsSpan().Slice(0, this.currentMaterialIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.geometryPacketList.AsSpan().Slice(0, this.currentGeometryPacketIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, geometryInstanceList.AsSpan().Slice(0, this.currentGeometryInstanceIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indirectCommandBufferCounters, new uint[100].AsSpan());

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            this.graphicsManager.GeometryInstancesCount = this.currentGeometryInstanceIndex;
            this.graphicsManager.MaterialsCount = this.currentMaterialIndex;
            this.graphicsManager.TexturesCount = this.currentTextureIndex;

            return copyCommandList;
        }

        private Queue<uint> previousCopyGpuDataIds = new Queue<uint>();

        private CommandList GenerateIndirectCommands(uint cameraCount, CommandList commandListToWait)
        {
            // Encore indirect command lists
            var computeCommandList = this.graphicsManager.CreateComputeCommandList("GenerateIndirectCommands");
            
            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);
            
            this.graphicsManager.SetShader(computeCommandList, this.drawMeshInstancesComputeShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.scenePropertiesBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.lightsBuffer, 2);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.materialsBuffer, 3);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryPacketsBuffer, 4);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryInstancesBuffer, 5);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.graphicsBufferList.AsSpan().Slice(0, this.currentGraphicsBufferIndex), 6);
            this.graphicsManager.SetShaderTextures(computeCommandList, this.textureList.AsSpan().Slice(0, this.currentTextureIndex), 10006);
            this.graphicsManager.SetShaderTextures(computeCommandList, this.cubeTextureList.AsSpan().Slice(0, this.currentCubeTextureIndex), 20006);
            this.graphicsManager.SetShaderIndirectCommandBuffers(computeCommandList, this.indirectCommandBufferList.AsSpan().Slice(0, this.currentIndirectCommandBufferIndex), 30006);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.indirectCommandBufferCounters, 30106);

            this.graphicsManager.DispatchThreads(computeCommandList, (uint)this.currentGeometryInstanceIndex, cameraCount, 1);
            this.graphicsManager.ExecuteComputeCommandList(computeCommandList);

            // Optimize indirect command lists pass
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("ComputeOptimizeRenderCommandList");

            this.graphicsManager.WaitForCommandList(copyCommandList, computeCommandList);

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.OptimizeIndirectCommandBuffer(copyCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, this.indirectCommandBufferCounters, 4 * Marshal.SizeOf<uint>());
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            return copyCommandList;
        }

        private CommandList GenerateDepthBuffer(ShaderCamera camera, CommandList commandListToWait, bool occlusionDepth = false)
        {
            var depthRenderPassDescriptor = new RenderPassDescriptor(null, !occlusionDepth ? this.textureList[camera.DepthBufferTextureIndex] : this.textureList[camera.OcclusionDepthTextureIndex], DepthBufferOperation.ClearWrite, true);
            var opaqueCommandList = this.graphicsManager.CreateRenderCommandList(depthRenderPassDescriptor, "GenerateDepthBuffer_Opaque");

            this.graphicsManager.WaitForCommandList(opaqueCommandList, commandListToWait);

            this.graphicsManager.SetShader(opaqueCommandList, this.renderMeshInstancesDepthShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(opaqueCommandList, !occlusionDepth ? this.indirectCommandBufferList[camera.OpaqueDepthCommandListIndex] : this.indirectCommandBufferList[camera.OcclusionDepthCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(opaqueCommandList);

            if (occlusionDepth)
            {
                return opaqueCommandList;
            }

            depthRenderPassDescriptor = new RenderPassDescriptor(null, this.textureList[camera.DepthBufferTextureIndex], DepthBufferOperation.Write, false);
            var transparentCommandList = this.graphicsManager.CreateRenderCommandList(depthRenderPassDescriptor, "GenerateDepthBuffer_Transparent");
            this.graphicsManager.WaitForCommandList(transparentCommandList, opaqueCommandList);

            this.graphicsManager.SetShader(transparentCommandList, this.renderMeshInstancesTransparentDepthShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(transparentCommandList, this.indirectCommandBufferList[camera.TransparentDepthCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(transparentCommandList);

            return transparentCommandList;
        }

        private CommandList ConvertToMomentShadowMap(ShaderCamera camera, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(this.textureList[camera.MomentShadowMapIndex], null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "ConvertToMomentShadowMap");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.convertToMomentShadowMapShader);
            this.graphicsManager.SetShaderTexture(commandList, this.textureList[camera.DepthBufferTextureIndex], 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.ExecuteRenderCommandList(commandList);

            return commandList;
        }

        private CommandList GaussianBlurShadowMap(Texture inputTexture, Texture outputTexture, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(outputTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "GaussianBlur");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.gaussianBlurHorizontalShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);
            this.graphicsManager.ExecuteRenderCommandList(commandList);

            renderTarget = new RenderTargetDescriptor(outputTexture, null, BlendOperation.None);
            hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            commandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "GaussianBlur");

            this.graphicsManager.WaitForCommandList(commandList, commandList);


            this.graphicsManager.SetShader(commandList, this.gaussianBlurVerticalShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.ExecuteRenderCommandList(commandList);

            return commandList;
        }

        private CommandList BloomPass(Texture inputTexture, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(this.bloomRenderTarget, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "BloomPass");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.bloomPassShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.ExecuteRenderCommandList(commandList);

            return commandList;
        }

        private CommandList ToneMapPass(Texture inputTexture, Texture bloomTexture, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(this.graphicsManager.MainRenderTargetTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "ToneMap");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.toneMapShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);
            this.graphicsManager.SetShaderTexture(commandList, bloomTexture, 1);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.ExecuteRenderCommandList(commandList);

            return commandList;
        }

        private CommandList ComputeMinMaxDepth(CommandList commandListToWait)
        {
            var computeCommandList = this.graphicsManager.CreateComputeCommandList("ComputeMinMaxDepth");
            var depthBuffer = this.textureList[0];
            
            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);
            
            this.graphicsManager.SetShader(computeCommandList, this.computeMinMaxDepthInitialShader);
            this.graphicsManager.SetShaderTexture(computeCommandList, depthBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.minMaxDepthComputeBuffer, 2);
            
            var threadGroupSize = this.graphicsManager.DispatchThreads(computeCommandList, (uint)depthBuffer.Width, (uint)depthBuffer.Height, 1);

            this.graphicsManager.ExecuteComputeCommandList(computeCommandList);

            // Logger.WriteMessage("==============================");
            var itemCountToProcessWidth = (uint)depthBuffer.Width;
            var itemCountToProcessHeight = (uint)depthBuffer.Height;
            var itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));
            
            var previousCommandList = computeCommandList;

            while (itemCountToProcess > 1)
            {
                itemCountToProcessWidth = itemCountToProcess;
                itemCountToProcessHeight = 1;

                var computeCommandStepList = this.graphicsManager.CreateComputeCommandList("ComputeMinMaxDepthStep");

                this.graphicsManager.WaitForCommandList(computeCommandStepList, previousCommandList);

                // Logger.WriteMessage($"Items to process: {itemCountToProcess} {itemCountToProcessWidth} {itemCountToProcessHeight}");

                // TODO: Use a indirect command buffer to dispatch the correct number of threads
                this.graphicsManager.SetShader(computeCommandStepList, this.computeMinMaxDepthStepShader);
                this.graphicsManager.SetShaderBuffer(computeCommandStepList, this.camerasBuffer, 1);
                this.graphicsManager.SetShaderBuffer(computeCommandStepList, this.minMaxDepthComputeBuffer, 2);
                threadGroupSize = this.graphicsManager.DispatchThreads(computeCommandStepList, itemCountToProcess, 1, 1);

                this.graphicsManager.ExecuteComputeCommandList(computeCommandStepList);

                previousCommandList = computeCommandStepList;

                itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));
                // Logger.WriteMessage($"Items to process: {itemCountToProcess}");
            }

            return previousCommandList;
        }

        private CommandList ComputeLightCameras(CommandList commandListToWait)
        {
            var computeCommandList = this.graphicsManager.CreateComputeCommandList("ComputeLightCameras");
            
            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);
            
            this.graphicsManager.SetShader(computeCommandList, this.computeLightCamerasShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.lightsBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);

            this.graphicsManager.DispatchThreads(computeCommandList, 1, 4, 1);
            this.graphicsManager.ExecuteComputeCommandList(computeCommandList);

            return computeCommandList;
        }

        private CommandList RunRenderPipeline(GraphicsScene scene, CommandList commandListToWait)
        {
            // Optimize indirect command lists pass
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("ResetIndirectCommandBuffers");

            this.graphicsManager.WaitForCommandList(copyCommandList, commandListToWait);

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.ResetIndirectCommandBuffer(copyCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            // TODO: A lot of CPU time is spend to set shader resources

            // Generate Main Camera Depth Buffer
            var commandList = GenerateIndirectCommands(1, copyCommandList);
            var mainCamera = this.cameraList[0];

            commandList = GenerateDepthBuffer(mainCamera, commandList, true);
            commandList = GenerateDepthBuffer(mainCamera, commandList);

            commandList = ComputeMinMaxDepth(commandList);
            commandList = ComputeLightCameras(commandList);

            // Generate Lights Depth Buffers
            commandList = GenerateIndirectCommands((uint)this.currentCameraIndex, commandList);
            var depthCommandLists = new CommandList[this.currentCameraIndex - 1];

            for (var i = 1; i < this.currentCameraIndex; i++)
            {
                var camera = this.cameraList[i];
                depthCommandLists[i - 1] = GenerateDepthBuffer(camera, commandList);
                depthCommandLists[i - 1] = ConvertToMomentShadowMap(camera, depthCommandLists[i - 1]);
                //depthCommandLists[i - 1] = GaussianBlurShadowMap(camera, depthCommandLists[i - 1]);
            }

            // var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(0.0f, 0.215f, 1.0f, 1), BlendOperation.None);
            var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(65 * 5, 135 * 5, 255 * 5, 1.0f) / 255.0f, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget1, this.depthBufferTexture, DepthBufferOperation.CompareEqual, true);
            var renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "MainRenderCommandList");

            this.graphicsManager.WaitForCommandLists(renderCommandList, depthCommandLists);

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(renderCommandList, this.indirectCommandBufferList[mainCamera.OpaqueCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            // Transparent Render pass
            var renderTarget2 = new RenderTargetDescriptor(this.transparentHdrRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneOne);
            var renderTarget3 = new RenderTargetDescriptor(this.transparentRevealageRenderTarget, new Vector4(1.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneMinusSourceColor);
            renderPassDescriptor = new RenderPassDescriptor(renderTarget2, renderTarget3, this.depthBufferTexture, DepthBufferOperation.CompareLess, false);
            var transparentRenderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "TransparentRenderCommandList");

            this.graphicsManager.WaitForCommandLists(transparentRenderCommandList, depthCommandLists);

            this.graphicsManager.SetShader(transparentRenderCommandList, this.renderMeshInstancesTransparentShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(transparentRenderCommandList, this.indirectCommandBufferList[mainCamera.TransparentCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(transparentRenderCommandList);

            // Debug pass
            // for (var i = 0; i < scene.Cameras.Count; i++)
            // {
            //     this.debugRenderer.DrawBoundingFrustum(scene.Cameras[i].BoundingFrustum, new Vector3(0, 0, 1));
            // }

            // //DrawGeometryInstancesBoundingBox(scene);

            // var renderTarget = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, null, BlendOperation.None);
            // var debugRenderPassDescriptor = new RenderPassDescriptor(renderTarget, this.depthBufferTexture, DepthBufferOperation.CompareLess, true);
            // var debugRenderCommandList = this.graphicsManager.CreateRenderCommandList(debugRenderPassDescriptor, "DebugRenderCommandList");

            // this.graphicsManager.WaitForCommandList(debugRenderCommandList, renderCommandList);
            // this.graphicsManager.WaitForCommandList(debugRenderCommandList, transparentRenderCommandList);

            // this.debugRenderer.Render(this.renderPassParametersGraphicsBuffer, debugRenderCommandList);

            // this.graphicsManager.ExecuteRenderCommandList(debugRenderCommandList);


            // Hdr Transfer pass
            var renderTarget = new RenderTargetDescriptor(this.graphicsManager.MainRenderTargetTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var hdrTransferRenderCommandList = this.graphicsManager.CreateRenderCommandList(hdrTransferRenderPassDescriptor, "HdrTransfer");

            this.graphicsManager.WaitForCommandList(hdrTransferRenderCommandList, renderCommandList);
            this.graphicsManager.WaitForCommandList(hdrTransferRenderCommandList, transparentRenderCommandList);
            
            this.graphicsManager.SetShader(hdrTransferRenderCommandList, this.computeHdrTransferShader);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.opaqueHdrRenderTarget, 0);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.transparentHdrRenderTarget, 1);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.transparentRevealageRenderTarget, 2);

            this.graphicsManager.DrawPrimitives(hdrTransferRenderCommandList, GeometryPrimitiveType.TriangleStrip, 0, 4);
            this.graphicsManager.ExecuteRenderCommandList(hdrTransferRenderCommandList);

            // Bloom Pass
            // commandList = BloomPass(this.graphicsManager.MainRenderTargetTexture, hdrTransferRenderCommandList);
            // commandList = GaussianBlurShadowMap(this.bloomRenderTarget, this.bloomRenderTarget, commandList);
            commandList = ToneMapPass(this.graphicsManager.MainRenderTargetTexture, this.bloomRenderTarget, commandList);

            var debugXOffset = this.graphicsManager.GetRenderSize().X - 256;

            this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 0), new Vector2(debugXOffset + 256, 256), this.shadowMaps[1], true);
            this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 256), new Vector2(debugXOffset + 256, 512), this.shadowMaps[3], true);
            this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 512), new Vector2(debugXOffset + 256, 768), this.shadowMaps[5], true);
            this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(debugXOffset, 768), new Vector2(debugXOffset + 256, 1024), this.shadowMaps[7], true);
            //this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(0, 0), new Vector2(this.graphicsManager.GetRenderSize().X, this.graphicsManager.GetRenderSize().Y), this.occlusionDepthTexture, true);

            return hdrTransferRenderCommandList;
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
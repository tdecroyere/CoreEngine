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

        private CommandBuffer copyCommandBuffer;
        private CommandBuffer resetIcbCommandBuffer;
        private CommandBuffer generateIndirectCommandsCommandBuffer;
        private CommandBuffer generateIndirectCommandsCommandBuffer2;
        private CommandBuffer[] generateDepthBufferCommandBuffers;
        private CommandBuffer[] convertToMomentShadowMapCommandBuffers;
        private CommandBuffer gaussianBlurShadowMapCommandBuffer;
        private CommandBuffer bloomPassCommandBuffer;
        private CommandBuffer toneMapCommandBuffer;
        private CommandBuffer computeMinMaxDepthCommandBuffer;
        private CommandBuffer computeLightsCamerasCommandBuffer;
        private CommandBuffer renderGeometryOpaqueCommandBuffer;
        private CommandBuffer resolveCommandBuffer;
        private CommandBuffer renderGeometryTransparentCommandBuffer;

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
            this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererOpaqueHdrRenderTarget");
            this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererTransparentHdrRenderTarget");
            this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererTransparentRevealageRenderTarget");
            this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererDepthBuffer");
            this.occlusionDepthTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, 1, true, GraphicsResourceType.Dynamic, "OcclusionDepthTexture");
            this.bloomRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, 1, true, GraphicsResourceType.Dynamic, "BloomTexture");

            this.minMaxDepthComputeBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector2>(10000, GraphicsResourceType.Dynamic, true, "ComputeMinMaxDepthWorkingBuffer");

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(1, GraphicsResourceType.Dynamic, true, "RenderPassConstantBufferOld");

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

            // Command Buffers
            this.copyCommandBuffer = this.graphicsManager.CreateCommandBuffer("CopySceneDataToGpu");
            this.resetIcbCommandBuffer = this.graphicsManager.CreateCommandBuffer("ResetIndirectCommandBuffers");
            this.generateIndirectCommandsCommandBuffer = this.graphicsManager.CreateCommandBuffer("GenerateIndirectCommands");
            this.generateIndirectCommandsCommandBuffer2 = this.graphicsManager.CreateCommandBuffer("GenerateIndirectCommands");

            this.generateDepthBufferCommandBuffers = new CommandBuffer[5];
            this.convertToMomentShadowMapCommandBuffers = new CommandBuffer[5];

            for (var i = 0; i < 5; i++)
            {
                this.generateDepthBufferCommandBuffers[i] = this.graphicsManager.CreateCommandBuffer("GenerateDepthBuffer");
                this.convertToMomentShadowMapCommandBuffers[i] = this.graphicsManager.CreateCommandBuffer("ConvertToMomentShadowMap");
            }
            
            this.gaussianBlurShadowMapCommandBuffer = this.graphicsManager.CreateCommandBuffer("GaussianBlurShadowMap");
            this.bloomPassCommandBuffer = this.graphicsManager.CreateCommandBuffer("BloomPass");
            this.toneMapCommandBuffer = this.graphicsManager.CreateCommandBuffer("ToneMap");
            this.computeMinMaxDepthCommandBuffer = this.graphicsManager.CreateCommandBuffer("ComputeMinMaxDepth");
            this.computeLightsCamerasCommandBuffer = this.graphicsManager.CreateCommandBuffer("ComputeLightsCameras");
            this.renderGeometryOpaqueCommandBuffer = this.graphicsManager.CreateCommandBuffer("RenderGeometryOpaque");
            this.resolveCommandBuffer = this.graphicsManager.CreateCommandBuffer("ResolveRenderTargets");
            this.renderGeometryTransparentCommandBuffer = this.graphicsManager.CreateCommandBuffer("RenderGeometryTransparent");
        }

        public CommandList Render()
        {
            var frameSize = this.graphicsManager.GetRenderSize();

            if (frameSize != this.currentFrameSize)
            {
                Logger.WriteMessage("Recreating Scene Renderer Render Targets");
                this.currentFrameSize = frameSize;

                this.graphicsManager.DeleteTexture(this.opaqueHdrRenderTarget);
                this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererOpaqueHdrRenderTarget");

                this.graphicsManager.DeleteTexture(this.transparentHdrRenderTarget);
                this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererTransparentHdrRenderTarget");

                this.graphicsManager.DeleteTexture(this.transparentRevealageRenderTarget);
                this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererTransparentHdrRenderTarget");

                this.graphicsManager.DeleteTexture(this.depthBufferTexture);
                this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Dynamic, "SceneRendererDepthBuffer");
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

            InitializeGpuData(scene);

            return RunRenderPipeline();
        }

        ShaderSceneProperties sceneProperties = new ShaderSceneProperties();

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
                    this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, shadowMapSize, shadowMapSize, 1, 1, 1, true, GraphicsResourceType.Dynamic, "ShadowMapDepth");
                }

                else
                {
                    Logger.WriteMessage("Create Moment Shadow map");
                    this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Unorm, shadowMapSize, shadowMapSize, 1, 1, 1, true, GraphicsResourceType.Dynamic, "ShadowMapMoment");
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

        private void InitializeGpuData(GraphicsScene scene)
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
        }

        private Queue<uint> previousCopyGpuDataIds = new Queue<uint>();

        private CommandList CopyGpuData()
        {
            // Copy buffers
            // var counters = this.graphicsManager.ReadGraphicsBufferData<uint>(this.indirectCommandBufferCounters);
            // var opaqueCounterIndex = this.cameraList[0].OpaqueCommandListIndex;
            // var transparentCounterIndex = this.cameraList[0].TransparentCommandListIndex;

            // if (counters[opaqueCounterIndex] > 0)
            // {
            //     this.graphicsManager.CulledGeometryInstancesCount = (int)counters[opaqueCounterIndex];
            // }

            var copyCommandList = this.graphicsManager.CreateCopyCommandList(copyCommandBuffer, "SceneComputeCopyCommandList");
            this.previousCopyGpuDataIds.Enqueue(copyCommandBuffer.GraphicsResourceId);

            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cameraList.AsSpan().Slice(0, this.currentCameraIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, new ShaderSceneProperties[] { this.sceneProperties });
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.lightList.AsSpan().Slice(0, this.currentLightIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderMaterial>(copyCommandList, this.materialsBuffer, this.materialList.AsSpan().Slice(0, this.currentMaterialIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.geometryPacketList.AsSpan().Slice(0, this.currentGeometryPacketIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, geometryInstanceList.AsSpan().Slice(0, this.currentGeometryInstanceIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indirectCommandBufferCounters, new uint[100].AsSpan());

            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(copyCommandBuffer);

            this.graphicsManager.GeometryInstancesCount = this.currentGeometryInstanceIndex;
            this.graphicsManager.MaterialsCount = this.currentMaterialIndex;
            this.graphicsManager.TexturesCount = this.currentTextureIndex;

            return copyCommandList;
        }
        
        private CommandList ResetIndirectCommandBuffers(CommandList commandListToWait)
        {
            var resetIcbCommandList = this.graphicsManager.CreateCopyCommandList(resetIcbCommandBuffer, "ResetIndirectCommandBuffers");

            this.graphicsManager.WaitForCommandList(resetIcbCommandList, commandListToWait);

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.ResetIndirectCommandBuffer(resetIcbCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.CommitCopyCommandList(resetIcbCommandList);
            this.graphicsManager.ExecuteCommandBuffer(resetIcbCommandBuffer);

            return resetIcbCommandList;
        }

        private CommandList GenerateIndirectCommands(uint cameraCount, CommandList commandListToWait)
        {
            var commandBuffer = (cameraCount == 1) ? this.generateIndirectCommandsCommandBuffer : this.generateIndirectCommandsCommandBuffer2;

            // Encore indirect command lists
            var computeCommandList = this.graphicsManager.CreateComputeCommandList(commandBuffer, "GenerateIndirectCommands");

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
            this.graphicsManager.CommitComputeCommandList(computeCommandList);

            // Optimize indirect command lists pass
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "ComputeOptimizeRenderCommandList");

            this.graphicsManager.WaitForCommandList(copyCommandList, computeCommandList);

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.OptimizeIndirectCommandBuffer(copyCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, this.indirectCommandBufferCounters, 4 * Marshal.SizeOf<uint>());
            this.graphicsManager.CommitCopyCommandList(copyCommandList);

            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);

            return copyCommandList;
        }

        int currentDepthCommandBuffer = 0;

        private CommandList GenerateDepthBuffer(ShaderCamera camera, CommandList commandListToWait, bool occlusionDepth = false)
        {
            var depthRenderPassDescriptor = new RenderPassDescriptor(null, !occlusionDepth ? this.textureList[camera.DepthBufferTextureIndex] : this.textureList[camera.OcclusionDepthTextureIndex], DepthBufferOperation.ClearWrite, true);
            var opaqueCommandList = this.graphicsManager.CreateRenderCommandList(generateDepthBufferCommandBuffers[currentDepthCommandBuffer], depthRenderPassDescriptor, "GenerateDepthBuffer_Opaque");

            this.graphicsManager.WaitForCommandList(opaqueCommandList, commandListToWait);

            this.graphicsManager.SetShader(opaqueCommandList, this.renderMeshInstancesDepthShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(opaqueCommandList, !occlusionDepth ? this.indirectCommandBufferList[camera.OpaqueDepthCommandListIndex] : this.indirectCommandBufferList[camera.OcclusionDepthCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.CommitRenderCommandList(opaqueCommandList);

            if (occlusionDepth)
            {
                this.graphicsManager.ExecuteCommandBuffer(generateDepthBufferCommandBuffers[currentDepthCommandBuffer]);
                return opaqueCommandList;
            }

            depthRenderPassDescriptor = new RenderPassDescriptor(null, this.textureList[camera.DepthBufferTextureIndex], DepthBufferOperation.Write, false);
            var transparentCommandList = this.graphicsManager.CreateRenderCommandList(generateDepthBufferCommandBuffers[currentDepthCommandBuffer], depthRenderPassDescriptor, "GenerateDepthBuffer_Transparent");
            this.graphicsManager.WaitForCommandList(transparentCommandList, opaqueCommandList);

            this.graphicsManager.SetShader(transparentCommandList, this.renderMeshInstancesTransparentDepthShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(transparentCommandList, this.indirectCommandBufferList[camera.TransparentDepthCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.CommitRenderCommandList(transparentCommandList);

            this.graphicsManager.ExecuteCommandBuffer(generateDepthBufferCommandBuffers[currentDepthCommandBuffer]);
            this.currentDepthCommandBuffer++;
            return transparentCommandList;
        }

        int currentMomentCommandBuffer = 0;

        private CommandList ConvertToMomentShadowMap(ShaderCamera camera, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(this.textureList[camera.MomentShadowMapIndex], null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(convertToMomentShadowMapCommandBuffers[currentMomentCommandBuffer], hdrTransferRenderPassDescriptor, "ConvertToMomentShadowMap");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.convertToMomentShadowMapShader);
            this.graphicsManager.SetShaderTexture(commandList, this.textureList[camera.DepthBufferTextureIndex], 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.CommitRenderCommandList(commandList);
            this.graphicsManager.ExecuteCommandBuffer(convertToMomentShadowMapCommandBuffers[currentMomentCommandBuffer]);

            currentMomentCommandBuffer++;

            return commandList;
        }

        private CommandList GaussianBlurShadowMap(Texture inputTexture, Texture outputTexture, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(outputTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(gaussianBlurShadowMapCommandBuffer, hdrTransferRenderPassDescriptor, "GaussianBlurHorizontal");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.gaussianBlurHorizontalShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);
            this.graphicsManager.CommitRenderCommandList(commandList);

            renderTarget = new RenderTargetDescriptor(outputTexture, null, BlendOperation.None);
            hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            commandList = this.graphicsManager.CreateRenderCommandList(gaussianBlurShadowMapCommandBuffer, hdrTransferRenderPassDescriptor, "GaussianBlurVertical");

            this.graphicsManager.WaitForCommandList(commandList, commandList);

            this.graphicsManager.SetShader(commandList, this.gaussianBlurVerticalShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.CommitRenderCommandList(commandList);
            this.graphicsManager.ExecuteCommandBuffer(gaussianBlurShadowMapCommandBuffer);

            return commandList;
        }

        private CommandList BloomPass(Texture inputTexture, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(this.bloomRenderTarget, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(bloomPassCommandBuffer, hdrTransferRenderPassDescriptor, "BloomPass");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.bloomPassShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.CommitRenderCommandList(commandList);
            this.graphicsManager.ExecuteCommandBuffer(bloomPassCommandBuffer);

            return commandList;
        }

        private CommandList ToneMapPass(Texture inputTexture, Texture bloomTexture, CommandList commandListToWait)
        {
            var renderTarget = new RenderTargetDescriptor(this.graphicsManager.MainRenderTargetTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var commandList = this.graphicsManager.CreateRenderCommandList(toneMapCommandBuffer, hdrTransferRenderPassDescriptor, "ToneMap");

            this.graphicsManager.WaitForCommandList(commandList, commandListToWait);

            this.graphicsManager.SetShader(commandList, this.toneMapShader);
            this.graphicsManager.SetShaderTexture(commandList, inputTexture, 0);
            this.graphicsManager.SetShaderTexture(commandList, bloomTexture, 1);

            this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.TriangleStrip, 0, 4);

            this.graphicsManager.CommitRenderCommandList(commandList);
            this.graphicsManager.ExecuteCommandBuffer(toneMapCommandBuffer);

            return commandList;
        }

        private CommandList ComputeMinMaxDepth(CommandList commandListToWait)
        {
            var computeCommandList = this.graphicsManager.CreateComputeCommandList(computeMinMaxDepthCommandBuffer, "ComputeMinMaxDepthInitial");
            var depthBuffer = this.textureList[0];

            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);

            this.graphicsManager.SetShader(computeCommandList, this.computeMinMaxDepthInitialShader);
            this.graphicsManager.SetShaderTexture(computeCommandList, depthBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.minMaxDepthComputeBuffer, 2);

            var threadGroupSize = this.graphicsManager.DispatchThreads(computeCommandList, (uint)depthBuffer.Width, (uint)depthBuffer.Height, 1);

            this.graphicsManager.CommitComputeCommandList(computeCommandList);

            // Logger.WriteMessage("==============================");
            var itemCountToProcessWidth = (uint)depthBuffer.Width;
            var itemCountToProcessHeight = (uint)depthBuffer.Height;
            var itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));

            var previousCommandList = computeCommandList;

            while (itemCountToProcess > 1)
            {
                itemCountToProcessWidth = itemCountToProcess;
                itemCountToProcessHeight = 1;

                var computeCommandStepList = this.graphicsManager.CreateComputeCommandList(computeMinMaxDepthCommandBuffer, "ComputeMinMaxDepthStep");

                this.graphicsManager.WaitForCommandList(computeCommandStepList, previousCommandList);

                // Logger.WriteMessage($"Items to process: {itemCountToProcess} {itemCountToProcessWidth} {itemCountToProcessHeight}");

                // TODO: Use a indirect command buffer to dispatch the correct number of threads
                this.graphicsManager.SetShader(computeCommandStepList, this.computeMinMaxDepthStepShader);
                this.graphicsManager.SetShaderBuffer(computeCommandStepList, this.camerasBuffer, 1);
                this.graphicsManager.SetShaderBuffer(computeCommandStepList, this.minMaxDepthComputeBuffer, 2);
                threadGroupSize = this.graphicsManager.DispatchThreads(computeCommandStepList, itemCountToProcess, 1, 1);

                this.graphicsManager.CommitComputeCommandList(computeCommandStepList);

                previousCommandList = computeCommandStepList;

                itemCountToProcess = (uint)(MathF.Ceiling((float)itemCountToProcessWidth / threadGroupSize.X) * MathF.Ceiling((float)itemCountToProcessHeight / threadGroupSize.Y));
                // Logger.WriteMessage($"Items to process: {itemCountToProcess}");
            }

            this.graphicsManager.ExecuteCommandBuffer(computeMinMaxDepthCommandBuffer);

            return previousCommandList;
        }

        private CommandList ComputeLightCameras(CommandList commandListToWait)
        {
            var computeCommandList = this.graphicsManager.CreateComputeCommandList(computeLightsCamerasCommandBuffer, "ComputeLightCameras");

            this.graphicsManager.WaitForCommandList(computeCommandList, commandListToWait);

            this.graphicsManager.SetShader(computeCommandList, this.computeLightCamerasShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.lightsBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.camerasBuffer, 1);

            this.graphicsManager.DispatchThreads(computeCommandList, 1, 4, 1);
            this.graphicsManager.CommitComputeCommandList(computeCommandList);

            this.graphicsManager.ExecuteCommandBuffer(computeLightsCamerasCommandBuffer);

            return computeCommandList;
        }

        private CommandList RenderGeometryOpaque(CommandList[] commandListsToWait, ShaderCamera mainCamera)
        {
            // var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(0.0f, 0.215f, 1.0f, 1), BlendOperation.None);
            var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(65 * 5, 135 * 5, 255 * 5, 1.0f) / 255.0f, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget1, this.depthBufferTexture, DepthBufferOperation.CompareEqual, true);
            var renderCommandList = this.graphicsManager.CreateRenderCommandList(renderGeometryOpaqueCommandBuffer, renderPassDescriptor, "MainRenderCommandList");

            this.graphicsManager.WaitForCommandLists(renderCommandList, commandListsToWait);

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(renderCommandList, this.indirectCommandBufferList[mainCamera.OpaqueCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.CommitRenderCommandList(renderCommandList);
            this.graphicsManager.ExecuteCommandBuffer(renderGeometryOpaqueCommandBuffer);

            return renderCommandList;
        }

        private CommandList ResolveRenderTargets(CommandList[] commandListsToWait)
        {
            // Hdr Transfer pass
            var renderTarget = new RenderTargetDescriptor(this.graphicsManager.MainRenderTargetTexture, null, BlendOperation.None);
            var hdrTransferRenderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
            var hdrTransferRenderCommandList = this.graphicsManager.CreateRenderCommandList(resolveCommandBuffer, hdrTransferRenderPassDescriptor, "HdrTransfer");

            this.graphicsManager.WaitForCommandLists(hdrTransferRenderCommandList, commandListsToWait);

            this.graphicsManager.SetShader(hdrTransferRenderCommandList, this.computeHdrTransferShader);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.opaqueHdrRenderTarget, 0);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.transparentHdrRenderTarget, 1);
            this.graphicsManager.SetShaderTexture(hdrTransferRenderCommandList, this.transparentRevealageRenderTarget, 2);

            this.graphicsManager.DrawPrimitives(hdrTransferRenderCommandList, GeometryPrimitiveType.TriangleStrip, 0, 4);
            this.graphicsManager.CommitRenderCommandList(hdrTransferRenderCommandList);
            this.graphicsManager.ExecuteCommandBuffer(resolveCommandBuffer);

            return hdrTransferRenderCommandList;
        }

        private CommandList RenderGeometryTransparent(CommandList[] commandListsToWait, ShaderCamera mainCamera)
        {
            var renderTarget2 = new RenderTargetDescriptor(this.transparentHdrRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneOne);
            var renderTarget3 = new RenderTargetDescriptor(this.transparentRevealageRenderTarget, new Vector4(1.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneMinusSourceColor);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget2, renderTarget3, this.depthBufferTexture, DepthBufferOperation.CompareLess, false);
            var transparentRenderCommandList = this.graphicsManager.CreateRenderCommandList(renderGeometryTransparentCommandBuffer, renderPassDescriptor, "TransparentRenderCommandList");

            this.graphicsManager.WaitForCommandLists(transparentRenderCommandList, commandListsToWait);

            this.graphicsManager.SetShader(transparentRenderCommandList, this.renderMeshInstancesTransparentShader);
            this.graphicsManager.ExecuteIndirectCommandBuffer(transparentRenderCommandList, this.indirectCommandBufferList[mainCamera.TransparentCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.CommitRenderCommandList(transparentRenderCommandList);

            this.graphicsManager.ExecuteCommandBuffer(renderGeometryTransparentCommandBuffer);

            return transparentRenderCommandList;
        }

        private CommandList RunRenderPipeline()
        {
            var mainCamera = this.cameraList[0];
            this.currentDepthCommandBuffer = 0;
            this.currentMomentCommandBuffer = 0;

            var commandList = CopyGpuData();
            commandList = ResetIndirectCommandBuffers(commandList);

            // Generate Main Camera Depth Buffer
            commandList = GenerateIndirectCommands(1, commandList);
            commandList = GenerateDepthBuffer(mainCamera, commandList);

            // Generate Lights Depth Buffers
            commandList = ComputeMinMaxDepth(commandList);
            commandList = ComputeLightCameras(commandList);

            commandList = GenerateIndirectCommands((uint)this.currentCameraIndex, commandList);
            var depthCommandLists = new CommandList[this.currentCameraIndex - 1];

            for (var i = 1; i < this.currentCameraIndex; i++)
            {
                var camera = this.cameraList[i];
                depthCommandLists[i - 1] = GenerateDepthBuffer(camera, commandList);
                depthCommandLists[i - 1] = ConvertToMomentShadowMap(camera, depthCommandLists[i - 1]);
                //depthCommandLists[i - 1] = GaussianBlurShadowMap(camera, depthCommandLists[i - 1]);
            }

            var renderGeometryOpaque = RenderGeometryOpaque(depthCommandLists, mainCamera);
            var renderGeometryTransparent = RenderGeometryTransparent(depthCommandLists, mainCamera);
            
            commandList = ResolveRenderTargets(new CommandList[] { renderGeometryOpaque, renderGeometryTransparent });

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

            return commandList;
        }

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }
    }
}
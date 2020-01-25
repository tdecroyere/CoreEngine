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
        public ShaderBoundingFrustum BoundingFrustum;
        public int OpaqueCommandListIndex;
        public int OpaqueDepthCommandListIndex;
        public int TransparentCommandListIndex;
        public int TransparentDepthCommandListIndex;
        public bool DepthOnly;
        public bool Reserved1;
        public byte Reserved2;
        public byte Reserved3;
        public float Reserved4;
        public float Reserved5;
        public float Reserved6;
    }

    internal struct ShaderLight
    {
        public Vector3 WorldSpacePosition;
        public int Camera1;
        public int Camera2;
        public int Camera3;
        public int Camera4;
        public float Reserved1;
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
        private Shader renderMeshInstancesTransparentDepthShader;
        private Shader computeHdrTransferShader;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants renderPassConstants;

        private Texture opaqueHdrRenderTarget;
        private Texture transparentHdrRenderTarget;
        private Texture transparentRevealageRenderTarget;
        private Texture depthBufferTexture;
        private Vector2 currentFrameSize;

        private Texture cubeMap;
        private Texture irradianceCubeMap;

        private readonly int multisampleCount = 4;

        // Compute shaders data structures
        private GraphicsBuffer scenePropertiesBuffer;
        private GraphicsBuffer camerasBuffer;
        private GraphicsBuffer lightsBuffer;
        private GraphicsBuffer materialsBuffer;
        private GraphicsBuffer geometryPacketsBuffer;
        private GraphicsBuffer geometryInstancesBuffer;
        private GraphicsBuffer indirectCommandBufferCounters;

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
            this.renderMeshInstancesDepthShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceDepth.shader");
            this.renderMeshInstancesShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstance.shader");
            this.renderMeshInstancesTransparentShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceTransparent.shader");
            this.renderMeshInstancesTransparentDepthShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/RenderMeshInstanceTransparentDepth.shader");
            this.computeHdrTransferShader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/ComputeHdrTransfer.shader");

            this.currentFrameSize = this.graphicsManager.GetRenderSize();
            this.opaqueHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererOpaqueHdrRenderTarget");
            this.transparentHdrRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.Rgba16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentHdrRenderTarget");
            this.transparentRevealageRenderTarget = this.graphicsManager.CreateTexture(TextureFormat.R16Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererTransparentRevealageRenderTarget");
            this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, 1, 1, this.multisampleCount, true, GraphicsResourceType.Static, "SceneRendererDepthBuffer");

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

        public void Render()
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

            CopyComputeGpuData(scene);
            CopyGpuData();
            RunRenderPipeline(scene);
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

        CommandList[] indirectCommandBufferList = new CommandList[100];
        int currentIndirectCommandBufferIndex = 0;

        private int AddIndirectCommandBuffer()
        {
            if (this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex].Id == 0)
            {
                this.indirectCommandBufferList[this.currentIndirectCommandBufferIndex] = this.graphicsManager.CreateIndirectCommandList(65536, "ComputeIndirectLightCommandList");
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

        private int AddShadowMap(int shadowMapSize)
        {
            // TODO: Use resource aliasing
            if (this.shadowMaps[this.currentShadowMapIndex] == null)
            {
                Logger.WriteMessage("Create Shadow map");
                this.shadowMaps[this.currentShadowMapIndex] = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, shadowMapSize, shadowMapSize, 1, 1, 1, true, GraphicsResourceType.Static, "SceneRendererLightShadowBuffer");
            }

            return this.currentShadowMapIndex++;
        }

        private int AddCamera(ShaderCamera camera, Texture depthTexture)
        {
            this.cameraList[this.currentCameraIndex] = camera;
            this.cameraList[this.currentCameraIndex].DepthBufferTextureIndex = AddTexture(depthTexture);

            this.cameraList[this.currentCameraIndex].OpaqueDepthCommandListIndex = AddIndirectCommandBuffer();
            this.cameraList[this.currentCameraIndex].TransparentDepthCommandListIndex = AddIndirectCommandBuffer();
            
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

        private void CopyComputeGpuData(GraphicsScene scene)
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
                        BoundingFrustum = new ShaderBoundingFrustum(scene.ActiveCamera.BoundingFrustum)
                    };

                    sceneProperties.DebugCameraIndex = AddCamera(shaderCamera, this.depthBufferTexture);
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
                        BoundingFrustum = new ShaderBoundingFrustum(camera.BoundingFrustum)
                    };

                    sceneProperties.ActiveCameraIndex = AddCamera(shaderCamera, this.depthBufferTexture);
                }
            }

            sceneProperties.IsDebugCameraActive = (scene.DebugCamera != null);

            // TEST LIGHT BUFFER
            AddCubeTexture(this.cubeMap);
            AddCubeTexture(this.irradianceCubeMap);

            //var lightDirection = Vector3.Normalize(new Vector3(-0.5f, 1.0f, 0.5f));
            // var lightHeight = 30.0f;
            
            // var lightDirection = Vector3.Normalize(new Vector3(-0.1f, 1.0f, 0.1f));
            var lightDirection = Vector3.Normalize(-new Vector3(0.172f, -0.818f, -0.549f));

            var lightHeight = 25.0f;
            
            var shadowMapSize = 1024;
            // var shadowMapSize = 2048;
            var cascadeCount = 4;

            var cascadeRanges = new Vector2[]
            {
                new Vector2(0.1f, 5.0f),
                new Vector2(0.1f, 10.0f),
                new Vector2(0.1f, 25.0f),
                new Vector2(0.1f, 50.0f)
            };
            
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

                var cascadeRange = cascadeRanges[i];

                var lightCamera = ComputeDirectionalLightCamera(scene.ActiveCamera!, lightDirection, shadowMapSize, lightHeight, cascadeRange.X, cascadeRange.Y);
                var shadowMapIndex = AddShadowMap(shadowMapSize);

                lightCameraIndex = AddCamera(lightCamera, this.shadowMaps[shadowMapIndex]);

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
            // TODO: This feature is still buggy and doesn't work every frames
            var counters = this.graphicsManager.ReadGraphicsBufferData<uint>(this.indirectCommandBufferCounters);
            
            if (counters[2] > 0)
            {
                this.graphicsManager.CulledGeometryInstancesCount = (int)counters[2];
            }

            var copyCommandList = this.graphicsManager.CreateCopyCommandList("SceneComputeCopyCommandList");

            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, new ShaderSceneProperties[] { sceneProperties });
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderCamera>(copyCommandList, this.camerasBuffer, this.cameraList.AsSpan().Slice(0, this.currentCameraIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderLight>(copyCommandList, this.lightsBuffer, this.lightList.AsSpan().Slice(0, this.currentLightIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderMaterial>(copyCommandList, this.materialsBuffer, this.materialList.AsSpan().Slice(0, this.currentMaterialIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, this.geometryPacketList.AsSpan().Slice(0, this.currentGeometryPacketIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, geometryInstanceList.AsSpan().Slice(0, this.currentGeometryInstanceIndex));
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indirectCommandBufferCounters, new uint[100].AsSpan());

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.ResetIndirectCommandList(copyCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
            this.graphicsManager.GeometryInstancesCount = this.currentGeometryInstanceIndex;
            this.graphicsManager.MaterialsCount = this.currentMaterialIndex;
            this.graphicsManager.TexturesCount = this.currentTextureIndex;
        }

        private ShaderCamera ComputeDirectionalLightCamera(Camera camera, Vector3 lightDirection, float shadowMapSize, float maxHeight, float nearPlaneDistance, float farPlaneDistance)
        {
            var lightCamera = new ShaderCamera();
            lightCamera.DepthOnly = true;

            var cameraViewProjMatrix = camera.ViewMatrix * MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(54.43f), this.graphicsManager.GetRenderSize().X / this.graphicsManager.GetRenderSize().Y, nearPlaneDistance, farPlaneDistance);

            var cameraFrustumCenter = new BoundingFrustum(cameraViewProjMatrix).GetCenterPoint();
            var cameraFrustumDepthDistance = farPlaneDistance - nearPlaneDistance;

            var lightPosition = new Vector3(0.0f, maxHeight * 10, 0.0f);
            var lightTarget = lightPosition - lightDirection;

            // var lightPosition = new Vector3(0, maxHeight, 0) + lightDirection;
            // var lightTarget = Vector3.Zero;

            lightCamera.WorldPosition = lightDirection;
            lightCamera.ViewMatrix = MathUtils.CreateLookAtMatrix(lightPosition, lightTarget, new Vector3(0, 1, 0));

            var cameraBoundingFrustum1 = new BoundingFrustum(cameraViewProjMatrix);

            var minPoint = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var maxPoint = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            var lightSpaceLeftTopNearPoint = Vector3.Transform(cameraBoundingFrustum1.LeftTopNearPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceLeftTopNearPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceLeftTopNearPoint);

            var lightSpaceLeftTopFarPoint = Vector3.Transform(cameraBoundingFrustum1.LeftTopFarPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceLeftTopFarPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceLeftTopFarPoint);

            var lightSpaceLeftBottomNearPoint = Vector3.Transform(cameraBoundingFrustum1.LeftBottomNearPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceLeftBottomNearPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceLeftBottomNearPoint);

            var lightSpaceLeftBottomFarPoint = Vector3.Transform(cameraBoundingFrustum1.LeftBottomFarPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceLeftBottomFarPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceLeftBottomFarPoint);

            var lightSpaceRightTopNearPoint = Vector3.Transform(cameraBoundingFrustum1.RightTopNearPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceRightTopNearPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceRightTopNearPoint);

            var lightSpaceRightTopFarPoint = Vector3.Transform(cameraBoundingFrustum1.RightTopFarPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceRightTopFarPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceRightTopFarPoint);

            var lightSpaceRightBottomNearPoint = Vector3.Transform(cameraBoundingFrustum1.RightBottomNearPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceRightBottomNearPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceRightBottomNearPoint);

            var lightSpaceRightBottomFarPoint = Vector3.Transform(cameraBoundingFrustum1.RightBottomFarPoint, lightCamera.ViewMatrix);
            minPoint = Vector3.Min(minPoint, lightSpaceRightBottomFarPoint);
            maxPoint = Vector3.Max(maxPoint, lightSpaceRightBottomFarPoint);

            lightCamera.ProjectionMatrix = MathUtils.CreateOrthographicMatrixOffCenter(minPoint.X, maxPoint.X, maxPoint.Y, minPoint.Y, 150.0f, maxHeight * 10);

            // Create the rounding matrix, by projecting the world-space origin and determining
            // the fractional offset in texel space
            var shadowMatrix = lightCamera.ViewMatrix * lightCamera.ProjectionMatrix;
            var transformedOrigin = Vector4.Transform(new Vector4(0.0f, 0.0f, 0.0f, 1.0f), shadowMatrix);
            var shadowOrigin = new Vector2(transformedOrigin.X, transformedOrigin.Y) * shadowMapSize / 2.0f;

            var roundedOrigin = new Vector2(MathF.Round(shadowOrigin.X), MathF.Round(shadowOrigin.Y));
            var roundOffset = (roundedOrigin - shadowOrigin) *  2.0f / shadowMapSize;

            lightCamera.ProjectionMatrix.M41 += roundOffset.X;
            lightCamera.ProjectionMatrix.M42 += roundOffset.Y;

            lightCamera.ViewProjectionMatrix = lightCamera.ViewMatrix * lightCamera.ProjectionMatrix;

            var boundingFrustum = new BoundingFrustum(lightCamera.ViewMatrix * lightCamera.ProjectionMatrix);
            lightCamera.BoundingFrustum = new ShaderBoundingFrustum(boundingFrustum);

            return lightCamera;
        }

        private void RunRenderPipeline(GraphicsScene scene)
        {
            // TODO: A lot of CPU time is spend to set shader resources

            // Encore indirect command lists
            var computeCommandList = this.graphicsManager.CreateComputeCommandList("GenerateIndirectCommands");
            
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
            this.graphicsManager.SetShaderIndirectCommandLists(computeCommandList, this.indirectCommandBufferList.AsSpan().Slice(0, this.currentIndirectCommandBufferIndex), 30006);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.indirectCommandBufferCounters, 30106);

            // TODO: Add an indirect command buffer for lights
            this.graphicsManager.DispatchThreads(computeCommandList, (uint)this.currentGeometryInstanceIndex, (uint)this.currentCameraIndex, 1);
            this.graphicsManager.ExecuteComputeCommandList(computeCommandList);

            // Optimize indirect command lists pass
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("ComputeOptimizeRenderCommandList");

            for (var i = 0; i < this.currentIndirectCommandBufferIndex; i++)
            {
                this.graphicsManager.OptimizeIndirectCommandList(copyCommandList, this.indirectCommandBufferList[i], this.currentGeometryInstanceIndex);
            }

            this.graphicsManager.CopyGraphicsBufferDataToCpu(copyCommandList, this.indirectCommandBufferCounters);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            // Light shadow passes
            for (var i = 0; i < this.currentCameraIndex; i++)
            {
                var camera = this.cameraList[i];

                var depthRenderPassDescriptor = new RenderPassDescriptor(null, this.textureList[camera.DepthBufferTextureIndex], DepthBufferOperation.Write, false);
                var depthRenderCommandList = this.graphicsManager.CreateRenderCommandList(depthRenderPassDescriptor, "DepthPrepassCommandList");

                this.graphicsManager.SetShader(depthRenderCommandList, this.renderMeshInstancesDepthShader);
                this.graphicsManager.ExecuteIndirectCommandList(depthRenderCommandList, this.indirectCommandBufferList[camera.OpaqueDepthCommandListIndex], this.currentGeometryInstanceIndex);
                
                this.graphicsManager.SetShader(depthRenderCommandList, this.renderMeshInstancesTransparentDepthShader);
                this.graphicsManager.ExecuteIndirectCommandList(depthRenderCommandList, this.indirectCommandBufferList[camera.TransparentDepthCommandListIndex], this.currentGeometryInstanceIndex);

                this.graphicsManager.ExecuteRenderCommandList(depthRenderCommandList);
            }

            // Render pass
            var mainCamera = this.cameraList[0];

            // var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(0.0f, 0.215f, 1.0f, 1), BlendOperation.None);
            var renderTarget1 = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, new Vector4(65 * 5, 135 * 5, 255 * 5, 1.0f) / 255.0f, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget1, this.depthBufferTexture, DepthBufferOperation.CompareEqual, false);
            var renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "MainRenderCommandList");

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesShader);
            this.graphicsManager.ExecuteIndirectCommandList(renderCommandList, this.indirectCommandBufferList[mainCamera.OpaqueCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            // Transparent Render pass
            var renderTarget2 = new RenderTargetDescriptor(this.transparentHdrRenderTarget, new Vector4(0.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneOne);
            var renderTarget3 = new RenderTargetDescriptor(this.transparentRevealageRenderTarget, new Vector4(1.0f, 0.0f, 0.0f, 0.0f), BlendOperation.AddOneMinusSourceColor);
            renderPassDescriptor = new RenderPassDescriptor(renderTarget2, renderTarget3, this.depthBufferTexture, DepthBufferOperation.CompareLess, false);
            renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "TransparentRenderCommandList");

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesTransparentShader);
            this.graphicsManager.ExecuteIndirectCommandList(renderCommandList, this.indirectCommandBufferList[mainCamera.TransparentCommandListIndex], this.currentGeometryInstanceIndex);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            // Debug pass
            for (var i = 0; i < scene.Cameras.Count; i++)
            {
                this.debugRenderer.DrawBoundingFrustum(scene.Cameras[i].BoundingFrustum, new Vector3(0, 0, 1));
            }

            //DrawGeometryInstancesBoundingBox(scene);

            this.debugRenderer.CopyDataToGpu();

            var renderTarget = new RenderTargetDescriptor(this.opaqueHdrRenderTarget, null, BlendOperation.None);
            var debugRenderPassDescriptor = new RenderPassDescriptor(renderTarget, this.depthBufferTexture, DepthBufferOperation.CompareLess, true);
            var debugRenderCommandList = this.graphicsManager.CreateRenderCommandList(debugRenderPassDescriptor, "DebugRenderCommandList");

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

            // this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(0, 0), new Vector2(512, 512), this.shadowMaps[0]);
            // this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(0, 512), new Vector2(512, 1024), this.shadowMaps[1]);
            // this.graphicsManager.Graphics2DRenderer.DrawRectangleSurface(new Vector2(0, 1024), new Vector2(512, 1536), this.shadowMaps[2]);
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
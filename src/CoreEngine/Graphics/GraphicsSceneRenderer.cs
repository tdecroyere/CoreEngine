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
        public uint VertexBufferIndex;
        public uint IndexBufferIndex;
    }

    internal struct ShaderGeometryInstance
    {
        public uint GeometryPacketIndex;
        public int StartIndex;
        public int IndexCount;
        public int Reserved1;
        public Matrix4x4 WorldMatrix;
        public ShaderBoundingBox WorldBoundingBox;
    }

    internal struct ShaderMesh
    {
        public uint GeometryInstanceStartIndex;
        public uint GeometryInstancesCount;
        public int Reserved1, Reserved2;
    }

    internal struct ShaderMeshInstance
    {
        public uint MeshIndex;
        public int Reserved1, Reserved2, Reserved3;
        public Matrix4x4 WorldMatrix;
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer
    {
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private Shader drawMeshInstancesComputeShader;
        private Shader renderMeshInstancesShader;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants renderPassConstants;

        private Texture depthBufferTexture;
        private Vector2 currentFrameSize;

        // Compute shaders data structures
        private GraphicsBuffer scenePropertiesBuffer;
        private GraphicsBuffer geometryPacketsBuffer;
        private GraphicsBuffer geometryInstancesBuffer;
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

            this.drawMeshInstancesComputeShader = resourcesManager.LoadResourceAsync<Shader>("/ComputeDrawMeshInstances.shader", "DrawMeshInstances");
            this.renderMeshInstancesShader = resourcesManager.LoadResourceAsync<Shader>("/RenderMeshInstance.shader");

            this.currentFrameSize = this.graphicsManager.GetRenderSize();
            this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, true, GraphicsResourceType.Dynamic, "SceneRendererDepthBuffer");

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(1, GraphicsResourceType.Dynamic);

            // Compute buffers
            this.scenePropertiesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderSceneProperties>(1, GraphicsResourceType.Dynamic, "ComputeSceneProperties");
            this.geometryPacketsBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryPacket>(1000, GraphicsResourceType.Dynamic, "ComputeGeometryPackets");
            this.geometryInstancesBuffer = this.graphicsManager.CreateGraphicsBuffer<ShaderGeometryInstance>(1000, GraphicsResourceType.Dynamic, "ComputeGeometryInstances");
            this.indirectCommandList = this.graphicsManager.CreateIndirectCommandList(65536, "ComputeIndirectCommandList");
        }

        public void CopyDataToGpuAndRender()
        {
            var frameSize = this.graphicsManager.GetRenderSize();

            if (frameSize != this.currentFrameSize)
            {
                Logger.WriteMessage("Recreating Scene Renderer Depth Buffer");
                this.currentFrameSize = frameSize;
                
                this.graphicsManager.RemoveTexture(this.depthBufferTexture);
                this.depthBufferTexture = this.graphicsManager.CreateTexture(TextureFormat.Depth32Float, (int)this.currentFrameSize.X, (int)this.currentFrameSize.Y, true, GraphicsResourceType.Dynamic, "SceneRendererDepthBuffer");
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

            //CullGeometryInstances(scene, scene.ActiveCamera);

            CopyComputeGpuData(scene);
            CopyGpuData(scene);
            RunRenderPipeline(scene, scene.ActiveCamera);
        }

        List<GraphicsBuffer> vertexBuffersList = new List<GraphicsBuffer>();
        List<GraphicsBuffer> indexBuffersList = new List<GraphicsBuffer>();
        List<ShaderGeometryInstance> geometryInstanceList = new List<ShaderGeometryInstance>();

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
                    ViewMatrix = scene.ActiveCamera.ViewMatrix,
                    ProjectionMatrix = scene.ActiveCamera.ProjectionMatrix,
                    BoundingFrustum = boundingFrutum
                };
            }

            if (scene.DebugCamera != null)
            {
                sceneProperties.DebugCamera = new ShaderCamera()
                {
                    ViewMatrix = scene.DebugCamera.ViewMatrix,
                    ProjectionMatrix = scene.DebugCamera.ProjectionMatrix
                };
            }

            sceneProperties.IsDebugCameraActive = (scene.DebugCamera != null);

            vertexBuffersList = new List<GraphicsBuffer>();
            indexBuffersList = new List<GraphicsBuffer>();
            var geometryPacketList = new List<ShaderGeometryPacket>();

            geometryInstanceList.Clear();

            // TODO: For the moment it only work if the mesh instances list is sorted by meshes!
            for (var i = 0; i < scene.MeshInstances.Count; i++)
            {
                var meshInstance = scene.MeshInstances[i];
                var mesh = meshInstance.Mesh;

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = mesh.GeometryInstances[j];
                    var geometryPacket = geometryInstance.GeometryPacket;

                    if (!vertexBuffersList.Contains(geometryPacket.VertexBuffer))
                    {
                        vertexBuffersList.Add(geometryPacket.VertexBuffer);
                    }

                    if (!indexBuffersList.Contains(geometryPacket.IndexBuffer))
                    {
                        indexBuffersList.Add(geometryPacket.IndexBuffer);
                    }

                    var shaderGeometryPacket = new ShaderGeometryPacket()
                    {
                        VertexBufferIndex = (uint)vertexBuffersList.IndexOf(geometryPacket.VertexBuffer),
                        IndexBufferIndex = (uint)indexBuffersList.IndexOf(geometryPacket.IndexBuffer)
                    };

                    if (!geometryPacketList.Contains(shaderGeometryPacket))
                    {
                        geometryPacketList.Add(shaderGeometryPacket);
                    }

                    var worldBoundingBox = new ShaderBoundingBox()
                    {
                        MinPoint = meshInstance.WorldBoundingBoxList[j].MinPoint,
                        MaxPoint = meshInstance.WorldBoundingBoxList[j].MaxPoint
                    };

                    var shaderGeometryInstance = new ShaderGeometryInstance()
                    {
                        GeometryPacketIndex = (uint)geometryPacketList.IndexOf(shaderGeometryPacket),
                        StartIndex = geometryInstance.StartIndex,
                        IndexCount = geometryInstance.IndexCount,
                        WorldMatrix = meshInstance.WorldMatrix,
                        WorldBoundingBox = worldBoundingBox
                    };

                    geometryInstanceList.Add(shaderGeometryInstance);
                }
            }

            // Copy buffers
            var copyCommandList = this.graphicsManager.CreateCopyCommandList("SceneComputeCopyCommandList", true);

            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderSceneProperties>(copyCommandList, this.scenePropertiesBuffer, new ShaderSceneProperties[] { sceneProperties });
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryPacket>(copyCommandList, this.geometryPacketsBuffer, geometryPacketList.ToArray());
            this.graphicsManager.UploadDataToGraphicsBuffer<ShaderGeometryInstance>(copyCommandList, this.geometryInstancesBuffer, geometryInstanceList.ToArray());
            this.graphicsManager.ResetIndirectCommandList(copyCommandList, this.indirectCommandList, 65536);

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
        }

        private void RunRenderPipeline(GraphicsScene scene, Camera camera)
        {
            var computeCommandList = this.graphicsManager.CreateComputeCommandList("ComputeRenderGeometryInstances", true);
            
            this.graphicsManager.SetShader(computeCommandList, this.drawMeshInstancesComputeShader);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.scenePropertiesBuffer, 0);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryPacketsBuffer, 1);
            this.graphicsManager.SetShaderBuffer(computeCommandList, this.geometryInstancesBuffer, 2);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.vertexBuffersList.ToArray(), 5);
            this.graphicsManager.SetShaderBuffers(computeCommandList, this.indexBuffersList.ToArray(), 1005);
            this.graphicsManager.SetShaderIndirectCommandList(computeCommandList, this.indirectCommandList, 2005);

            this.graphicsManager.DispatchThreadGroups(computeCommandList, (uint)geometryInstanceList.Count, 1, 1);
            this.graphicsManager.ExecuteComputeCommandList(computeCommandList);

            var copyCommandList = this.graphicsManager.CreateCopyCommandList("ComputeOptimizeRenderCommandList", true);
            this.graphicsManager.OptimizeIndirectCommandList(copyCommandList, this.indirectCommandList, 65536);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            var renderPassDescriptor = new RenderPassDescriptor(this.graphicsManager.MainRenderTargetTexture, new Vector4(0.0f, 0.215f, 1.0f, 1), this.depthBufferTexture, true, true, true);
            var renderCommandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "SceneRenderCommandList");

            this.graphicsManager.SetShader(renderCommandList, this.renderMeshInstancesShader);
            this.graphicsManager.ExecuteIndirectCommandList(renderCommandList, this.indirectCommandList, 65536);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);

            var debugRenderPassDescriptor = new RenderPassDescriptor(this.graphicsManager.MainRenderTargetTexture, null, this.depthBufferTexture, true, false, true);
            var debugRenderCommandList = this.graphicsManager.CreateRenderCommandList(debugRenderPassDescriptor, "DebugRenderCommandList");

            this.debugRenderer.ClearDebugLines();
            DrawCameraBoundingFrustum(scene);
            DrawGeometryInstancesBoundingBox(scene);

            this.debugRenderer.Render(this.renderPassParametersGraphicsBuffer, debugRenderCommandList);

            this.graphicsManager.ExecuteRenderCommandList(debugRenderCommandList);
        }

        // private void CullGeometryInstances(GraphicsScene scene, Camera camera)
        // {
        //     this.currentObjectPropertyIndex = 0;
        //     this.objectPropertiesMapping.Clear();
        //     this.meshGeometryInstances.Clear();
        //     this.meshGeometryInstancesParamIdList.Clear();

        //     for (var i = 0; i < scene.MeshInstances.Count; i++)
        //     {
        //         var meshInstance = scene.MeshInstances[i];

        //         this.objectProperties[i] = new ObjectProperties() { WorldMatrix = meshInstance.WorldMatrix };
        //         this.objectPropertiesMapping.Add(meshInstance.Id, i);

        //         for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
        //         {
        //             var geometryInstance = meshInstance.Mesh.GeometryInstances[j];
        //             var worldBoundingBox = meshInstance.WorldBoundingBoxList[j];

        //             if (BoundingIntersectUtils.Intersect(camera.BoundingFrustum, worldBoundingBox))
        //             {
        //                 this.meshGeometryInstances.Add(geometryInstance);
        //                 this.meshGeometryInstancesParamIdList.Add((uint)this.objectPropertiesMapping[meshInstance.Id]);
        //             }
        //         }
        //     }

        //     // Prepare draw parameters
        //     for (var i = 0; i < this.meshGeometryInstances.Count; i++)
        //     {
        //         this.vertexShaderParameters[i] = this.meshGeometryInstancesParamIdList[i];
        //     }
        // }

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }

        private void CopyGpuData(GraphicsScene scene)
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

                for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = meshInstance.Mesh.GeometryInstances[j];
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
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Collections;
using CoreEngine.Diagnostics;
using CoreEngine.HostServices;

namespace CoreEngine.Graphics
{
    public struct ObjectProperties
    {
        public Matrix4x4 WorldMatrix { get; set; }
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer : SystemManager
    {
        private readonly GraphicsManager graphicsManager;
        private readonly GraphicsDebugRenderer debugRenderer;

        // Dissociate this?
        public GraphicsScene CurrentScene { get; }

        private RenderPassConstants renderPassConstants;
        private List<GeometryInstance> meshGeometryInstances;
        private List<uint> meshGeometryInstancesParamIdList;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer vertexShaderParametersGraphicsBuffer;
        private GraphicsBuffer objectPropertiesGraphicsBuffer;
        internal uint currentObjectPropertyIndex = 0;

        private ObjectProperties[] objectProperties;
        private uint[] vertexShaderParameters;

        public GraphicsSceneRenderer(GraphicsManager graphicsManager, GraphicsDebugRenderer debugRenderer)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsManager = graphicsManager;
            this.debugRenderer = debugRenderer;
            this.CurrentScene = new GraphicsScene();

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(RenderPassConstants)));

            this.vertexShaderParameters = new uint[1024];
            this.vertexShaderParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(int)) * 1024);

            this.objectProperties = new ObjectProperties[1024];
            this.objectPropertiesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(Matrix4x4)) * 1024);

            this.meshGeometryInstances = new List<GeometryInstance>();
            this.meshGeometryInstancesParamIdList = new List<uint>();
        }

        public override void PostUpdate()
        {
            this.CurrentScene.CleanItems();

            var camera = this.CurrentScene.DebugCamera;

            if (camera == null)
            {
                camera = this.CurrentScene.ActiveCamera;
            }

            if (camera != null)
            {
                SetupCamera(camera);
            }

            UpdateCameraBoundingFrustrum();
            UpdateMeshWorldBoundingBox();
            UpdateDebugCameraBoundingFrustrum();
            UpdateDebugMeshBoundingBox();

            CopyGpuData();

            this.CurrentScene.ResetItemsStatus();
        }

        private void UpdateCameraBoundingFrustrum()
        {
            for (var i = 0; i < this.CurrentScene.Cameras.Count; i++)
            {
                var camera = this.CurrentScene.Cameras[i];

                if (camera.IsDirty)
                {
                    camera.BoundingFrustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
                }
            }
        }

        private void UpdateDebugCameraBoundingFrustrum()
        {
            for (var i = 0; i < this.CurrentScene.Cameras.Count; i++)
            {
                var camera = this.CurrentScene.Cameras[i];

                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftTopNearPoint, camera.BoundingFrustum.LeftTopFarPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftBottomNearPoint, camera.BoundingFrustum.LeftBottomFarPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.RightTopNearPoint, camera.BoundingFrustum.RightTopFarPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.RightBottomNearPoint, camera.BoundingFrustum.RightBottomFarPoint);

                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftTopNearPoint, camera.BoundingFrustum.RightTopNearPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftBottomNearPoint, camera.BoundingFrustum.RightBottomNearPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftTopNearPoint, camera.BoundingFrustum.LeftBottomNearPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.RightTopNearPoint, camera.BoundingFrustum.RightBottomNearPoint);

                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftTopFarPoint, camera.BoundingFrustum.RightTopFarPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftBottomFarPoint, camera.BoundingFrustum.RightBottomFarPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.LeftTopFarPoint, camera.BoundingFrustum.LeftBottomFarPoint);
                this.debugRenderer.DrawDebugLine(camera.BoundingFrustum.RightTopFarPoint, camera.BoundingFrustum.RightBottomFarPoint);
            }
        }

        private void UpdateMeshWorldBoundingBox()
        {
            for (var i = 0; i < this.CurrentScene.MeshInstances.Count; i++)
            {
                var meshInstance = this.CurrentScene.MeshInstances[i];

                if (meshInstance.IsDirty)
                {
                    meshInstance.WorldBoundingBoxList.Clear();

                    for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
                    {
                        var geometryInstance = meshInstance.Mesh.GeometryInstances[j];

                        var boundingBox = BoundingBox.CreateTransformed(geometryInstance.BoundingBox, meshInstance.WorldMatrix);
                        meshInstance.WorldBoundingBoxList.Add(boundingBox);
                    }
                }
            }
        }

        private void UpdateDebugMeshBoundingBox()
        {
            for (var i = 0; i < this.CurrentScene.MeshInstances.Count; i++)
            {
                var meshInstance = this.CurrentScene.MeshInstances[i];

                for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = meshInstance.Mesh.GeometryInstances[j];
                    var worldBoundingBox = meshInstance.WorldBoundingBoxList[j];

                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint, worldBoundingBox.MinPoint + new Vector3(0, 0, worldBoundingBox.ZSize));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint, worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, 0));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(0, 0, worldBoundingBox.ZSize), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, worldBoundingBox.ZSize));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, 0), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, worldBoundingBox.ZSize));
                    
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, 0), worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, worldBoundingBox.ZSize));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, 0), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, 0));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, worldBoundingBox.ZSize), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, worldBoundingBox.ZSize));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, 0), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, worldBoundingBox.ZSize));
                    
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint, worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, 0));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, 0), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, 0));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, 0, worldBoundingBox.ZSize), worldBoundingBox.MinPoint + new Vector3(worldBoundingBox.XSize, worldBoundingBox.YSize, worldBoundingBox.ZSize));
                    this.debugRenderer.DrawDebugLine(worldBoundingBox.MinPoint + new Vector3(0, 0, worldBoundingBox.ZSize), worldBoundingBox.MinPoint + new Vector3(0, worldBoundingBox.YSize, worldBoundingBox.ZSize));
                }
            }
        }

        public void Render()
        {
            RunRenderPipeline();
        }

        private void RunRenderPipeline()
        {
            var renderCommandList = this.graphicsManager.CreateRenderCommandList();
            DrawGeometryInstances(renderCommandList);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);
        }

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }

        private void CopyGpuData()
        {
            var copyCommandList = this.graphicsManager.CreateCopyCommandList();

            // TODO: Process pending gpu resource loading here
            this.meshGeometryInstances.Clear();
            this.meshGeometryInstancesParamIdList.Clear();

            CopyObjectPropertiesToGpu(copyCommandList, this.CurrentScene.MeshInstances);
            CopyRenderPassConstantsToGpu(copyCommandList, this.renderPassConstants);
            CopyDrawParametersToGpu(copyCommandList);

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
        }

        private void CopyObjectPropertiesToGpu(CommandList commandList, ItemCollection<MeshInstance> meshInstances)
        {
            for (var i = 0; i < meshInstances.Count; i++)
            {
                var meshInstance = meshInstances[i];

                if (meshInstance.IsDirty)
                {
                    this.objectProperties[i] = new ObjectProperties() { WorldMatrix = meshInstance.WorldMatrix };
                }

                var mesh = meshInstance.Mesh;

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = mesh.GeometryInstances[j];
                    this.meshGeometryInstances.Add(geometryInstance);
                    this.meshGeometryInstancesParamIdList.Add(meshInstance.ObjectPropertiesIndex);
                }
            }

            // TODO: Only update partially the buffer?
            this.graphicsManager.UploadDataToGraphicsBuffer<ObjectProperties>(commandList, this.objectPropertiesGraphicsBuffer, this.objectProperties);
        }

        bool shaderParametersCreated = false;
        private void CopyRenderPassConstantsToGpu(CommandList commandList, RenderPassConstants renderPassConstants)
        {
            if (!shaderParametersCreated)
            {
                this.graphicsManager.CreateShaderParameters(this.renderPassParametersGraphicsBuffer, this.objectPropertiesGraphicsBuffer, this.vertexShaderParametersGraphicsBuffer);
                shaderParametersCreated = true;
            }

            // TODO: Switch to configurable render pass constants
            this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants>(commandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants[] {renderPassConstants});
        }

        private void CopyDrawParametersToGpu(CommandList commandList)
        {
            // Prepare draw parameters
            this.vertexShaderParameters.Initialize();

            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                this.vertexShaderParameters[i] = this.meshGeometryInstancesParamIdList[i];
            }

            // TODO: Implement a ring buffer strategy by creating several versions of the same
            // argument buffer per shader?
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(commandList, this.vertexShaderParametersGraphicsBuffer, this.vertexShaderParameters);
        }

        private void DrawGeometryInstances(CommandList commandList)
        {
            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                // TODO: Calculate base instanceid based on the previous batch size

                var geometryInstance = this.meshGeometryInstances[i];
                this.graphicsManager.DrawPrimitives(commandList, geometryInstance.PrimitiveType, geometryInstance.StartIndex, geometryInstance.IndexCount, geometryInstance.GeometryPacket.VertexBuffer, geometryInstance.GeometryPacket.IndexBuffer, (uint)i);
            }
        }
    }
}
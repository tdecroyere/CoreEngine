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

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsSceneRenderer
    {
        private readonly GraphicsManager graphicsManager;
        private readonly DebugRenderer debugRenderer;
        private readonly GraphicsSceneQueue sceneQueue;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer vertexShaderParametersGraphicsBuffer;
        private GraphicsBuffer objectPropertiesGraphicsBuffer;

        private RenderPassConstants renderPassConstants;
        private List<GeometryInstance> meshGeometryInstances;
        private List<uint> meshGeometryInstancesParamIdList;
        private uint[] vertexShaderParameters;

        private Dictionary<ItemIdentifier, int> objectPropertiesMapping;
        private ObjectProperties[] objectProperties;
        internal int currentObjectPropertyIndex = 0;

        public GraphicsSceneRenderer(GraphicsManager graphicsManager, GraphicsSceneQueue sceneQueue, ResourcesManager resourcesManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsManager = graphicsManager;
            this.debugRenderer = new DebugRenderer(graphicsManager, resourcesManager);
            this.sceneQueue = sceneQueue;

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants>(1, GraphicsResourceType.Dynamic);

            this.vertexShaderParameters = new uint[1024];
            this.vertexShaderParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<int>(1024, GraphicsResourceType.Dynamic);

            this.objectPropertiesMapping = new Dictionary<ItemIdentifier, int>();
            this.objectProperties = new ObjectProperties[1024];
            this.objectPropertiesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<Matrix4x4>(1024, GraphicsResourceType.Dynamic);

            this.meshGeometryInstances = new List<GeometryInstance>();
            this.meshGeometryInstancesParamIdList = new List<uint>();
        }

        public void Render()
        {
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

            CopyGpuData(scene);
            RunRenderPipeline(scene);
        }

        private void RunRenderPipeline(GraphicsScene scene)
        {
            var renderCommandList = this.graphicsManager.CreateRenderCommandList();

            this.graphicsManager.SetShader(renderCommandList, this.graphicsManager.testShader);

            if (this.argumentBuffer != null)
            {
                this.graphicsManager.SetGraphicsBuffer(renderCommandList, this.argumentBuffer.Value, GraphicsBindStage.Vertex, 1);
            }

            DrawGeometryInstances(renderCommandList);

            this.debugRenderer.ClearDebugLines();
            DrawCameraBoundingFrustum(scene);
            DrawGeometryInstancesBoundingBox(scene);

            this.graphicsManager.SetGraphicsBuffer(renderCommandList, this.renderPassParametersGraphicsBuffer, GraphicsBindStage.Vertex, 1);
            this.debugRenderer.Render(renderCommandList);
            
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);
        }

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }

        private void CopyGpuData(GraphicsScene scene)
        {
            var copyCommandList = this.graphicsManager.CreateCopyCommandList();

            this.meshGeometryInstances.Clear();
            this.meshGeometryInstancesParamIdList.Clear();

            CopyRenderPassConstantsToGpu(copyCommandList, this.renderPassConstants);
            CopyObjectPropertiesToGpu(copyCommandList, scene.MeshInstances);
            CopyDrawParametersToGpu(copyCommandList);

            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
        }

        GraphicsBuffer? argumentBuffer = null;
        private void CopyRenderPassConstantsToGpu(CommandList commandList, RenderPassConstants renderPassConstants)
        {
            if (argumentBuffer == null)
            {
                argumentBuffer = this.graphicsManager.CreateShaderParameters(this.graphicsManager.testShader, this.renderPassParametersGraphicsBuffer, this.objectPropertiesGraphicsBuffer, this.vertexShaderParametersGraphicsBuffer);
            }

            // TODO: Switch to configurable render pass constants
            this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants>(commandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants[] {renderPassConstants});
        }

        private void CopyObjectPropertiesToGpu(CommandList commandList, ItemCollection<MeshInstance> meshInstances)
        {
            for (var i = 0; i < meshInstances.Count; i++)
            {
                var meshInstance = meshInstances[i];

                if (!this.objectPropertiesMapping.ContainsKey(meshInstance.Id))
                {
                    this.objectPropertiesMapping.Add(meshInstance.Id, this.currentObjectPropertyIndex++);
                }

                if (meshInstance.IsDirty)
                {
                    this.objectProperties[this.objectPropertiesMapping[meshInstance.Id]] = new ObjectProperties() { WorldMatrix = meshInstance.WorldMatrix };
                }

                var mesh = meshInstance.Mesh;

                for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = mesh.GeometryInstances[j];
                    this.meshGeometryInstances.Add(geometryInstance);
                    this.meshGeometryInstancesParamIdList.Add((uint)this.objectPropertiesMapping[meshInstance.Id]);
                }
            }

            // TODO: Only update partially the buffer?
            this.graphicsManager.UploadDataToGraphicsBuffer<ObjectProperties>(commandList, this.objectPropertiesGraphicsBuffer, this.objectProperties);
        }

        private void CopyDrawParametersToGpu(CommandList commandList)
        {
            // Prepare draw parameters
            this.vertexShaderParameters.Initialize();

            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                this.vertexShaderParameters[i] = this.meshGeometryInstancesParamIdList[i];
            }

            // argument buffer per shader?
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(commandList, this.vertexShaderParametersGraphicsBuffer, this.vertexShaderParameters);
        }

        private void DrawGeometryInstances(CommandList commandList)
        {
            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                // TODO: Calculate base instanceid based on the previous batch size

                var geometryInstance = this.meshGeometryInstances[i];
                this.graphicsManager.DrawGeometryInstances(commandList, geometryInstance, 1, i);
            }
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
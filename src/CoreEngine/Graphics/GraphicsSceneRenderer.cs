using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
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
    public class GraphicsSceneRenderer : SystemManager
    {
        private readonly IGraphicsService graphicsService;
        private readonly GraphicsManager graphicsManager;

        private GraphicsScene currentScene;

        private List<Entity> meshInstancesToRemove;
        private RenderPassConstants renderPassConstants;
        private List<GeometryInstance> meshGeometryInstances;
        private List<uint> meshGeometryInstancesParamIdList;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer vertexShaderParametersGraphicsBuffer;
        private GraphicsBuffer objectPropertiesGraphicsBuffer;
        private uint currentObjectPropertyIndex = 0;

        // TODO: Remove the dependency to GraphicsService. Only GraphicsManager should have a dependency to it
        public GraphicsSceneRenderer(IGraphicsService graphicsService, GraphicsManager graphicsManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsService = graphicsService;
            this.graphicsManager = graphicsManager;

            this.currentScene = new GraphicsScene();

            this.meshInstancesToRemove = new List<Entity>();

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(RenderPassConstants)));
            this.vertexShaderParametersGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(int)) * 1024);
            this.objectPropertiesGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(Matrix4x4)) * 256);

            this.meshGeometryInstances = new List<GeometryInstance>();
            this.meshGeometryInstancesParamIdList = new List<uint>();
        }

        public void UpdateScene(Entity? camera)
        {
            if (camera != null)
            {
                this.currentScene.ActiveCamera = this.currentScene.Cameras[camera.Value];
            }

            else
            {
                this.currentScene.ActiveCamera = null;
            }
        }

        // TODO: Remove worldmatrix parameter so we can pass graphics paramters in constant buffers
        public void AddOrUpdateEntity(Entity entity, Mesh mesh, Matrix4x4 worldMatrix)
        {
            if (this.currentScene.MeshInstances.ContainsKey(entity))
            {
                this.currentScene.MeshInstances[entity].Mesh = mesh;
                this.currentScene.MeshInstances[entity].IsAlive = true;

                if (this.currentScene.MeshInstances[entity].WorldMatrix != worldMatrix)
                {
                    this.currentScene.MeshInstances[entity].WorldMatrix = worldMatrix;
                    this.currentScene.MeshInstances[entity].IsDirty = true;
                }

                else
                {
                    this.currentScene.MeshInstances[entity].IsDirty = false;
                }
            }

            else
            {
                this.currentScene.MeshInstances.Add(entity, new MeshInstance(entity, mesh, worldMatrix, this.currentObjectPropertyIndex));
                this.currentObjectPropertyIndex++;
            }
        }

        public void AddOrUpdateCamera(Entity entity, Matrix4x4 viewMatrix)
        {
            var renderSize = this.graphicsService.GetRenderSize();
            var renderWidth = renderSize.X;
            var renderHeight = renderSize.Y;

            var projectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(54.43f), renderWidth / renderHeight, 10.0f, 100000.0f);
            // var projectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(39.375f), renderWidth / renderHeight, 10.0f, 100000.0f);

            Camera camera;

            if (this.currentScene.Cameras.ContainsKey(entity))
            {
                camera = this.currentScene.Cameras[entity];

                camera.IsAlive = true;
                camera.ViewMatrix = viewMatrix;
                camera.ProjectionMatrix = projectionMatrix;
            }

            else
            {
                camera = new Camera(entity, viewMatrix, projectionMatrix);
                this.currentScene.Cameras.Add(entity, camera);
            }
        }
        
        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }

        public override void PostUpdate()
        {
            this.graphicsService.BeginCopyGpuData();
            // TODO: Process pending gpu resource loading here

            if (this.currentScene.ActiveCamera != null)
            {
                SetupCamera(this.currentScene.ActiveCamera);
            }

            SetRenderPassConstants(this.renderPassConstants);
            ProcessActiveMeshInstances();

            // Prepare draw parameters
            var vertexShaderParametersSpan = this.vertexShaderParametersGraphicsBuffer.MemoryBuffer;
            vertexShaderParametersSpan.Clear();

            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                var objectPropertiesIndex = this.meshGeometryInstancesParamIdList[i];
                MemoryMarshal.Write(vertexShaderParametersSpan.Slice(i * 4), ref objectPropertiesIndex);
            }

            // TODO: Implement a ring buffer strategy by creating several versions of the same
            // argument buffer per shader?
            this.graphicsService.UploadDataToGraphicsBuffer(this.vertexShaderParametersGraphicsBuffer.Id, this.vertexShaderParametersGraphicsBuffer.MemoryBuffer);
            this.graphicsService.EndCopyGpuData();

            UpdateMeshInstancesStatus(false);
        }

        private void ProcessActiveMeshInstances()
        {
            this.meshInstancesToRemove.Clear();
            this.meshGeometryInstances.Clear();
            this.meshGeometryInstancesParamIdList.Clear();

            // TODO: Replace that with an hybrid dictionary/list
            foreach(var meshInstance in this.currentScene.MeshInstances.Values)
            {
                if (!meshInstance.IsAlive)
                {
                    this.meshInstancesToRemove.Add(meshInstance.Entity);
                }

                else
                {
                    // TODO: Move that to a proper update function
                    // TODO: IsDirty is not taken into account for the moment
                    var objectProperties = new ObjectProperties() { WorldMatrix = meshInstance.WorldMatrix };
                    var objectPropertiesIndex = meshInstance.ObjectPropertiesIndex;
                    var objectPropertiesOffset = (int)objectPropertiesIndex * Marshal.SizeOf<ObjectProperties>();

                    var objectBufferSpan = this.objectPropertiesGraphicsBuffer.MemoryBuffer.Slice(objectPropertiesOffset);
                    MemoryMarshal.Write(objectBufferSpan, ref objectProperties);

                    var mesh = meshInstance.Mesh;

                    for (var i = 0; i < mesh.GeometryInstances.Count; i++)
                    {
                        var geometryInstance = mesh.GeometryInstances[i];
                        this.meshGeometryInstances.Add(geometryInstance);
                        this.meshGeometryInstancesParamIdList.Add(meshInstance.ObjectPropertiesIndex);
                    }
                }
            }

            // TODO: Only update partially the buffer?
            this.graphicsService.UploadDataToGraphicsBuffer(this.objectPropertiesGraphicsBuffer.Id, this.objectPropertiesGraphicsBuffer.MemoryBuffer);

            for (var i = 0; i < this.meshInstancesToRemove.Count; i++)
            {
                // TODO: Remove GPU data!
                this.currentScene.MeshInstances.Remove(this.meshInstancesToRemove[i]);
            }
        }

        public void Render()
        {
            RunRenderPipeline();
        }

        private void RunRenderPipeline()
        {
            //this.graphicsService.BeginRender();
            DrawGeometryInstances();
            //this.graphicsService.EndRender();
        }

        bool shaderParametersCreated = false;
        public void SetRenderPassConstants(RenderPassConstants renderPassConstants)
        {
            if (!shaderParametersCreated)
            {
                this.graphicsService.CreateShaderParameters(this.renderPassParametersGraphicsBuffer.Id, this.objectPropertiesGraphicsBuffer.Id, this.vertexShaderParametersGraphicsBuffer.Id);
                shaderParametersCreated = true;
            }

            // TODO: Switch to configurable render pass constants
            MemoryMarshal.Write(this.renderPassParametersGraphicsBuffer.MemoryBuffer, ref renderPassConstants);
            this.graphicsService.UploadDataToGraphicsBuffer(this.renderPassParametersGraphicsBuffer.Id, this.renderPassParametersGraphicsBuffer.MemoryBuffer);
        }

        public void DrawGeometryInstances()
        {
            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                // TODO: Calculate base instanceid based on the previous batch size

                var geometryInstance = this.meshGeometryInstances[i];
                this.graphicsService.DrawPrimitives(geometryInstance.StartIndex, geometryInstance.IndexCount, geometryInstance.GeometryPacket.VertexBuffer.Id, geometryInstance.GeometryPacket.IndexBuffer.Id, (uint)i);
            }
        }

        private void UpdateMeshInstancesStatus(bool isAlive)
        {
            // TODO: Replace that with an hybrid dictionary/list
            foreach(var meshInstance in this.currentScene.MeshInstances.Values)
            {
                meshInstance.IsAlive = isAlive;
            }
        }
    }
}
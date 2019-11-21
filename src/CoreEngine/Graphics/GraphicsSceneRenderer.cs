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
    public class GraphicsSceneRenderer : SystemManager
    {
        private readonly IGraphicsService graphicsService;
        private readonly GraphicsManager graphicsManager;

        // Dissociate this?
        public GraphicsScene CurrentScene 
        {
            get;
        }

        private List<ItemIdentifier> meshInstancesToRemove;
        private RenderPassConstants renderPassConstants;
        private List<GeometryInstance> meshGeometryInstances;
        private List<uint> meshGeometryInstancesParamIdList;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer vertexShaderParametersGraphicsBuffer;
        private GraphicsBuffer objectPropertiesGraphicsBuffer;
        internal uint currentObjectPropertyIndex = 0;

        // TODO: Remove the dependency to GraphicsService. Only GraphicsManager should have a dependency to it
        public GraphicsSceneRenderer(IGraphicsService graphicsService, GraphicsManager graphicsManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            this.graphicsService = graphicsService;
            this.graphicsManager = graphicsManager;

            this.CurrentScene = new GraphicsScene();

            this.meshInstancesToRemove = new List<ItemIdentifier>();

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(RenderPassConstants)));
            this.vertexShaderParametersGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(int)) * 1024);
            this.objectPropertiesGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(Matrix4x4)) * 256);

            this.meshGeometryInstances = new List<GeometryInstance>();
            this.meshGeometryInstancesParamIdList = new List<uint>();
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

            if (this.CurrentScene.ActiveCamera != null)
            {
                SetupCamera(this.CurrentScene.ActiveCamera);
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

            for (var i = 0; i < this.CurrentScene.MeshInstances.Count; i++)
            {
                var meshInstance = this.CurrentScene.MeshInstances[i];
                var meshInstanceKey = this.CurrentScene.MeshInstances.Keys[i];

                if (!meshInstance.IsAlive)
                {
                    this.meshInstancesToRemove.Add(meshInstanceKey);
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

                    for (var j = 0; j < mesh.GeometryInstances.Count; j++)
                    {
                        var geometryInstance = mesh.GeometryInstances[j];
                        this.meshGeometryInstances.Add(geometryInstance);
                        this.meshGeometryInstancesParamIdList.Add(meshInstance.ObjectPropertiesIndex);
                    }
                }
            }

            // TODO: Only update partially the buffer?
            this.graphicsService.UploadDataToGraphicsBuffer(this.objectPropertiesGraphicsBuffer.Id, this.objectPropertiesGraphicsBuffer.MemoryBuffer);

            for (var i = 0; i < this.meshInstancesToRemove.Count; i++)
            {
                Logger.WriteMessage("Remove mesh instance");
                // TODO: Remove GPU data!
                this.CurrentScene.MeshInstances.Remove(this.meshInstancesToRemove[i]);
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
            for (var i = 0; i < this.CurrentScene.MeshInstances.Count; i++)
            {
                var meshInstance = this.CurrentScene.MeshInstances[i];
                meshInstance.IsAlive = isAlive;
            }
        }
    }
}
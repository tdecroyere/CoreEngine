using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public struct ObjectProperties
    {
        public Matrix4x4 WorldMatrix { get; set; }
    }

    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsManager : SystemManager
    {
        private readonly GraphicsService graphicsService;
        private readonly MemoryService memoryService;
        private readonly ResourcesManager resourcesManager;
        private Dictionary<Entity, MeshInstance> meshInstances;
        private List<Entity> meshInstancesToRemove;
        private RenderPassConstants renderPassConstants;
        private List<GeometryInstance> meshGeometryInstances;
        private List<uint> meshGeometryInstancesParamIdList;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer vertexShaderParametersGraphicsBuffer;
        private GraphicsBuffer objectPropertiesGraphicsBuffer;
        private uint currentObjectPropertyIndex = 0;

        public GraphicsManager(GraphicsService graphicsService, MemoryService memoryService, ResourcesManager resourcesManager)
        {
            this.graphicsService = graphicsService;
            this.memoryService = memoryService;
            this.resourcesManager = resourcesManager;

            this.meshInstances = new Dictionary<Entity, MeshInstance>();
            this.meshInstancesToRemove = new List<Entity>();

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = CreateDynamicGraphicsBuffer((uint)Marshal.SizeOf(typeof(RenderPassConstants)));
            this.vertexShaderParametersGraphicsBuffer = CreateDynamicGraphicsBuffer((uint)Marshal.SizeOf(typeof(int)) * 1024);
            this.objectPropertiesGraphicsBuffer = CreateDynamicGraphicsBuffer((uint)Marshal.SizeOf(typeof(Matrix4x4)) * 256);

            this.meshGeometryInstances = new List<GeometryInstance>();
            this.meshGeometryInstancesParamIdList = new List<uint>();
            
            InitResourceLoaders();
        }

        internal GraphicsBuffer CreateStaticGraphicsBuffer(ReadOnlySpan<byte> data)
        {
            var dataMemoryBuffer = this.memoryService.CreateMemoryBuffer((uint)data.Length);

            if (!data.TryCopyTo(dataMemoryBuffer.AsSpan()))
            {
                throw new InvalidOperationException("Error while copying graphics buffer data.");
            }

            var graphicsBufferId = graphicsService.CreateStaticGraphicsBuffer(dataMemoryBuffer);
            this.memoryService.DestroyMemoryBuffer(dataMemoryBuffer.Id);

            return new GraphicsBuffer(graphicsBufferId, dataMemoryBuffer.Length);
        }

        internal GraphicsBuffer CreateDynamicGraphicsBuffer(uint length)
        {
            var graphicsBuffer = graphicsService.CreateDynamicGraphicsBuffer(length);
            return new GraphicsBuffer(graphicsBuffer);
        }

        // TODO: Remove worldmatrix parameter so we can pass graphics paramters in constant buffers
        public void AddOrUpdateEntity(Entity entity, Mesh mesh, Matrix4x4 worldMatrix)
        {
            if (this.meshInstances.ContainsKey(entity))
            {
                this.meshInstances[entity].Mesh = mesh;
                this.meshInstances[entity].IsAlive = true;

                if (this.meshInstances[entity].WorldMatrix != worldMatrix)
                {
                    this.meshInstances[entity].WorldMatrix = worldMatrix;
                    this.meshInstances[entity].IsDirty = true;
                }

                else
                {
                    this.meshInstances[entity].IsDirty = false;
                }
            }

            else
            {
                this.meshInstances.Add(entity, new MeshInstance(entity, mesh, worldMatrix, this.currentObjectPropertyIndex));
                this.currentObjectPropertyIndex++;
            }
        }
        
        public void UpdateCamera(Matrix4x4 viewMatrix)
        {
            var renderSize = this.graphicsService.GetRenderSize();
            var renderWidth = renderSize.X;
            var renderHeight = renderSize.Y;

            // TODO: Try to update directly the host memory buffer

            this.renderPassConstants.ViewMatrix = viewMatrix;

            // this.renderPassConstants.ProjectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(39.375f), renderWidth / renderHeight, 10.0f, 100000.0f);
            this.renderPassConstants.ProjectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(54.43f), renderWidth / renderHeight, 10.0f, 100000.0f);
        }

        public override void PostUpdate()
        {
            this.graphicsService.BeginCopyGpuData();
            // TODO: Process pending gpu resource loading here
            SetRenderPassConstants(this.renderPassConstants);
            ProcessActiveMeshInstances();

            // Prepare draw parameters
            var vertexShaderParametersSpan = this.vertexShaderParametersGraphicsBuffer.MemoryBuffer.AsSpan();
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
            foreach(var meshInstance in this.meshInstances.Values)
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

                    var objectBufferSpan = this.objectPropertiesGraphicsBuffer.MemoryBuffer.AsSpan().Slice(objectPropertiesOffset);
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
                this.meshInstances.Remove(this.meshInstancesToRemove[i]);
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
            MemoryMarshal.Write(this.renderPassParametersGraphicsBuffer.MemoryBuffer.AsSpan(), ref renderPassConstants);
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
            foreach(var meshInstance in this.meshInstances.Values)
            {
                meshInstance.IsAlive = isAlive;
            }
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.resourcesManager, this.graphicsService, this.memoryService));
            this.resourcesManager.AddResourceLoader(new MaterialResourceLoader(this.resourcesManager, this, this.memoryService));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.resourcesManager, this, this.memoryService));
        }
    }
}
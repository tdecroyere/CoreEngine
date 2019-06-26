using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
    public class GraphicsManager : SystemManager
    {
        private readonly GraphicsService graphicsService;
        private readonly MemoryService memoryService;
        private readonly ResourcesManager resourcesManager;
        private Dictionary<Entity, MeshInstance> meshInstances;
        private List<Entity> meshInstancesToRemove;
        private RenderPassConstants renderPassConstants;
        private MemoryBuffer renderPassConstantsMemoryBuffer;
        private List<GeometryInstance> meshGeometryInstances;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private MemoryBuffer objectParametersMemoryBuffer;
        private GraphicsBuffer objectParametersGraphicsBuffer;

        public GraphicsManager(GraphicsService graphicsService, MemoryService memoryService, ResourcesManager resourcesManager)
        {
            this.graphicsService = graphicsService;
            this.memoryService = memoryService;
            this.resourcesManager = resourcesManager;

            this.meshInstances = new Dictionary<Entity, MeshInstance>();
            this.meshInstancesToRemove = new List<Entity>();

            this.renderPassConstantsMemoryBuffer = memoryService.CreateMemoryBuffer(Marshal.SizeOf(typeof(RenderPassConstants)));
            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = CreateGraphicsBuffer((ReadOnlySpan<byte>)this.renderPassConstantsMemoryBuffer.AsSpan());

            this.objectParametersMemoryBuffer = memoryService.CreateMemoryBuffer(Marshal.SizeOf(typeof(Matrix4x4)));
            this.objectParametersGraphicsBuffer = CreateGraphicsBuffer((ReadOnlySpan<byte>)this.objectParametersMemoryBuffer.AsSpan());

            this.meshGeometryInstances = new List<GeometryInstance>();
            
            InitResourceLoaders();
        }

        internal GraphicsBuffer CreateGraphicsBuffer(ReadOnlySpan<byte> data)
        {
            var dataMemoryBuffer = this.memoryService.CreateMemoryBuffer(data.Length);

            if (!data.TryCopyTo(dataMemoryBuffer.AsSpan()))
            {
                throw new InvalidOperationException("Error while copying graphics buffer data.");
            }

            var graphicsBufferId = graphicsService.CreateGraphicsBuffer(dataMemoryBuffer);
            this.memoryService.DestroyMemoryBuffer(dataMemoryBuffer.Id);

            return new GraphicsBuffer(graphicsBufferId, dataMemoryBuffer.Length);
        }

        // TODO: Remove worldmatrix parameter so we can pass graphics paramters in constant buffers
        public void AddOrUpdateEntity(Entity entity, Mesh mesh, Matrix4x4 worldMatrix)
        {
            if (this.meshInstances.ContainsKey(entity))
            {
                this.meshInstances[entity].Mesh = mesh;
                this.meshInstances[entity].WorldMatrix = worldMatrix;
                this.meshInstances[entity].IsAlive = true;
            }

            else
            {
                this.meshInstances.Add(entity, new MeshInstance(entity, mesh, worldMatrix));
            }
        }
        
        public void UpdateCamera(in Matrix4x4 viewMatrix)
        {
            var renderSize = this.graphicsService.GetRenderSize();
            var renderWidth = renderSize.X;
            var renderHeight = renderSize.Y;

            this.renderPassConstants.ViewMatrix = viewMatrix;

            // this.renderPassConstants.ProjectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(39.375f), renderWidth / renderHeight, 10.0f, 100000.0f);
            this.renderPassConstants.ProjectionMatrix = MathUtils.CreatePerspectiveFieldOfViewMatrix(MathUtils.DegreesToRad(54.43f), renderWidth / renderHeight, 10.0f, 100000.0f);
        }

        public override void Update()
        {
            ProcessActiveMeshInstances();
            RunRenderPipeline();
            UpdateMeshInstancesStatus(false);
        }

        private void RunRenderPipeline()
        {
            SetRenderPassConstants(this.renderPassConstants);
            DrawGeometryInstances();
        }

        private void ProcessActiveMeshInstances()
        {
            this.meshInstancesToRemove.Clear();
            this.meshGeometryInstances.Clear();

            // TODO: Replace that with an hybrid dictionary/list
            foreach(var meshInstance in this.meshInstances.Values)
            {
                if (!meshInstance.IsAlive)
                {
                    this.meshInstancesToRemove.Add(meshInstance.Entity);
                }

                else
                {
                    var mesh = meshInstance.Mesh;

                    for (var i = 0; i < mesh.GeometryInstances.Count; i++)
                    {
                        var geometryInstance = mesh.GeometryInstances[i];
                        this.meshGeometryInstances.Add(geometryInstance);
                    }
                }
            }

            for (var i = 0; i < this.meshInstancesToRemove.Count; i++)
            {
                this.meshInstances.Remove(this.meshInstancesToRemove[i]);
            }
        }
bool shaderParametersCreated = false;
        public void SetRenderPassConstants(RenderPassConstants renderPassConstants)
        {
            if (!shaderParametersCreated)
            {
                this.graphicsService.CreateShaderParameters(this.renderPassParametersGraphicsBuffer.Id, this.objectParametersGraphicsBuffer.Id);
                shaderParametersCreated = true;
            }

            // TODO: Switch to configurable render pass constants
            MemoryMarshal.Write(this.renderPassConstantsMemoryBuffer.AsSpan(), ref renderPassConstants);
            this.graphicsService.UploadDataToGraphicsBuffer(this.renderPassParametersGraphicsBuffer.Id, this.renderPassConstantsMemoryBuffer);
        }

        public void DrawGeometryInstances()
        {
            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                // TODO: Switch to configurable render pass constants
                // MemoryMarshal.Write(this.objectParametersMemoryBuffer.AsSpan(), ref this.meshGeometryInstances[i]);
                // this.graphicsService.UploadDataToGraphicsBuffer(this.objectParametersGraphicsBuffer.Id, this.objectParametersMemoryBuffer);

                // TODO: Process object parameters
                var geometryInstance = this.meshGeometryInstances[i];
                this.graphicsService.DrawPrimitives(geometryInstance.StartIndex, geometryInstance.IndexCount, geometryInstance.GeometryPacket.VertexBuffer.Id, geometryInstance.GeometryPacket.IndexBuffer.Id, Matrix4x4.Identity);
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
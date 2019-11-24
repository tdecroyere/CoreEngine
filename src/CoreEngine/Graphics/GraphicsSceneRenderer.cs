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
        private readonly IGraphicsService graphicsService;
        private readonly GraphicsManager graphicsManager;

        // Dissociate this?
        public GraphicsScene CurrentScene { get; }

        private RenderPassConstants renderPassConstants;
        private List<GeometryInstance> meshGeometryInstances;
        private List<uint> meshGeometryInstancesParamIdList;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer vertexShaderParametersGraphicsBuffer;
        private GraphicsBuffer objectPropertiesGraphicsBuffer;
        internal uint currentObjectPropertyIndex = 0;

        private Mesh boundingBoxMesh;

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

            this.renderPassConstants = new RenderPassConstants();
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(RenderPassConstants)));
            this.vertexShaderParametersGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(int)) * 1024);
            this.objectPropertiesGraphicsBuffer = this.graphicsManager.CreateDynamicGraphicsBuffer(Marshal.SizeOf(typeof(Matrix4x4)) * 1024);

            this.meshGeometryInstances = new List<GeometryInstance>();
            this.meshGeometryInstancesParamIdList = new List<uint>();

            this.boundingBoxMesh = SetupBoundingBoxMesh();
        }

        private Mesh SetupBoundingBoxMesh()
        {
            var vertexLayout = new VertexLayout(VertexElementType.Float3, VertexElementType.Float3);

            var geometryPacketVertexCount = 8;
            var geometryPacketIndexCount = 8;

            var vertexSize = sizeof(float) * 6;
            var vertexBufferSize = geometryPacketVertexCount * vertexSize;
            var indexBufferSize = geometryPacketIndexCount * sizeof(uint);

            var vertexData = new float[]
            {
                -0.5f, -0.5f, -0.5f, 0, 0, 0,
                -0.5f, 0.5f, -0.5f, 0, 0, 0,
                0.5f, 0.5f, -0.5f, 0, 0, 0,
                0.5f, -0.5f, -0.5f, 0, 0, 0,
                -0.5f, -0.5f, 0.5f, 0, 0, 0,
                -0.5f, 0.5f, 0.5f, 0, 0, 0,
                0.5f, 0.5f, 0.5f, 0, 0, 0,
                0.5f, -0.5f, 0.5f, 0, 0, 0
            };

            var indexData = new uint[]
            {
                0, 1, 1, 2, 2, 3, 3, 0, 
                4, 5, 5, 6, 6, 7, 7, 4,
                0, 4, 1, 5,
                2, 6, 3, 7
            };

            var vertexBufferData = MemoryMarshal.Cast<float, byte>(new Span<float>(vertexData));
            var vertexBuffer = this.graphicsManager.CreateStaticGraphicsBuffer(vertexBufferData);

            var indexBufferData = MemoryMarshal.Cast<uint, byte>(new Span<uint>(indexData));
            var indexBuffer = this.graphicsManager.CreateStaticGraphicsBuffer(indexBufferData);
            
            var geometryPacket = new GeometryPacket(vertexLayout, vertexBuffer, indexBuffer);

            var material = new Material();
            var geometryInstance = new GeometryInstance(geometryPacket, material, 0, (uint)indexData.Length, new BoundingBox(), GeometryPrimitiveType.Line);
            
            var mesh = new Mesh();
            mesh.GeometryInstances.Add(geometryInstance);

            return mesh;
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

            UpdateMeshWorldBoundingBox();
            UpdateDebugBoundingBox();

            CopyGpuData();

            this.CurrentScene.ResetItemsStatus();
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

        private void UpdateDebugBoundingBox()
        {
            for (var i = 0; i < this.CurrentScene.MeshInstances.Count; i++)
            {
                var meshInstance = this.CurrentScene.MeshInstances[i];

                for (var j = 0; j < meshInstance.Mesh.GeometryInstances.Count; j++)
                {
                    var geometryInstance = meshInstance.Mesh.GeometryInstances[j];
                    var worldBoundingBox = meshInstance.WorldBoundingBoxList[j];

                    var scale = Matrix4x4.CreateScale(worldBoundingBox.XSize, worldBoundingBox.YSize, worldBoundingBox.ZSize);
                    var translation = MathUtils.CreateTranslation(new Vector3(worldBoundingBox.Center.X, worldBoundingBox.Center.Y, worldBoundingBox.Center.Z));

                    if (meshInstance.BoundingBoxMeshList.Count <= j)
                    {
                        var boundingBoxMeshInstance = new MeshInstance(this.boundingBoxMesh, scale * translation, this.currentObjectPropertyIndex++);
                        var boundingBoxMeshInstanceId = this.CurrentScene.DebugMeshInstances.Add(boundingBoxMeshInstance);

                        meshInstance.BoundingBoxMeshList.Add(boundingBoxMeshInstanceId);
                    }

                    else if (meshInstance.IsDirty)
                    {
                        var boundingBoxMeshInstanceId = meshInstance.BoundingBoxMeshList[j];
                        var boundingBoxMeshInstance = this.CurrentScene.DebugMeshInstances[boundingBoxMeshInstanceId];

                        boundingBoxMeshInstance.WorldMatrix = scale * translation;
                    }
                }
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

        private void SetupCamera(Camera camera)
        {
            this.renderPassConstants.ViewMatrix = camera.ViewMatrix;
            this.renderPassConstants.ProjectionMatrix = camera.ProjectionMatrix;
        }

        private void CopyGpuData()
        {
            this.graphicsService.BeginCopyGpuData();

            // TODO: Process pending gpu resource loading here
            this.meshGeometryInstances.Clear();
            this.meshGeometryInstancesParamIdList.Clear();

            CopyObjectPropertiesToGpu(this.CurrentScene.MeshInstances);
            CopyObjectPropertiesToGpu(this.CurrentScene.DebugMeshInstances);
            CopyRenderPassConstantsToGpu(this.renderPassConstants);
            CopyDrawParametersToGpu();

            this.graphicsService.EndCopyGpuData();
        }

        private void CopyObjectPropertiesToGpu(ItemCollection<MeshInstance> meshInstances)
        {
            for (var i = 0; i < meshInstances.Count; i++)
            {
                var meshInstance = meshInstances[i];

                if (meshInstance.IsDirty)
                {
                    var objectProperties = new ObjectProperties() { WorldMatrix = meshInstance.WorldMatrix };
                    var objectPropertiesIndex = meshInstance.ObjectPropertiesIndex;
                    var objectPropertiesOffset = (int)objectPropertiesIndex * Marshal.SizeOf<ObjectProperties>();

                    var objectBufferSpan = this.objectPropertiesGraphicsBuffer.MemoryBuffer.Slice(objectPropertiesOffset);
                    MemoryMarshal.Write(objectBufferSpan, ref objectProperties);
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
            this.graphicsService.UploadDataToGraphicsBuffer(this.objectPropertiesGraphicsBuffer.Id, this.objectPropertiesGraphicsBuffer.MemoryBuffer);
        }

        bool shaderParametersCreated = false;
        private void CopyRenderPassConstantsToGpu(RenderPassConstants renderPassConstants)
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

        private void CopyDrawParametersToGpu()
        {
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
        }

        private void DrawGeometryInstances()
        {
            for (var i = 0; i < this.meshGeometryInstances.Count; i++)
            {
                // TODO: Calculate base instanceid based on the previous batch size

                var geometryInstance = this.meshGeometryInstances[i];
                this.graphicsService.DrawPrimitives((GraphicsPrimitiveType)(int)geometryInstance.PrimitiveType, geometryInstance.StartIndex, geometryInstance.IndexCount, geometryInstance.GeometryPacket.VertexBuffer.Id, geometryInstance.GeometryPacket.IndexBuffer.Id, (uint)i);
            }
        }
    }
}
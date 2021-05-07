using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    enum DebugPrimitiveType : uint
    {
        Line,
        Cube,
        Sphere
    }

    // TODO: Find a way to auto align fields to 16 Bytes (Required by shaders)
    readonly struct DebugPrimitive
    {
        private DebugPrimitive(Matrix4x4 worldMatrix, Vector4 color)
        {
            this.WorldMatrix = worldMatrix;
            this.Color = color;
        }

        public readonly Matrix4x4 WorldMatrix { get; }
        public readonly Vector4 Color { get; }

        public static DebugPrimitive CreateLine(Vector3 point1, Vector3 point2, Vector3 color)
        {
            var directionVector = point2 - point1;
            var scale = new Vector3(1, 1, directionVector.Length());

            Matrix4x4.Invert(MathUtils.CreateLookAtMatrix(point1, point2, new Vector3(0, 1, 0)), out var transformMatrix);

            var worldMatrix = Matrix4x4.CreateScale(scale) * transformMatrix;
            return new DebugPrimitive(worldMatrix, new Vector4(color, 1));
        }

        public static DebugPrimitive CreateBoundingBox(BoundingBox boundingBox, Vector3 color)
        {
            var worldMatrix = Matrix4x4.CreateScale(boundingBox.Size) * MathUtils.CreateTranslation(boundingBox.Center);
            return new DebugPrimitive(worldMatrix, new Vector4(color, 1));
        }

        public static DebugPrimitive CreateSphere(Vector3 position, float radius, Vector3 color)
        {
            var worldMatrix = Matrix4x4.CreateScale(new Vector3(radius * 2, radius * 2, radius * 2)) * MathUtils.CreateTranslation(position);
            return new DebugPrimitive(worldMatrix, new Vector4(color, 1));
        }
    }

    public class DebugRenderer
    {
        private const uint maxPrimitiveCountPerThreadGroup = 32;  
        
        private readonly GraphicsManager graphicsManager;
        private readonly RenderManager renderManager;

        private readonly Shader shader;
        private readonly GraphicsBuffer cpuPrimitiveBuffer;
        private readonly GraphicsBuffer primitiveBuffer;

        private readonly GraphicsBuffer vertexBuffer;
        private readonly GraphicsBuffer indexBuffer;

        private readonly List<DebugPrimitive> linePrimitives;
        private readonly List<DebugPrimitive> cubePrimitives;
        private readonly List<DebugPrimitive> spherePrimitives;

        private int currentPrimitiveCount;
        private int sphereVertexBufferOffset;
        private int sphereIndexBufferOffset;
        private int lineVertexBufferOffset;
        private int lineIndexBufferOffset;

        public DebugRenderer(GraphicsManager graphicsManager, RenderManager renderManager, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            this.renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/DebugRender.shader");

            var maxPrimitiveCount = 10000;

            this.linePrimitives = new List<DebugPrimitive>();
            this.cubePrimitives = new List<DebugPrimitive>();
            this.spherePrimitives = new List<DebugPrimitive>();

            this.cpuPrimitiveBuffer = this.graphicsManager.CreateGraphicsBuffer<DebugPrimitive>(GraphicsHeapType.Upload, maxPrimitiveCount * 4, isStatic: false, label: "DebugPrimitiveBuffer_Cpu");
            this.primitiveBuffer = this.graphicsManager.CreateGraphicsBuffer<DebugPrimitive>(GraphicsHeapType.Gpu, maxPrimitiveCount * 4, isStatic: false, label: "DebugPrimitiveBuffer_Gpu");
            
            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector3>(GraphicsHeapType.Gpu, 100, isStatic: true, "DebugVertexBuffer_Gpu");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Gpu, 300, isStatic: true, "DebugIndexBuffer_Gpu");

            InitGeometryBuffers();
            this.currentPrimitiveCount = 0;
        }

        private void InitGeometryBuffers()
        {
            var vertexBufferCpu = this.graphicsManager.CreateGraphicsBuffer<Vector3>(GraphicsHeapType.Upload, 100, isStatic: true, "DebugVertexBuffer_Cpu");
            var indexBufferCpu = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, 300, isStatic: true, "DebugIndexBuffer_Cpu");

            var vertexBufferData = this.graphicsManager.GetCpuGraphicsBufferPointer<Vector3>(vertexBufferCpu);
            var indexBufferData = this.graphicsManager.GetCpuGraphicsBufferPointer<uint>(indexBufferCpu);

            // Init Cube
            var minPoint = new Vector3(-0.5f, -0.5f, -0.5f);
            var maxPoint = new Vector3(0.5f, 0.5f, 0.5f);

            float xSize = maxPoint.X - minPoint.X;
            float ySize = maxPoint.Y - minPoint.Y;
            float zSize = maxPoint.Z - minPoint.Z;

            vertexBufferData[0] = minPoint;
            vertexBufferData[1] = minPoint + new Vector3(0, 0, zSize);
            vertexBufferData[2] = minPoint + new Vector3(xSize, 0, 0);
            vertexBufferData[3] = minPoint + new Vector3(xSize, 0, zSize);
            vertexBufferData[4] = minPoint + new Vector3(0, ySize, 0);
            vertexBufferData[5] = minPoint + new Vector3(0, ySize, zSize);
            vertexBufferData[6] = minPoint + new Vector3(xSize, ySize, 0);
            vertexBufferData[7] = minPoint + new Vector3(xSize, ySize, zSize);

            indexBufferData[0] = 0;
            indexBufferData[1] = 1;
            indexBufferData[2] = 0;
            indexBufferData[3] = 2;
            indexBufferData[4] = 1;
            indexBufferData[5] = 3;
            indexBufferData[6] = 2;
            indexBufferData[7] = 3;

            indexBufferData[8] = 4;
            indexBufferData[9] = 5;
            indexBufferData[10] = 4;
            indexBufferData[11] = 6;
            indexBufferData[12] = 5;
            indexBufferData[13] = 7;
            indexBufferData[14] = 6;
            indexBufferData[15] = 7;

            indexBufferData[16] = 0;
            indexBufferData[17] = 4;
            indexBufferData[18] = 2;
            indexBufferData[19] = 6;
            indexBufferData[20] = 3;
            indexBufferData[21] = 7;
            indexBufferData[22] = 1;
            indexBufferData[23] = 5;

            int currentVertexCount = 8;
            int currentIndexCount = 24;

            this.sphereVertexBufferOffset = currentVertexCount;
            this.sphereIndexBufferOffset = currentIndexCount;

            var position = Vector3.Zero;
            var radius = 0.5f;
            const uint steps = 30;

            float stepAngle = 360.0f * MathF.PI / 180.0f / steps;

            var vertexOffset = currentVertexCount - this.sphereVertexBufferOffset;

            for (var i = 0; i < steps; i++)
            {
                vertexBufferData[currentVertexCount++] = position + new Vector3(0, -radius * MathF.Cos(i * stepAngle), radius * MathF.Sin(i * stepAngle));
                indexBufferData[currentIndexCount++] = (uint)(vertexOffset + i);
                indexBufferData[currentIndexCount++] = (uint)(vertexOffset + ((i + 1) % steps));
            }

            vertexOffset = currentVertexCount - this.sphereVertexBufferOffset;

            for (var i = 0; i < steps; i++)
            {
                vertexBufferData[currentVertexCount++] = position + new Vector3(-radius * MathF.Cos(i * stepAngle), radius * MathF.Sin(i * stepAngle), 0);
                indexBufferData[currentIndexCount++] = (uint)(vertexOffset + i);
                indexBufferData[currentIndexCount++] = (uint)(vertexOffset + ((i + 1) % steps));
            }

            vertexOffset = currentVertexCount - this.sphereVertexBufferOffset;

            for (var i = 0; i < steps; i++)
            {
                vertexBufferData[currentVertexCount++] = position + new Vector3(-radius * MathF.Cos(i * stepAngle), 0, radius * MathF.Sin(i * stepAngle));
                indexBufferData[currentIndexCount++] = (uint)(vertexOffset + i);
                indexBufferData[currentIndexCount++] = (uint)(vertexOffset + ((i + 1) % steps));
            }

            this.lineVertexBufferOffset = currentVertexCount;
            this.lineIndexBufferOffset = currentIndexCount;

            vertexBufferData[currentVertexCount++] = new Vector3(0, 0, 0.0f);
            vertexBufferData[currentVertexCount++] = new Vector3(0.0f, 0.0f, 1.0f);
            indexBufferData[currentIndexCount++] = 0;
            indexBufferData[currentIndexCount++] = 1;

            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "DebugRendererCopy");
            this.graphicsManager.CopyDataToGraphicsBuffer<Vector3>(copyCommandList, this.vertexBuffer, vertexBufferCpu, currentVertexCount);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, indexBufferCpu, currentIndexCount);
            this.graphicsManager.CommitCommandList(copyCommandList);

            this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList }, isAwaitable: false);
        }

        public void ClearDebugLines()
        {
            this.currentPrimitiveCount = 0;

            this.linePrimitives.Clear();
            this.cubePrimitives.Clear();
            this.spherePrimitives.Clear();
        }

        public void DrawLine(Vector3 point1, Vector3 point2)
        {
            DrawLine(point1, point2, Vector3.Zero);
        }

        public void DrawLine(Vector3 point1, Vector3 point2, Vector3 color)
        {
            this.linePrimitives.Add(DebugPrimitive.CreateLine(point1, point2, color));
            this.currentPrimitiveCount++;
        }

        public void DrawBoundingFrustum(BoundingFrustum boundingFrustum, Vector3 color)
        {
            if (boundingFrustum == null)
            {
                throw new ArgumentNullException(nameof(boundingFrustum));
            }
            
            DrawLine(boundingFrustum.LeftTopNearPoint, boundingFrustum.LeftTopFarPoint, color);
            DrawLine(boundingFrustum.LeftBottomNearPoint, boundingFrustum.LeftBottomFarPoint, color);
            DrawLine(boundingFrustum.RightTopNearPoint, boundingFrustum.RightTopFarPoint, color);
            DrawLine(boundingFrustum.RightBottomNearPoint, boundingFrustum.RightBottomFarPoint, color);

            DrawLine(boundingFrustum.LeftTopNearPoint, boundingFrustum.RightTopNearPoint, color);
            DrawLine(boundingFrustum.LeftBottomNearPoint, boundingFrustum.RightBottomNearPoint, color);
            DrawLine(boundingFrustum.LeftTopNearPoint, boundingFrustum.LeftBottomNearPoint, color);
            DrawLine(boundingFrustum.RightTopNearPoint, boundingFrustum.RightBottomNearPoint, color);

            DrawLine(boundingFrustum.LeftTopFarPoint, boundingFrustum.RightTopFarPoint, color);
            DrawLine(boundingFrustum.LeftBottomFarPoint, boundingFrustum.RightBottomFarPoint, color);
            DrawLine(boundingFrustum.LeftTopFarPoint, boundingFrustum.LeftBottomFarPoint, color);
            DrawLine(boundingFrustum.RightTopFarPoint, boundingFrustum.RightBottomFarPoint, color);
        }

        public void DrawBoundingBox(BoundingBox boundingBox, Vector3 color)
        {
            this.cubePrimitives.Add(DebugPrimitive.CreateBoundingBox(boundingBox, color));
            this.currentPrimitiveCount++;
        }

        public void DrawSphere(Vector3 position, float radius, Vector3 color)
        {
            this.spherePrimitives.Add(DebugPrimitive.CreateSphere(position, radius, color));
            this.currentPrimitiveCount++;
        }

        public void Render(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture renderTargetTexture, Texture? depthTexture)
        {
            if (this.currentPrimitiveCount > 0)
            {
                var copyCommandList = CreateCopyCommandList();
                var renderCommandList = CreateRenderCommandList(renderPassParametersGraphicsBuffer, renderTargetTexture, depthTexture);

                var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList }, isAwaitable: true);
                
                this.graphicsManager.WaitForCommandQueue(this.renderManager.RenderCommandQueue, copyFence);
                this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { renderCommandList }, isAwaitable: false);
            }
        }

        private CommandList CreateCopyCommandList()
        {
            var cpuPrimitiveData = this.graphicsManager.GetCpuGraphicsBufferPointer<DebugPrimitive>(this.cpuPrimitiveBuffer);
            var currentInstanceIndex = 0;

            for (var i = 0; i < this.linePrimitives.Count; i++)
            {
                cpuPrimitiveData[currentInstanceIndex++] = this.linePrimitives[i];
            }

            for (var i = 0; i < this.cubePrimitives.Count; i++)
            {
                cpuPrimitiveData[currentInstanceIndex++] = this.cubePrimitives[i];
            }

            for (var i = 0; i < this.spherePrimitives.Count; i++)
            {
                cpuPrimitiveData[currentInstanceIndex++] = this.spherePrimitives[i];
            }

            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "DebugRendererCopy");

            var startCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
            this.graphicsManager.CopyDataToGraphicsBuffer<DebugPrimitive>(copyCommandList, this.primitiveBuffer, this.cpuPrimitiveBuffer, this.currentPrimitiveCount);
            var endCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            this.graphicsManager.CommitCommandList(copyCommandList);
            this.renderManager.AddGpuTiming("DebugRendererCopy", QueryBufferType.CopyTimestamp, startCopyQueryIndex, endCopyQueryIndex);

            return copyCommandList;
        }

        private CommandList CreateRenderCommandList(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture renderTargetTexture, Texture? depthTexture)
        {
            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "DebugRenderer");

            // var renderTarget = new RenderTargetDescriptor(renderTargetTexture, null, BlendOperation.None);
            var renderTarget = new RenderTargetDescriptor(renderTargetTexture, Vector4.Zero, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthTexture, DepthBufferOperation.CompareGreater, true, 0);

            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);
            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.SetShader(renderCommandList, this.shader);
            this.graphicsManager.SetShaderBuffer(renderCommandList, this.primitiveBuffer, 1);
            this.graphicsManager.SetShaderBuffer(renderCommandList, renderPassParametersGraphicsBuffer, 2);
            this.graphicsManager.SetShaderBuffer(renderCommandList, this.vertexBuffer, 3);
            this.graphicsManager.SetShaderBuffer(renderCommandList, this.indexBuffer, 4);

            if (this.linePrimitives.Count > 0)
            {
                this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.linePrimitives.Count, 0, (uint)this.lineVertexBufferOffset, (uint)this.lineIndexBufferOffset / 2, (uint)DebugPrimitiveType.Line });
                this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.linePrimitives.Count / maxPrimitiveCountPerThreadGroup), 1, 1);
            }

            if (this.cubePrimitives.Count > 0)
            {
                this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.cubePrimitives.Count, (uint)this.linePrimitives.Count, 0, 0, (uint)DebugPrimitiveType.Cube });
                this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.cubePrimitives.Count / maxPrimitiveCountPerThreadGroup), 1, 1);
            }

            if (this.spherePrimitives.Count > 0)
            {
                this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.spherePrimitives.Count, (uint)this.linePrimitives.Count + (uint)this.cubePrimitives.Count, (uint)this.sphereVertexBufferOffset, (uint)this.sphereIndexBufferOffset / 2, (uint)DebugPrimitiveType.Sphere });
                this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.spherePrimitives.Count / maxPrimitiveCountPerThreadGroup), 1, 1);
            }

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.EndRenderPass(renderCommandList);

            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming("DebugRenderer", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return renderCommandList;
        }
    }
}
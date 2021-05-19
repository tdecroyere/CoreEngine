using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Graphics;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    // TODO: Find a way to auto align fields to 16 Bytes (Required by shaders)
    readonly struct DebugPrimitive
    {
        private DebugPrimitive(in Matrix4x4 worldMatrix, in Vector4 color)
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

        public static DebugPrimitive CreateBoundingBox(in BoundingBox boundingBox, Vector3 color)
        {
            var worldMatrix = MathUtils.CreateScaleTranslation(boundingBox.Size, boundingBox.Center);
            return new DebugPrimitive(worldMatrix, new Vector4(color, 1));
        }

        public static DebugPrimitive CreateBoundingFrustum(BoundingFrustum boundingFrustum, Vector3 color)
        {
            Matrix4x4.Invert(boundingFrustum.Matrix, out var transformMatrix);
            return new DebugPrimitive(transformMatrix, new Vector4(color, 1));
        }

        public static DebugPrimitive CreateSphere(Vector3 position, float radius, Vector3 color)
        {
            var diameter = radius * 2.0f;
            var worldMatrix = MathUtils.CreateScaleTranslation(new Vector3(diameter), position);
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

        private readonly DebugPrimitive[] linePrimitives;
        private readonly DebugPrimitive[] cubePrimitives;
        private readonly DebugPrimitive[] spherePrimitives;

        private int currentLinePrimitiveCount;
        private int currentCubePrimitiveCount;
        private int currentSpherePrimitiveCount;


        private int sphereVertexBufferOffset;
        private int sphereIndexBufferOffset;
        private int lineVertexBufferOffset;
        private int lineIndexBufferOffset;
        private readonly int maxPrimitiveCount = 10000;

        public DebugRenderer(GraphicsManager graphicsManager, RenderManager renderManager, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));
            this.renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/DebugRender.shader");

            // TODO: Change that
            this.linePrimitives = new DebugPrimitive[maxPrimitiveCount];
            this.cubePrimitives = new DebugPrimitive[maxPrimitiveCount];
            this.spherePrimitives = new DebugPrimitive[maxPrimitiveCount];

            this.cpuPrimitiveBuffer = this.graphicsManager.CreateGraphicsBuffer<DebugPrimitive>(GraphicsHeapType.Upload, maxPrimitiveCount, isStatic: false, label: "DebugPrimitiveBuffer_Cpu");
            this.primitiveBuffer = this.graphicsManager.CreateGraphicsBuffer<DebugPrimitive>(GraphicsHeapType.Gpu, maxPrimitiveCount, isStatic: false, label: "DebugPrimitiveBuffer_Gpu");
            
            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector3>(GraphicsHeapType.Gpu, 100, isStatic: true, "DebugVertexBuffer_Gpu");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Gpu, 300, isStatic: true, "DebugIndexBuffer_Gpu");

            InitGeometryBuffers();
            this.currentLinePrimitiveCount = 0;
            this.currentCubePrimitiveCount = 0;
            this.currentSpherePrimitiveCount = 0;
        }

        private void InitGeometryBuffers()
        {
            var vertexBufferData = new Vector3[100];
            var indexBufferData = new uint[300];

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

            using var vertexBufferCpu = this.graphicsManager.CreateGraphicsBuffer<Vector3>(GraphicsHeapType.Upload, 100, isStatic: true, "DebugVertexBuffer_Cpu");
            using var indexBufferCpu = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, 300, isStatic: true, "DebugIndexBuffer_Cpu");

            this.graphicsManager.CopyDataToGraphicsBuffer<Vector3>(vertexBufferCpu, 0, vertexBufferData);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(indexBufferCpu, 0, indexBufferData);

            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "DebugRendererCopy");
            this.graphicsManager.CopyDataToGraphicsBuffer<Vector3>(copyCommandList, this.vertexBuffer, vertexBufferCpu, currentVertexCount);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, indexBufferCpu, currentIndexCount);
            this.graphicsManager.CommitCommandList(copyCommandList);

            this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList }, isAwaitable: false);
        }

        public void ClearDebugLines()
        {
            this.currentLinePrimitiveCount = 0;
            this.currentCubePrimitiveCount = 0;
            this.currentSpherePrimitiveCount = 0;
        }

        public void DrawLine(Vector3 point1, Vector3 point2)
        {
            DrawLine(point1, point2, Vector3.Zero);
        }

        public void DrawLine(Vector3 point1, Vector3 point2, Vector3 color)
        {
            this.linePrimitives[this.currentLinePrimitiveCount++] = DebugPrimitive.CreateLine(point1, point2, color);
        }

        public void DrawBoundingFrustum(in BoundingFrustum boundingFrustum, Vector3 color)
        {
            if (boundingFrustum == null)
            {
                throw new ArgumentNullException(nameof(boundingFrustum));
            }

            this.cubePrimitives[this.currentCubePrimitiveCount++] = DebugPrimitive.CreateBoundingFrustum(boundingFrustum, color);
        }

        public void DrawBoundingBox(in BoundingBox boundingBox, in Vector3 color)
        {
            this.cubePrimitives[this.currentCubePrimitiveCount++] = DebugPrimitive.CreateBoundingBox(boundingBox, color);
        }

        public void DrawSphere(Vector3 position, float radius, Vector3 color)
        {
            this.spherePrimitives[this.currentSpherePrimitiveCount++] = DebugPrimitive.CreateSphere(position, radius, color);
        }

        public void Render(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture renderTargetTexture, Texture? depthTexture)
        {
            if (renderPassParametersGraphicsBuffer == null)
            {
                throw new ArgumentNullException(nameof(renderPassParametersGraphicsBuffer));
            }

            if (this.currentLinePrimitiveCount > 0 || this.currentCubePrimitiveCount > 0 || this.currentSpherePrimitiveCount > 0)
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
            var tempArray = ArrayPool<DebugPrimitive>.Shared.Rent(maxPrimitiveCount);

            this.linePrimitives.AsSpan().Slice(0, this.currentLinePrimitiveCount).CopyTo(tempArray);
            this.cubePrimitives.AsSpan().Slice(0, this.currentCubePrimitiveCount).CopyTo(tempArray.AsSpan().Slice(this.currentLinePrimitiveCount));
            this.spherePrimitives.AsSpan().Slice(0, this.currentSpherePrimitiveCount).CopyTo(tempArray.AsSpan().Slice(this.currentLinePrimitiveCount + this.currentCubePrimitiveCount));

            this.graphicsManager.CopyDataToGraphicsBuffer<DebugPrimitive>(this.cpuPrimitiveBuffer, 0, tempArray.AsSpan().Slice(0, this.currentLinePrimitiveCount + this.currentCubePrimitiveCount + this.currentSpherePrimitiveCount));

            ArrayPool<DebugPrimitive>.Shared.Return(tempArray);

            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "DebugRendererCopy");

            var startCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
            this.graphicsManager.CopyDataToGraphicsBuffer<DebugPrimitive>(copyCommandList, this.primitiveBuffer, this.cpuPrimitiveBuffer, this.currentLinePrimitiveCount + this.currentCubePrimitiveCount + this.currentSpherePrimitiveCount);
            var endCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            this.graphicsManager.CommitCommandList(copyCommandList);
            this.renderManager.AddGpuTiming("DebugRendererCopy", QueryBufferType.CopyTimestamp, startCopyQueryIndex, endCopyQueryIndex);

            return copyCommandList;
        }

        private CommandList CreateRenderCommandList(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture renderTargetTexture, Texture? depthTexture)
        {
            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "DebugRenderer");

            var renderTarget = new RenderTargetDescriptor(renderTargetTexture, null, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthTexture, DepthBufferOperation.CompareGreater, backfaceCulling: true);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);

            this.graphicsManager.SetShader(renderCommandList, this.shader);

            if (this.currentLinePrimitiveCount > 0)
            {
                this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.currentLinePrimitiveCount, 0, (uint)this.lineVertexBufferOffset, (uint)this.lineIndexBufferOffset / 2, 128, 2, 1, this.primitiveBuffer.ShaderResourceIndex, renderPassParametersGraphicsBuffer.ShaderResourceIndex, this.vertexBuffer.ShaderResourceIndex, this.indexBuffer.ShaderResourceIndex });
                this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.currentLinePrimitiveCount / maxPrimitiveCountPerThreadGroup), 1, 1);
            }

            if (this.currentCubePrimitiveCount > 0)
            {
                this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.currentCubePrimitiveCount, (uint)this.currentLinePrimitiveCount, 0, 0, 20, 8, 12, this.primitiveBuffer.ShaderResourceIndex, renderPassParametersGraphicsBuffer.ShaderResourceIndex, this.vertexBuffer.ShaderResourceIndex, this.indexBuffer.ShaderResourceIndex });
                this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.currentCubePrimitiveCount / maxPrimitiveCountPerThreadGroup), 1, 1);
            }

            if (this.currentSpherePrimitiveCount > 0)
            {
                this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.currentSpherePrimitiveCount, (uint)this.currentLinePrimitiveCount + (uint)this.currentCubePrimitiveCount, (uint)this.sphereVertexBufferOffset, (uint)this.sphereIndexBufferOffset / 2, 2, 90, 90, this.primitiveBuffer.ShaderResourceIndex, renderPassParametersGraphicsBuffer.ShaderResourceIndex, this.vertexBuffer.ShaderResourceIndex, this.indexBuffer.ShaderResourceIndex });
                this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.currentSpherePrimitiveCount / maxPrimitiveCountPerThreadGroup), 1, 1);
            }

            this.graphicsManager.EndRenderPass(renderCommandList);
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming("DebugRenderer", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return renderCommandList;
        }
    }
}
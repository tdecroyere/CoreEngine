using System;
using System.Numerics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    public class DebugRenderer
    {
        private readonly GraphicsManager graphicsManager;
        private readonly RenderManager renderManager;

        private Shader shader;
        private GraphicsBuffer cpuVertexBuffer;
        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer cpuIndexBuffer;
        private GraphicsBuffer indexBuffer;

        private int currentDebugLineIndex;

        public DebugRenderer(GraphicsManager graphicsManager, RenderManager renderManager, ResourcesManager resourcesManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
            }

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager;
            this.renderManager = renderManager;

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/DebugRender.shader");

            var maxLineCount = 100000;

            this.cpuVertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector4>(GraphicsHeapType.Upload, maxLineCount * 4, isStatic: false, label: "DebugVertexBuffer");
            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector4>(GraphicsHeapType.Gpu, maxLineCount * 4, isStatic: false, label: "DebugVertexBuffer");
            this.cpuIndexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, maxLineCount * 2, isStatic: false, label: "DebugIndexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Gpu, maxLineCount * 2, isStatic: false, label: "DebugIndexBuffer");

            this.currentDebugLineIndex = 0;
        }

        public void ClearDebugLines()
        {
            this.currentDebugLineIndex = 0;
        }

        public void DrawLine(Vector3 point1, Vector3 point2)
        {
            DrawLine(point1, point2, Vector3.Zero);
        }

        public void DrawLine(Vector3 point1, Vector3 point2, Vector3 color)
        {
            var vertexData = this.graphicsManager.GetCpuGraphicsBufferPointer<Vector4>(this.cpuVertexBuffer);

            vertexData[this.currentDebugLineIndex * 4] = new Vector4(point1, 0);
            vertexData[this.currentDebugLineIndex * 4 + 1] = new Vector4(color, 0);
            vertexData[this.currentDebugLineIndex * 4 + 2] = new Vector4(point2, 0);
            vertexData[this.currentDebugLineIndex * 4 + 3] = new Vector4(color, 0);

            var indexData = this.graphicsManager.GetCpuGraphicsBufferPointer<uint>(this.cpuIndexBuffer);

            indexData[this.currentDebugLineIndex * 2] = (uint)this.currentDebugLineIndex * 2;
            indexData[this.currentDebugLineIndex * 2 + 1] = (uint)this.currentDebugLineIndex * 2 + 1;

            this.currentDebugLineIndex++;
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
            var point1 = boundingBox.MinPoint;
            var point2 = boundingBox.MinPoint + new Vector3(0, 0, boundingBox.ZSize);
            var point3 = boundingBox.MinPoint + new Vector3(boundingBox.XSize, 0, 0);
            var point4 = boundingBox.MinPoint + new Vector3(boundingBox.XSize, 0, boundingBox.ZSize);
            var point5 = boundingBox.MinPoint + new Vector3(0, boundingBox.YSize, 0);
            var point6 = boundingBox.MinPoint + new Vector3(0, boundingBox.YSize, boundingBox.ZSize);
            var point7 = boundingBox.MinPoint + new Vector3(boundingBox.XSize, boundingBox.YSize, 0);
            var point8 = boundingBox.MinPoint + new Vector3(boundingBox.XSize, boundingBox.YSize, boundingBox.ZSize);

            DrawLine(point1, point2, color);
            DrawLine(point1, point3, color);
            DrawLine(point2, point4, color);
            DrawLine(point3, point4, color);

            DrawLine(point5, point6, color);
            DrawLine(point5, point7, color);
            DrawLine(point6, point8, color);
            DrawLine(point7, point8, color);

            DrawLine(point1, point5, color);
            DrawLine(point3, point7, color);
            DrawLine(point4, point8, color);
            DrawLine(point2, point6, color);
        }

        public void DrawSphere(Vector3 position, float radius, int steps, Vector3 color)
        {
            var stepAngle = 360.0f / steps;
            var pointPosition = new Vector3(0, -radius * MathF.Cos(MathUtils.DegreesToRad(0)), radius * MathF.Sin(MathUtils.DegreesToRad(0)));

            for (var i = 1; i < steps + 1; i++)
            {
                var pointPosition2 = new Vector3(0, -radius * MathF.Cos(MathUtils.DegreesToRad(i * stepAngle)), radius * MathF.Sin(MathUtils.DegreesToRad(i * stepAngle)));

                DrawLine(position + pointPosition, position + pointPosition2, color);
                pointPosition = pointPosition2;
            }

            pointPosition = new Vector3(-radius * MathF.Cos(MathUtils.DegreesToRad(0)), radius * MathF.Sin(MathUtils.DegreesToRad(0)), 0);

            for (var i = 1; i < steps + 1; i++)
            {
                var pointPosition2 = new Vector3(-radius * MathF.Cos(MathUtils.DegreesToRad(i * stepAngle)), radius * MathF.Sin(MathUtils.DegreesToRad(i * stepAngle)), 0);

                DrawLine(position + pointPosition, position + pointPosition2, color);
                pointPosition = pointPosition2;
            }

            pointPosition = new Vector3(-radius * MathF.Cos(MathUtils.DegreesToRad(0)), 0, radius * MathF.Sin(MathUtils.DegreesToRad(0)));

            for (var i = 1; i < steps + 1; i++)
            {
                var pointPosition2 = new Vector3(-radius * MathF.Cos(MathUtils.DegreesToRad(i * stepAngle)), 0, radius * MathF.Sin(MathUtils.DegreesToRad(i * stepAngle)));

                DrawLine(position + pointPosition, position + pointPosition2, color);
                pointPosition = pointPosition2;
            }
        }

        public void Render(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture renderTargetTexture, Texture? depthTexture)
        {
            if (this.currentDebugLineIndex > 0)
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
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "DebugRendererCopy");

            var startCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
            this.graphicsManager.CopyDataToGraphicsBuffer<Vector4>(copyCommandList, this.vertexBuffer, this.cpuVertexBuffer, this.currentDebugLineIndex * 4);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, this.cpuIndexBuffer, this.currentDebugLineIndex * 2);
            var endCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            this.graphicsManager.CommitCommandList(copyCommandList);
            this.renderManager.AddGpuTiming("DebugRendererCopy", QueryBufferType.CopyTimestamp, startCopyQueryIndex, endCopyQueryIndex);

            return copyCommandList;
        }

        private CommandList CreateRenderCommandList(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture renderTargetTexture, Texture? depthTexture)
        {
            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "DebugRenderer");

            var renderTarget = new RenderTargetDescriptor(renderTargetTexture, null, BlendOperation.None);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthTexture, DepthBufferOperation.CompareGreater, true, PrimitiveType.Line);

            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);

            this.graphicsManager.SetShader(renderCommandList, this.shader);
            this.graphicsManager.SetShaderBuffer(renderCommandList, this.vertexBuffer, 0);
            this.graphicsManager.SetShaderBuffer(renderCommandList, renderPassParametersGraphicsBuffer, 1);

            this.graphicsManager.SetIndexBuffer(renderCommandList, this.indexBuffer);
            this.graphicsManager.DrawIndexedPrimitives(renderCommandList, PrimitiveType.Line, 0, this.currentDebugLineIndex * 2, 1, 0);

            this.graphicsManager.EndRenderPass(renderCommandList);

            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming("DebugRenderer", QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);

            return renderCommandList;
        }
    }
}
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
        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer indexBuffer;

        public CommandBuffer copyCommandBuffer;
        public CommandBuffer commandBuffer;

        private int currentDebugLineIndex;
        private Vector4[] vertexData;
        private uint[] indexData;

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
            this.vertexData = new Vector4[maxLineCount * 4];
            this.indexData = new uint[maxLineCount * 2];

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector4>(maxLineCount * 4, isStatic: false, isWriteOnly: true, label: "DebugVertexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(maxLineCount * 2, isStatic: false, isWriteOnly: true, label: "DebugIndexBuffer");

            this.copyCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "DebugRendererCopy");
            this.commandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Render, "DebugRenderer");
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
            this.vertexData[this.currentDebugLineIndex * 4] = new Vector4(point1, 0);
            this.vertexData[this.currentDebugLineIndex * 4 + 1] = new Vector4(color, 0);
            this.vertexData[this.currentDebugLineIndex * 4 + 2] = new Vector4(point2, 0);
            this.vertexData[this.currentDebugLineIndex * 4 + 3] = new Vector4(color, 0);

            this.indexData[this.currentDebugLineIndex * 2] = (uint)this.currentDebugLineIndex * 2;
            this.indexData[this.currentDebugLineIndex * 2 + 1] = (uint)this.currentDebugLineIndex * 2 + 1;

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

        public CommandList Render(GraphicsBuffer renderPassParametersGraphicsBuffer, Texture? depthTexture, CommandList previousCommandList)
        {
            if (this.currentDebugLineIndex > 0)
            {
                this.graphicsManager.ResetCommandBuffer(copyCommandBuffer);

                var copyCommandList = this.graphicsManager.CreateCopyCommandList(this.copyCommandBuffer, "DebugCopyCommandList");
                this.graphicsManager.UploadDataToGraphicsBuffer<Vector4>(copyCommandList, this.vertexBuffer, this.vertexData.AsSpan().Slice(0, this.currentDebugLineIndex * 4));
                this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, this.indexData.AsSpan().Slice(0, this.currentDebugLineIndex * 2));
                this.graphicsManager.CommitCopyCommandList(copyCommandList);
                this.graphicsManager.ExecuteCommandBuffer(copyCommandBuffer);

                this.graphicsManager.ResetCommandBuffer(commandBuffer);

                var renderTarget = new RenderTargetDescriptor(this.renderManager.MainRenderTargetTexture, new Vector4(0, 0, 0, 1), BlendOperation.None);
                var renderPassDescriptor = new RenderPassDescriptor(renderTarget, depthTexture, DepthBufferOperation.CompareLess, true);
                var commandList = this.graphicsManager.CreateRenderCommandList(commandBuffer, renderPassDescriptor, "Graphics2DRenderCommandList");
                this.graphicsManager.WaitForCommandList(commandList, previousCommandList);

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetShaderBuffer(commandList, this.vertexBuffer, 0);
                this.graphicsManager.SetShaderBuffer(commandList, renderPassParametersGraphicsBuffer, 1);

                this.graphicsManager.SetIndexBuffer(commandList, this.indexBuffer);
                this.graphicsManager.DrawIndexedPrimitives(commandList, PrimitiveType.Line, 0, this.currentDebugLineIndex * 2, 1, 0);
                this.graphicsManager.CommitRenderCommandList(commandList);
                this.graphicsManager.ExecuteCommandBuffer(commandBuffer);

                return commandList;
            }

            return previousCommandList;
        }
    }
}
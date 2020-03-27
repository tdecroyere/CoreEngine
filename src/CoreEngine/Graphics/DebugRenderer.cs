using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class DebugRenderer
    {
        private readonly GraphicsManager graphicsManager;

        private Shader shader;
        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer indexBuffer;

        private int currentDebugLineIndex;
        private Vector4[] vertexData;
        private uint[] indexData;

        public DebugRenderer(GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager;

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/DebugRender.shader");

            var maxLineCount = 10000;
            this.vertexData = new Vector4[maxLineCount * 4];
            this.indexData = new uint[maxLineCount * 2];

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector4>(maxLineCount * 4, GraphicsResourceType.Dynamic, true, "DebugVertexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(maxLineCount * 2, GraphicsResourceType.Dynamic, true, "DebugIndexBuffer");

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

        private void DrawBoundingBox(BoundingBox boundingBox, Vector3 color)
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

        // public void Render(GraphicsBuffer renderPassParametersGraphicsBuffer, CommandList renderCommandList)
        // {
        //     // TODO: Refactor to avoid crash
        //     if (this.currentDebugLineIndex > 0)
        //     {
        //         var commandBuffer = this.graphicsManager.CreateCommandBuffer("DebugRenderer");

        //         var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "DebugCopyCommandList");
        //         this.graphicsManager.UploadDataToGraphicsBuffer<Vector4>(copyCommandList, this.vertexBuffer, this.vertexData);
        //         this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, this.indexData);
        //         this.graphicsManager.CommitCopyCommandList(copyCommandList);

        //         this.graphicsManager.SetShader(renderCommandList, this.shader);
        //         this.graphicsManager.SetShaderBuffer(renderCommandList, this.vertexBuffer, 0);
        //         this.graphicsManager.SetShaderBuffer(renderCommandList, renderPassParametersGraphicsBuffer, 1);

        //         this.graphicsManager.SetIndexBuffer(renderCommandList, this.indexBuffer);
        //         this.graphicsManager.DrawIndexedPrimitives(renderCommandList, GeometryPrimitiveType.Line, 0, this.currentDebugLineIndex * 2, 1, 0);

        //         this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
        //     }
        // }
    }
}
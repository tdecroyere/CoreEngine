using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;

namespace CoreEngine.Graphics
{
    public class GraphicsDebugRenderer : SystemManager
    {
        private readonly GraphicsManager graphicsManager;

        private GeometryPacket debugGeometryPacket;
        private int currentDebugLineIndex;
        private Vector3[] debugVertexData;
        private uint[] debugIndexData;

        public GraphicsDebugRenderer(GraphicsManager graphicsManager)
        {
            this.graphicsManager = graphicsManager;

            var maxLineCount = 10000;
            this.debugVertexData = new Vector3[maxLineCount * 4];
            this.debugIndexData = new uint[maxLineCount * 2];
            this.debugGeometryPacket = SetupDebugGeometryPacket();
            this.currentDebugLineIndex = 0;
        }

        public override void PostUpdate()
        {
            ClearDebugLines();
        }

        private GeometryPacket SetupDebugGeometryPacket()
        {
            var maxLineCount = 10000;
            var vertexLayout = new VertexLayout(VertexElementType.Float3, VertexElementType.Float3);

            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(Vector3)) * 2 * (maxLineCount * 2));
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(uint)) * maxLineCount * 2);
            
            return new GeometryPacket(vertexLayout, vertexBuffer, indexBuffer);
        }

        private void ClearDebugLines()
        {
            this.currentDebugLineIndex = 0;
        }

        public void DrawDebugLine(Vector3 point1, Vector3 point2)
        {
            this.debugVertexData[this.currentDebugLineIndex * 4] = point1;
            this.debugVertexData[this.currentDebugLineIndex * 4 + 1] = Vector3.Zero;
            this.debugVertexData[this.currentDebugLineIndex * 4 + 2] = point2;
            this.debugVertexData[this.currentDebugLineIndex * 4 + 3] = Vector3.Zero;

            this.debugIndexData[this.currentDebugLineIndex * 2] = (uint)this.currentDebugLineIndex * 2;
            this.debugIndexData[this.currentDebugLineIndex * 2 + 1] = (uint)this.currentDebugLineIndex * 2 + 1;

            this.currentDebugLineIndex++;
        }

        public void Render()
        {
            var copyCommandList = this.graphicsManager.CreateCopyCommandList();
            this.graphicsManager.UploadDataToGraphicsBuffer<Vector3>(copyCommandList, this.debugGeometryPacket.VertexBuffer, this.debugVertexData);
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.debugGeometryPacket.IndexBuffer, this.debugIndexData);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            var renderCommandList = this.graphicsManager.CreateRenderCommandList();
            this.graphicsManager.DrawPrimitives(renderCommandList, GeometryPrimitiveType.Line, 0, (uint)this.currentDebugLineIndex * 2, this.debugGeometryPacket.VertexBuffer, debugGeometryPacket.IndexBuffer, 0);
            this.graphicsManager.ExecuteRenderCommandList(renderCommandList);
        }
    }
}
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
        private GeometryPacket debugGeometryPacket;
        private int currentDebugLineIndex;
        private Vector3[] debugVertexData;
        private uint[] debugIndexData;

        public DebugRenderer(GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            // TODO: Refactor code like in Graphics2DRenderer

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager;

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/DebugRender.shader");

            var maxLineCount = 10000;
            this.debugVertexData = new Vector3[maxLineCount * 4];
            this.debugIndexData = new uint[maxLineCount * 2];
            this.debugGeometryPacket = SetupDebugGeometryPacket();
            this.currentDebugLineIndex = 0;
        }

        private GeometryPacket SetupDebugGeometryPacket()
        {
            var maxLineCount = 10000;
            var vertexLayout = new VertexLayout(VertexElementType.Float3, VertexElementType.Float3);

            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector3>(2 * (maxLineCount * 2), GraphicsResourceType.Dynamic);
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(maxLineCount * 2, GraphicsResourceType.Dynamic);
            
            return new GeometryPacket(vertexLayout, vertexBuffer, indexBuffer);
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
            this.debugVertexData[this.currentDebugLineIndex * 4] = point1;
            this.debugVertexData[this.currentDebugLineIndex * 4 + 1] = color;
            this.debugVertexData[this.currentDebugLineIndex * 4 + 2] = point2;
            this.debugVertexData[this.currentDebugLineIndex * 4 + 3] = color;

            this.debugIndexData[this.currentDebugLineIndex * 2] = (uint)this.currentDebugLineIndex * 2;
            this.debugIndexData[this.currentDebugLineIndex * 2 + 1] = (uint)this.currentDebugLineIndex * 2 + 1;

            this.currentDebugLineIndex++;
        }

        public void Render(CommandList? renderCommandList = null)
        {
            if (this.currentDebugLineIndex > 0)
            {
                var copyCommandList = this.graphicsManager.CreateCopyCommandList();
                this.graphicsManager.UploadDataToGraphicsBuffer<Vector3>(copyCommandList, this.debugGeometryPacket.VertexBuffer, this.debugVertexData);
                this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.debugGeometryPacket.IndexBuffer, this.debugIndexData);
                this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

                CommandList commandList;

                if (renderCommandList == null)
                {
                    commandList = this.graphicsManager.CreateRenderCommandList();
                }

                else
                {
                    commandList = renderCommandList.Value;
                }

                this.graphicsManager.SetShader(commandList, this.shader);

                var geometryInstance = new GeometryInstance(this.debugGeometryPacket, new Material(), 0, this.currentDebugLineIndex * 2, new BoundingBox(), GeometryPrimitiveType.Line);
                this.graphicsManager.DrawGeometryInstances(commandList, geometryInstance, 1, 0);
                
                if (renderCommandList == null)
                {
                    this.graphicsManager.ExecuteRenderCommandList(commandList);
                }
            }
        }
    }
}
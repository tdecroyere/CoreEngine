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

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/DebugRender.shader");

            var maxLineCount = 10000;
            this.vertexData = new Vector4[maxLineCount * 4];
            this.indexData = new uint[maxLineCount * 2];

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Vector4>(maxLineCount * 4, GraphicsResourceType.Dynamic, "DebugVertexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(maxLineCount * 2, GraphicsResourceType.Dynamic, "DebugIndexBuffer");

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

        public void CopyDataToGpu()
        {
            if (this.currentDebugLineIndex > 0)
            {
                var copyCommandList = this.graphicsManager.CreateCopyCommandList("DebugCopyCommandList");
                this.graphicsManager.UploadDataToGraphicsBuffer<Vector4>(copyCommandList, this.vertexBuffer, this.vertexData);
                this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, this.indexData);
                this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
            }
        }

        public void Render(GraphicsBuffer renderPassParametersGraphicsBuffer, CommandList? renderCommandList = null)
        {
            if (this.currentDebugLineIndex > 0)
            {
                CommandList commandList;

                if (renderCommandList == null)
                {
                    commandList = this.graphicsManager.CreateRenderCommandList("DebugRenderCommandList");
                }

                else
                {
                    commandList = renderCommandList.Value;
                }

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetShaderBuffer(commandList, this.vertexBuffer, 0);
                this.graphicsManager.SetShaderBuffer(commandList, renderPassParametersGraphicsBuffer, 1);

                this.graphicsManager.SetIndexBuffer(commandList, this.indexBuffer);
                this.graphicsManager.DrawIndexedPrimitives(commandList, GeometryPrimitiveType.Line, 0, this.currentDebugLineIndex * 2, 1, 0);
                
                if (renderCommandList == null)
                {
                    this.graphicsManager.ExecuteRenderCommandList(commandList);
                }
            }
        }
    }
}
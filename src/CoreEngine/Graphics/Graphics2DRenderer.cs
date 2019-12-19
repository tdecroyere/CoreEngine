using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CoreEngine.Diagnostics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    readonly struct Graphics2DVertex
    {
        public Graphics2DVertex(Vector2 position, Vector2 textureCoordinates)
        {
            this.Position = position;
            this.TextureCoordinates = textureCoordinates;
        }

        public readonly Vector2 Position { get; }
        public readonly Vector2 TextureCoordinates { get; }
    }

    readonly struct RenderPassConstants2D
    {
        public RenderPassConstants2D(Matrix4x4 projectionMatrix)
        {
            this.ProjectionMatrix = projectionMatrix;
        }

        public readonly Matrix4x4 ProjectionMatrix { get; }
    }

    // TODO: Find a way to auto align fields to 16 (Required by shaders)
    readonly struct RectangleSurface
    {
        public RectangleSurface(Matrix4x4 worldMatrix, Vector2 textureMinPoint, Vector2 textureMaxPoint, uint textureIndex)
        {
            this.WorldMatrix = worldMatrix;
            this.TextureMinPoint = textureMinPoint;
            this.TextureMaxPoint = textureMaxPoint;
            this.TextureIndex = textureIndex;
            this.Reserved = 1;
            this.Reserved2 = 2;
            this.Reserved3 = 3;
        }

        public readonly Matrix4x4 WorldMatrix { get; }
        public readonly Vector2 TextureMinPoint { get; }
        public readonly Vector2 TextureMaxPoint { get; }
        public readonly uint TextureIndex { get; }
        public readonly uint Reserved { get; }
        public readonly uint Reserved2 { get; }
        public readonly uint Reserved3 { get; }
    }

    public class Graphics2DRenderer : SystemManager
    {
        private readonly GraphicsManager graphicsManager;
        
        private int currentSurfaceCount;
        private float scaleFactor = 1.0f;

        private Shader shader;
        private Font systemFont;

        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer indexBuffer;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer rectangleSurfacesGraphicsBuffer;

        private RenderPassConstants2D renderPassConstants;
        private RectangleSurface[] rectangleSurfaces;

        private List<Texture> textures;

        public Graphics2DRenderer(GraphicsManager graphicsManager, ResourcesManager resourcesManager)
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

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/Graphics2DRender.shader");
            this.systemFont = resourcesManager.LoadResourceAsync<Font>("/SystemFont.font");
            this.textures = new List<Texture>();

            var maxSurfaceCount = 10000;
            this.rectangleSurfaces = new RectangleSurface[maxSurfaceCount];

            // TODO: Use a compute shader to compute vertex and index buffer because UVs are computed on the fly
            var vertexData = new Graphics2DVertex[4];
            var indexData = new uint[6];

            vertexData[0] = new Graphics2DVertex(new Vector2(0, 0), new Vector2(0, 0));
            vertexData[1] = new Graphics2DVertex(new Vector2(1, 0), new Vector2(1, 0));
            vertexData[2] = new Graphics2DVertex(new Vector2(0, 1), new Vector2(0, 1));
            vertexData[3] = new Graphics2DVertex(new Vector2(1, 1), new Vector2(1, 1));

            indexData[0] = 0;
            indexData[1] = 1;
            indexData[2] = 2;
            indexData[3] = 2;
            indexData[4] = 1;
            indexData[5] = 3;

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Graphics2DVertex>(vertexData.Length, GraphicsResourceType.Static, "Graphics2DVertexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(indexData.Length, GraphicsResourceType.Static, "Graphics2DIndexBuffer");

            var copyCommandList = this.graphicsManager.CreateCopyCommandList();
            this.graphicsManager.UploadDataToGraphicsBuffer<Graphics2DVertex>(copyCommandList, this.vertexBuffer, vertexData);
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, indexData);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants2D>(1, GraphicsResourceType.Dynamic, "Graphics2DRenderPassBuffer");
            this.rectangleSurfacesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RectangleSurface>(maxSurfaceCount, GraphicsResourceType.Dynamic, "Graphics2DRectanbleSurfacesBuffer");
        }

        public override void PreUpdate()
        {
            this.textures.Clear();
            var renderSize = this.graphicsManager.GetRenderSize();

            this.currentSurfaceCount = 0;
            this.renderPassConstants = new RenderPassConstants2D(MathUtils.CreateOrthographicMatrixOffCenter(0, renderSize.X, 0, renderSize.Y, 0, 1));
        }

        public void DrawRectangleTexture(Vector2 position, Texture texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }
            
            DrawRectangleSurface(position, position + new Vector2(texture.Width, texture.Height), texture);
        }

        public void DrawText(string text, Vector2 position, Font? font = null)
        {
            if (text == null)
            {
                return;
            }

            if (font == null)
            {
                font = this.systemFont;
            }

            for (var i = 0; i < text.Length; i++)
            {
                var glyphInfo = font.GlyphInfos[text[i]];

                DrawRectangleSurface(position, position + new Vector2(glyphInfo.Width, glyphInfo.Height), font.Texture, glyphInfo.TextureMinPoint, glyphInfo.TextureMaxPoint);
                position.X += glyphInfo.Width;
            }
        }

        public void DrawRectangleSurface(Vector2 minPoint, Vector2 maxPoint, Texture texture)
        {
            DrawRectangleSurface(minPoint, maxPoint, texture, Vector2.Zero, new Vector2(1, 1));
        }

        public void DrawRectangleSurface(Vector2 minPoint, Vector2 maxPoint, Texture texture, Vector2 textureMinPoint, Vector2 textureMaxPoint)
        {
            var vertexOffset = this.currentSurfaceCount * 4;
            var indexOffset = this.currentSurfaceCount * 6;

            minPoint *= this.scaleFactor;
            maxPoint *= this.scaleFactor;

            var size = maxPoint - minPoint;
            var worldMatrix = Matrix4x4.CreateScale(new Vector3(size, 0)) * Matrix4x4.CreateTranslation(new Vector3(minPoint, 0));

            var textureIndex = this.textures.IndexOf(texture);

            if (textureIndex == -1)
            {
                textureIndex = this.textures.Count;
                this.textures.Add(texture);
            }

            this.rectangleSurfaces[this.currentSurfaceCount] = new RectangleSurface(worldMatrix, textureMinPoint, textureMaxPoint, (uint)textureIndex);
            this.currentSurfaceCount++;
        }

        public void CopyDataToGpu()
        {
            if (this.currentSurfaceCount > 0)
            {
                var copyCommandList = this.graphicsManager.CreateCopyCommandList("Graphics2DCopyCommandList");
                this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants2D>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants2D[] {renderPassConstants});
                this.graphicsManager.UploadDataToGraphicsBuffer<RectangleSurface>(copyCommandList, this.rectangleSurfacesGraphicsBuffer, this.rectangleSurfaces);
                this.graphicsManager.ExecuteCopyCommandList(copyCommandList);
            }
        }

        public void Render(CommandList? renderCommandList = null)
        {
            if (this.currentSurfaceCount > 0)
            {
                CommandList commandList;

                if (renderCommandList == null)
                {
                    var renderPassDescriptor = new RenderPassDescriptor(this.graphicsManager.MainRenderTargetTexture, null, null, false, false, true);
                    commandList = this.graphicsManager.CreateRenderCommandList(renderPassDescriptor, "Graphics2DRenderCommandList");
                }

                else
                {
                    commandList = renderCommandList.Value;
                }

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetShaderBuffer(commandList, this.vertexBuffer, 0);
                this.graphicsManager.SetShaderBuffer(commandList, this.renderPassParametersGraphicsBuffer, 1);
                this.graphicsManager.SetShaderBuffer(commandList, this.rectangleSurfacesGraphicsBuffer, 2);
                this.graphicsManager.SetShaderTextures(commandList, this.textures.ToArray(), 3);

                this.graphicsManager.SetIndexBuffer(commandList, this.indexBuffer);
                this.graphicsManager.DrawIndexedPrimitives(commandList, GeometryPrimitiveType.Triangle, 0, 6, this.currentSurfaceCount, 0);

                if (renderCommandList == null)
                {
                    this.graphicsManager.ExecuteRenderCommandList(commandList);
                }
            }
        }
    }
}
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
        public RectangleSurface(Matrix4x4 worldMatrix, uint textureIndex)
        {
            this.WorldMatrix = worldMatrix;
            this.TextureIndex = textureIndex;
            this.Reserved = 1;
            this.Reserved2 = 2;
            this.Reserved3 = 3;
        }

        public readonly Matrix4x4 WorldMatrix { get; }
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

        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer indexBuffer;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer rectangleSurfacesGraphicsBuffer;

        private RenderPassConstants2D renderPassConstants;
        private RectangleSurface[] rectangleSurfaces;

        private Texture? testTexture;
        private Texture? testTexture2;

        private Texture[] textures;

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
            this.testTexture = resourcesManager.LoadResourceAsync<Texture>("/pokemon.texture");
            this.testTexture2 = resourcesManager.LoadResourceAsync<Texture>("/pokemon2.texture");

            this.textures = new Texture[]
            {
                this.testTexture,
                this.testTexture2
            };

            var maxSurfaceCount = 10000;
            this.rectangleSurfaces = new RectangleSurface[maxSurfaceCount];

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

        // TODO: Use an argument buffer to store the texture reference list in gpu memory
        public void DrawRectangleSurface(Vector2 minPoint, Vector2 maxPoint, Texture texture)
        {
            var vertexOffset = this.currentSurfaceCount * 4;
            var indexOffset = this.currentSurfaceCount * 6;

            minPoint *= this.scaleFactor;
            maxPoint *= this.scaleFactor;

            var size = maxPoint - minPoint;
            var worldMatrix = Matrix4x4.CreateScale(new Vector3(size, 0)) * Matrix4x4.CreateTranslation(new Vector3(minPoint, 0));

            var list = new List<Texture>(this.textures);
            var textureIndex = (uint)list.IndexOf(texture);

            this.rectangleSurfaces[this.currentSurfaceCount] = new RectangleSurface(worldMatrix, (uint)textureIndex);
            this.currentSurfaceCount++;
        }

        public void Render()
        {
            // TODO: Disable depth test

            if (this.testTexture != null)
                this.DrawRectangleTexture(new Vector2(0, 0), testTexture);

            if (this.testTexture2 != null)
            {
                this.DrawRectangleTexture(new Vector2(2000, 100), testTexture2);
                this.DrawRectangleTexture(new Vector2(1000, 1000), testTexture2);
            }

            if (this.currentSurfaceCount > 0)
            {
                var copyCommandList = this.graphicsManager.CreateCopyCommandList("Graphics2DCopyCommandList");
                this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants2D>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants2D[] {renderPassConstants});
                this.graphicsManager.UploadDataToGraphicsBuffer<RectangleSurface>(copyCommandList, this.rectangleSurfacesGraphicsBuffer, this.rectangleSurfaces);
                this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

                var commandList = this.graphicsManager.CreateRenderCommandList("Graphics2DRenderCommandList");

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetShaderBuffer(commandList, this.vertexBuffer, 0);
                this.graphicsManager.SetShaderBuffer(commandList, this.renderPassParametersGraphicsBuffer, 1);
                this.graphicsManager.SetShaderBuffer(commandList, this.rectangleSurfacesGraphicsBuffer, 2);
                this.graphicsManager.SetShaderTextures(commandList, this.textures, 3);

                this.graphicsManager.SetIndexBuffer(commandList, this.indexBuffer);
                this.graphicsManager.DrawIndexedPrimitives(commandList, GeometryPrimitiveType.Triangle, 0, 6, this.currentSurfaceCount, 0);

                this.graphicsManager.ExecuteRenderCommandList(commandList);
            }
        }
    }
}
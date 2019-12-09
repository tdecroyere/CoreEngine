using System;
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
            this.Position = new Vector3(position, 0);
            this.TextureCoordinates = new Vector3(textureCoordinates, 0);
        }

        public readonly Vector3 Position { get; }
        public readonly Vector3 TextureCoordinates { get; }
    }

    readonly struct RenderPassConstants2D
    {
        public RenderPassConstants2D(Matrix4x4 projectionMatrix)
        {
            this.ProjectionMatrix = projectionMatrix;
        }

        public readonly Matrix4x4 ProjectionMatrix { get; }
    }

    readonly struct SurfaceProperties
    {
        public SurfaceProperties(Matrix4x4 worldMatrix, Texture texture)
        {
            this.WorldMatrix = worldMatrix;
            // this.TextureId = texture.TextureId;
        }

        public readonly Matrix4x4 WorldMatrix { get; }
        // public readonly uint TextureId { get; }
    }

    public class Graphics2DRenderer : SystemManager
    {
        private readonly GraphicsManager graphicsManager;

        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer indexBuffer;
        private Shader shader;
        private int currentSurfaceCount;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants2D renderPassConstants;

        private GraphicsBuffer surfacePropertiesGraphicsBuffer;
        private SurfaceProperties[] surfaceProperties;

        private float scaleFactor = 1.0f;

        private Texture? testTexture;
        private Texture? testTexture2;

        public Graphics2DRenderer(GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager;

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/Graphics2DRender.shader");
            this.testTexture = resourcesManager.LoadResourceAsync<Texture>("/pokemon.texture");
            this.testTexture2 = resourcesManager.LoadResourceAsync<Texture>("/pokemon2.texture");

            var maxSurfaceCount = 10000;
            this.surfaceProperties = new SurfaceProperties[maxSurfaceCount];

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

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Graphics2DVertex>(vertexData.Length);
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(indexData.Length);
            
            var copyCommandList = this.graphicsManager.CreateCopyCommandList();
            this.graphicsManager.UploadDataToGraphicsBuffer<Graphics2DVertex>(copyCommandList, this.vertexBuffer, vertexData);
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, indexData);
            this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants2D>(1, GraphicsResourceType.Dynamic);
            this.surfacePropertiesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<SurfaceProperties>(maxSurfaceCount, GraphicsResourceType.Dynamic);
        }

        public override void PreUpdate()
        {
            var renderSize = this.graphicsManager.GetRenderSize();

            this.currentSurfaceCount = 0;
            this.renderPassConstants = new RenderPassConstants2D(MathUtils.CreateOrthographicMatrixOffCenter(0, renderSize.X, 0, renderSize.Y, 0, 1));
        }

        public void DrawRectangleTexture(Vector2 position, Texture texture)
        {
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

            this.surfaceProperties[this.currentSurfaceCount] = new SurfaceProperties(worldMatrix, texture);

            // this.vertexData[vertexOffset] = new Graphics2DVertex(new Vector2(minPoint.X, minPoint.Y), new Vector2(0, 0));
            // this.vertexData[vertexOffset + 1] = new Graphics2DVertex(new Vector2(maxPoint.X, minPoint.Y), new Vector2(1, 0));
            // this.vertexData[vertexOffset + 2] = new Graphics2DVertex(new Vector2(minPoint.X, maxPoint.Y), new Vector2(0, 1));
            // this.vertexData[vertexOffset + 3] = new Graphics2DVertex(new Vector2(maxPoint.X, maxPoint.Y), new Vector2(1, 1));

            // this.indexData[indexOffset] = (uint)vertexOffset;
            // this.indexData[indexOffset + 1] = (uint)vertexOffset + 1;
            // this.indexData[indexOffset + 2] = (uint)vertexOffset + 2;
            // this.indexData[indexOffset + 3] = (uint)vertexOffset + 2;
            // this.indexData[indexOffset + 4] = (uint)vertexOffset + 1;
            // this.indexData[indexOffset + 5] = (uint)vertexOffset + 3;

            this.currentSurfaceCount++;
        }

        public void Render()
        {
            // TODO: Disable depth test

            if (this.testTexture != null)
                this.DrawRectangleTexture(new Vector2(0, 0), testTexture);

            if (this.testTexture2 != null)
                this.DrawRectangleTexture(new Vector2(2000, 100), testTexture2);

            if (this.currentSurfaceCount > 0)
            {
                var copyCommandList = this.graphicsManager.CreateCopyCommandList();
                this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants2D>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants2D[] {renderPassConstants});
                this.graphicsManager.UploadDataToGraphicsBuffer<SurfaceProperties>(copyCommandList, this.surfacePropertiesGraphicsBuffer, this.surfaceProperties);
                this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

                var commandList = this.graphicsManager.CreateRenderCommandList();

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetGraphicsBuffer(commandList, this.renderPassParametersGraphicsBuffer, GraphicsBindStage.Vertex, 1);
                this.graphicsManager.SetGraphicsBuffer(commandList, this.surfacePropertiesGraphicsBuffer, GraphicsBindStage.Vertex, 2);

                if (this.testTexture != null)
                {
                    this.graphicsManager.SetTexture(commandList, this.testTexture, GraphicsBindStage.Pixel, 1);
                }

                this.graphicsManager.SetGraphicsBuffer(commandList, this.surfacePropertiesGraphicsBuffer, GraphicsBindStage.Pixel, 2);

                this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.Triangle, 0, 6, this.vertexBuffer, this.indexBuffer, 2, 0);

                this.graphicsManager.ExecuteRenderCommandList(commandList);
            }
        }
    }
}
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

    public class Graphics2DRenderer : SystemManager
    {
        private readonly GraphicsManager graphicsManager;

        private Shader shader;
        private GeometryPacket geometryPacket;
        private int currentSurfaceIndex;
        private Graphics2DVertex[] vertexData;
        private uint[] indexData;

        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private RenderPassConstants2D renderPassConstants;

        private float scaleFactor = 1.0f;

        private Texture? testTexture;

        public Graphics2DRenderer(GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.graphicsManager = graphicsManager;

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/Graphics2DRender.shader");
            this.testTexture = resourcesManager.LoadResourceAsync<Texture>("/pokemon.texture");

            var vertexLayout = new VertexLayout(VertexElementType.Float3, VertexElementType.Float2);

            var maxSurfaceCount = 10000;
            this.vertexData = new Graphics2DVertex[maxSurfaceCount * 4];
            this.indexData = new uint[maxSurfaceCount * 6];

            var vertexBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(Graphics2DVertex)) * (maxSurfaceCount * 4), GraphicsResourceType.Dynamic);
            var indexBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(uint)) * maxSurfaceCount * 6, GraphicsResourceType.Dynamic);
            
            this.geometryPacket = new GeometryPacket(vertexLayout, vertexBuffer, indexBuffer);

            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer(Marshal.SizeOf(typeof(RenderPassConstants2D)), GraphicsResourceType.Dynamic);
        }

        public override void PreUpdate()
        {
            var renderSize = this.graphicsManager.GetRenderSize();

            this.currentSurfaceIndex = 0;
            this.renderPassConstants = new RenderPassConstants2D(MathUtils.CreateOrthographicMatrixOffCenter(0, renderSize.X, 0, renderSize.Y, 0, 1));
        }

        // TODO: Use an argument buffer to store the texture reference list in gpu memory
        public void DrawRectangleSurface(Vector2 minPoint, Vector2 maxPoint, Texture texture)
        {
            this.testTexture = texture;

            var vertexOffset = this.currentSurfaceIndex * 4;
            var indexOffset = this.currentSurfaceIndex * 6;

            minPoint *= this.scaleFactor;
            maxPoint *= this.scaleFactor;

            this.vertexData[vertexOffset] = new Graphics2DVertex(new Vector2(minPoint.X, minPoint.Y), new Vector2(0, 0));
            this.vertexData[vertexOffset + 1] = new Graphics2DVertex(new Vector2(maxPoint.X, minPoint.Y), new Vector2(1, 0));
            this.vertexData[vertexOffset + 2] = new Graphics2DVertex(new Vector2(minPoint.X, maxPoint.Y), new Vector2(0, 1));
            this.vertexData[vertexOffset + 3] = new Graphics2DVertex(new Vector2(maxPoint.X, maxPoint.Y), new Vector2(1, 1));

            this.indexData[indexOffset] = (uint)vertexOffset;
            this.indexData[indexOffset + 1] = (uint)vertexOffset + 1;
            this.indexData[indexOffset + 2] = (uint)vertexOffset + 2;
            this.indexData[indexOffset + 3] = (uint)vertexOffset + 2;
            this.indexData[indexOffset + 4] = (uint)vertexOffset + 1;
            this.indexData[indexOffset + 5] = (uint)vertexOffset + 3;

            this.currentSurfaceIndex++;
        }

        public void Render()
        {
            if (this.testTexture != null)
                this.DrawRectangleSurface(new Vector2(100, 100), new Vector2(testTexture.Width, testTexture.Height), testTexture);

            if (this.currentSurfaceIndex > 0)
            {
                var copyCommandList = this.graphicsManager.CreateCopyCommandList();
                this.graphicsManager.UploadDataToGraphicsBuffer<Graphics2DVertex>(copyCommandList, this.geometryPacket.VertexBuffer, this.vertexData);
                this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.geometryPacket.IndexBuffer, this.indexData);
                this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants2D>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants2D[] {renderPassConstants});
                this.graphicsManager.ExecuteCopyCommandList(copyCommandList);

                var commandList = this.graphicsManager.CreateRenderCommandList();

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetGraphicsBuffer(commandList, this.renderPassParametersGraphicsBuffer, GraphicsBindStage.Vertex, 1);

                if (this.testTexture != null)
                {
                    this.graphicsManager.SetTexture(commandList, this.testTexture, GraphicsBindStage.Pixel, 1);
                }

                var geometryInstance = new GeometryInstance(this.geometryPacket, new Material(), 0, (uint)this.currentSurfaceIndex * 6, new BoundingBox());
                this.graphicsManager.DrawPrimitives(commandList, geometryInstance, 0);

                this.graphicsManager.ExecuteRenderCommandList(commandList);
            }
        }
    }
}
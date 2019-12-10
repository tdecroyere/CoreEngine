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

    // TODO: Find a way to auto align fields to 16 (Required by shaders)
    readonly struct SurfaceProperties
    {
        public SurfaceProperties(Matrix4x4 worldMatrix, uint textureIndex)
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

    // class ShaderInputParameters
    // {
    //     public IList<SurfaceProperties> SurfaceProperties { get; }
    //     public IList<Texture> Textures { get; }
    // }

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

        private IList<Texture> textures;

        private GraphicsBuffer vertexShaderParameters;

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
            
            textures = new Texture[]
            {
                this.testTexture,
                this.testTexture2
            };

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

            var shaderParameterDescriptors = new ShaderParameterDescriptor[]
            {
                new ShaderParameterDescriptor(this.surfacePropertiesGraphicsBuffer, ShaderParameterType.Buffer, 0),
                new ShaderParameterDescriptor(new IGraphicsResource[] { this.testTexture, this.testTexture2 }, ShaderParameterType.Texture, 1)
            };

            // TODO: Store the shader parameter descriptors in the shader file so that we can have
            this.vertexShaderParameters = this.graphicsManager.CreateShaderParameters(this.shader, 2, shaderParameterDescriptors);
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

            var textureIndex = this.textures.IndexOf(texture);
            this.surfaceProperties[this.currentSurfaceCount] = new SurfaceProperties(worldMatrix, (uint)textureIndex);
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
                this.graphicsManager.SetGraphicsBuffer(commandList, this.renderPassParametersGraphicsBuffer, ShaderBindStage.Vertex, 1);
                this.graphicsManager.SetGraphicsBuffer(commandList, this.vertexShaderParameters, ShaderBindStage.Vertex, 2);
                this.graphicsManager.SetGraphicsBuffer(commandList, this.vertexShaderParameters, ShaderBindStage.Pixel, 1);

                this.graphicsManager.DrawPrimitives(commandList, GeometryPrimitiveType.Triangle, 0, 6, this.vertexBuffer, this.indexBuffer, 2, 0);

                this.graphicsManager.ExecuteRenderCommandList(commandList);
            }
        }
    }
}
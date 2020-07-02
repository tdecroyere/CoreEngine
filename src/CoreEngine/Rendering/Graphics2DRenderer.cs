using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
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
        public RectangleSurface(Matrix4x4 worldMatrix, Vector2 textureMinPoint, Vector2 textureMaxPoint, uint textureIndex, bool isOpaque)
        {
            this.WorldMatrix = worldMatrix;
            this.TextureMinPoint = textureMinPoint;
            this.TextureMaxPoint = textureMaxPoint;
            this.TextureIndex = textureIndex;
            this.IsOpaque = isOpaque;
            this.Reserved1 = 0;
            this.Reserved2 = 0;
            this.Reserved3 = 0;
            this.Reserved4 = 0;
            this.Reserved5 = 0;
        }

        public readonly Matrix4x4 WorldMatrix { get; }
        public readonly Vector2 TextureMinPoint { get; }
        public readonly Vector2 TextureMaxPoint { get; }
        public readonly uint TextureIndex { get; }
        public readonly bool IsOpaque { get; }
        public readonly byte Reserved1 { get; }
        public readonly byte Reserved2 { get; }
        public readonly byte Reserved3 { get; }
        public readonly uint Reserved4 { get; }
        public readonly uint Reserved5 { get; }
    }

    public class Graphics2DRenderer : SystemManager
    {
        private readonly GraphicsManager graphicsManager;
        private readonly RenderManager renderManager;
        
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
        public CommandBuffer copyCommandBuffer;
        public CommandBuffer commandBuffer;

        public Graphics2DRenderer(RenderManager renderManager, GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            if (renderManager == null)
            {
                throw new ArgumentNullException(nameof(renderManager));
            }

            if (graphicsManager == null)
            {
                throw new ArgumentNullException(nameof(graphicsManager));
            }

            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.renderManager = renderManager;
            this.graphicsManager = graphicsManager;

            this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/Graphics2DRender.shader");
            this.systemFont = resourcesManager.LoadResourceAsync<Font>("/System/Fonts/SystemFont.font");
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

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Graphics2DVertex>(vertexData.Length, isStatic: true, isWriteOnly: true, label: "Graphics2DVertexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(indexData.Length, isStatic: true, isWriteOnly: true, label: "Graphics2DIndexBuffer");

            var commandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "Graphics2DRenderer");
            this.graphicsManager.ResetCommandBuffer(commandBuffer);
            var copyCommandList = this.graphicsManager.CreateCopyCommandList(commandBuffer, "Graphics2DRendererCommandList");
            this.graphicsManager.UploadDataToGraphicsBuffer<Graphics2DVertex>(copyCommandList, this.vertexBuffer, vertexData);
            this.graphicsManager.UploadDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, indexData);
            this.graphicsManager.CommitCopyCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandBuffer(commandBuffer);
            this.graphicsManager.DeleteCommandBuffer(commandBuffer);

            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants2D>(1, isStatic: false, isWriteOnly: true, label: "Graphics2DRenderPassBuffer");
            this.rectangleSurfacesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RectangleSurface>(maxSurfaceCount, isStatic: false, isWriteOnly: true, label: "Graphics2DRectanbleSurfacesBuffer");

            this.copyCommandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Copy, "Graphics2DRendererCopy");
            this.commandBuffer = this.graphicsManager.CreateCommandBuffer(CommandListType.Render, "Graphics2DRenderer");
        }

        public override void PreUpdate()
        {
            this.textures.Clear();
            this.currentSurfaceCount = 0;
            
            var renderSize = this.graphicsManager.GetRenderSize();
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
                if (!font.GlyphInfos.ContainsKey(text[i]))
                {
                    continue;
                }
                
                var glyphInfo = font.GlyphInfos[text[i]];

                DrawRectangleSurface(position, position + new Vector2(glyphInfo.Width, glyphInfo.Height), font.Texture, glyphInfo.TextureMinPoint, glyphInfo.TextureMaxPoint, false);
                position.X += glyphInfo.Width;
            }
        }

        public void DrawRectangleSurface(Vector2 minPoint, Vector2 maxPoint, Texture texture, bool isOpaque = false)
        {
            DrawRectangleSurface(minPoint, maxPoint, texture, Vector2.Zero, new Vector2(1, 1), isOpaque);
        }

        public void DrawRectangleSurface(Vector2 minPoint, Vector2 maxPoint, Texture texture, Vector2 textureMinPoint, Vector2 textureMaxPoint, bool isOpaque)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }
            
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

            this.rectangleSurfaces[this.currentSurfaceCount] = new RectangleSurface(worldMatrix, textureMinPoint, textureMaxPoint, (uint)textureIndex, isOpaque);
            this.currentSurfaceCount++;
        }

        public CommandList Render(CommandList previousCommandList)
        {
            if (this.currentSurfaceCount > 0)
            {
                this.graphicsManager.ResetCommandBuffer(copyCommandBuffer);

                var copyCommandList = this.graphicsManager.CreateCopyCommandList(copyCommandBuffer, "Graphics2DCopyCommandList");
                this.graphicsManager.UploadDataToGraphicsBuffer<RenderPassConstants2D>(copyCommandList, this.renderPassParametersGraphicsBuffer, new RenderPassConstants2D[] {renderPassConstants});
                this.graphicsManager.UploadDataToGraphicsBuffer<RectangleSurface>(copyCommandList, this.rectangleSurfacesGraphicsBuffer, this.rectangleSurfaces.AsSpan().Slice(0, this.currentSurfaceCount));
                this.graphicsManager.CommitCopyCommandList(copyCommandList);
                this.graphicsManager.ExecuteCommandBuffer(copyCommandBuffer);

                this.graphicsManager.ResetCommandBuffer(commandBuffer);
                var renderTarget = new RenderTargetDescriptor(this.renderManager.MainRenderTargetTexture, null, BlendOperation.AlphaBlending);
                var renderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true);
                var commandList = this.graphicsManager.CreateRenderCommandList(commandBuffer, renderPassDescriptor, "Graphics2DRenderCommandList");

                this.graphicsManager.WaitForCommandList(commandList, copyCommandList);
                this.graphicsManager.WaitForCommandList(commandList, previousCommandList);

                this.graphicsManager.SetShader(commandList, this.shader);
                this.graphicsManager.SetShaderBuffer(commandList, this.vertexBuffer, 0);
                this.graphicsManager.SetShaderBuffer(commandList, this.renderPassParametersGraphicsBuffer, 1);
                this.graphicsManager.SetShaderBuffer(commandList, this.rectangleSurfacesGraphicsBuffer, 2);
                this.graphicsManager.SetShaderTextures(commandList, this.textures.ToArray(), 3);

                this.graphicsManager.SetIndexBuffer(commandList, this.indexBuffer);
                this.graphicsManager.DrawIndexedPrimitives(commandList, PrimitiveType.Triangle, 0, 6, this.currentSurfaceCount, 0);

                this.graphicsManager.CommitRenderCommandList(commandList);

                this.graphicsManager.ExecuteCommandBuffer(commandBuffer);

                return commandList;
            }

            return previousCommandList;
        }
    }
}
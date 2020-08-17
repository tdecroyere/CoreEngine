using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Diagnostics;
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
        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;
        
        private int currentSurfaceCount;
        private float scaleFactor = 1.0f;

        private Shader shader;
        private Font systemFont;

        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer indexBuffer;

        private GraphicsBuffer cpuRenderPassParametersGraphicsBuffer;
        private GraphicsBuffer renderPassParametersGraphicsBuffer;
        private GraphicsBuffer cpuRectangleSurfacesGraphicsBuffer;
        private GraphicsBuffer rectangleSurfacesGraphicsBuffer;

        private List<Texture> textures;
        public CommandList copyCommandList { get; }
        public CommandList renderCommandList { get; }

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

            var cpuVertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Graphics2DVertex>(GraphicsHeapType.Upload, 4, isStatic: true, label: "CpuGraphics2DVertexBuffer");
            var cpuIndexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Upload, 6, isStatic: true, label: "CpuGraphics2DIndexBuffer");

            var vertexData = this.graphicsManager.GetCpuGraphicsBufferPointer<Graphics2DVertex>(cpuVertexBuffer);
            var indexData = this.graphicsManager.GetCpuGraphicsBufferPointer<uint>(cpuIndexBuffer);

            // TODO: Use a compute shader to compute vertex and index buffer because UVs are computed on the fly

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

            this.vertexBuffer = this.graphicsManager.CreateGraphicsBuffer<Graphics2DVertex>(GraphicsHeapType.Gpu, vertexData.Length, isStatic: true, label: "Graphics2DVertexBuffer");
            this.indexBuffer = this.graphicsManager.CreateGraphicsBuffer<uint>(GraphicsHeapType.Gpu, indexData.Length, isStatic: true, label: "Graphics2DIndexBuffer");

            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "Graphics2DRenderer");
            this.graphicsManager.ResetCommandList(copyCommandList);
            this.graphicsManager.CopyDataToGraphicsBuffer<Graphics2DVertex>(copyCommandList, this.vertexBuffer, cpuVertexBuffer, vertexData.Length);
            this.graphicsManager.CopyDataToGraphicsBuffer<uint>(copyCommandList, this.indexBuffer, cpuIndexBuffer, indexData.Length);
            this.graphicsManager.CommitCommandList(copyCommandList);
            this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList }, isAwaitable: false);
            this.graphicsManager.DeleteCommandList(copyCommandList);

            this.graphicsManager.DeleteGraphicsBuffer(cpuVertexBuffer);
            this.graphicsManager.DeleteGraphicsBuffer(cpuIndexBuffer);

            this.cpuRenderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants2D>(GraphicsHeapType.Upload, 1, isStatic: false, label: "Graphics2DRenderPassBuffer");
            this.renderPassParametersGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RenderPassConstants2D>(GraphicsHeapType.Gpu, 1, isStatic: false, label: "Graphics2DRenderPassBuffer");
            this.cpuRectangleSurfacesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RectangleSurface>(GraphicsHeapType.Upload, maxSurfaceCount, isStatic: false, label: "Graphics2DRectangleSurfacesBuffer");
            this.rectangleSurfacesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RectangleSurface>(GraphicsHeapType.Gpu, maxSurfaceCount, isStatic: false, label: "Graphics2DRectangleSurfacesBuffer");

            this.copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, "Graphics2DRendererCopy");
            this.renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, "Graphics2DRenderer");
        }

        public override void PreUpdate()
        {
            this.textures.Clear();
            this.currentSurfaceCount = 0;
            
            var renderSize = this.graphicsManager.GetRenderSize();

            var renderPassConstants = this.graphicsManager.GetCpuGraphicsBufferPointer<RenderPassConstants2D>(this.cpuRenderPassParametersGraphicsBuffer);
            renderPassConstants[0] = new RenderPassConstants2D(MathUtils.CreateOrthographicMatrixOffCenter(0, renderSize.X, 0, renderSize.Y, 0, 1));
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

            var rectangleSurfaces = this.graphicsManager.GetCpuGraphicsBufferPointer<RectangleSurface>(this.cpuRectangleSurfacesGraphicsBuffer);
            rectangleSurfaces[this.currentSurfaceCount] = new RectangleSurface(worldMatrix, textureMinPoint, textureMaxPoint, (uint)textureIndex, isOpaque);
            this.currentSurfaceCount++;
        }

        public void Render(Texture renderTargetTexture)
        {
            if (this.currentSurfaceCount > 0)
            {
                this.graphicsManager.ResetCommandList(this.copyCommandList);

                var startCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
                this.graphicsManager.CopyDataToGraphicsBuffer<RenderPassConstants2D>(copyCommandList, this.renderPassParametersGraphicsBuffer, this.cpuRenderPassParametersGraphicsBuffer, 1);
                this.graphicsManager.CopyDataToGraphicsBuffer<RectangleSurface>(copyCommandList, this.rectangleSurfacesGraphicsBuffer, this.cpuRectangleSurfacesGraphicsBuffer, this.currentSurfaceCount);
                var endCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
                this.graphicsManager.CommitCommandList(copyCommandList);
                var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList }, isAwaitable: true);

                this.graphicsManager.ResetCommandList(renderCommandList);
                // var renderTarget = new RenderTargetDescriptor(renderTargetTexture, null, BlendOperation.AlphaBlending);
                var renderTarget = new RenderTargetDescriptor(renderTargetTexture, Vector4.Zero, BlendOperation.AlphaBlending);
                var renderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, true, PrimitiveType.Triangle);

                this.graphicsManager.WaitForCommandQueue(this.renderManager.RenderCommandQueue, this.renderManager.CopyCommandQueue, copyFence);
                
                var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
                this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor);

                this.graphicsManager.SetShader(renderCommandList, this.shader);
                this.graphicsManager.SetShaderBuffer(renderCommandList, this.vertexBuffer, 0);
                this.graphicsManager.SetShaderBuffer(renderCommandList, this.renderPassParametersGraphicsBuffer, 1);
                this.graphicsManager.SetShaderBuffer(renderCommandList, this.rectangleSurfacesGraphicsBuffer, 2);
                this.graphicsManager.SetShaderTextures(renderCommandList, this.textures.ToArray(), 3);

                this.graphicsManager.SetIndexBuffer(renderCommandList, this.indexBuffer);

                // Don't use a vertex buffer and an index, use instances instead
                Logger.WriteMessage("Draw2D");
                this.graphicsManager.DrawIndexedPrimitives(renderCommandList, PrimitiveType.Triangle, 0, 6, this.currentSurfaceCount, 0);

                this.graphicsManager.EndRenderPass(this.renderCommandList);
                var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
                this.graphicsManager.CommitCommandList(this.renderCommandList);
                this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { renderCommandList }, isAwaitable: false);

                this.renderManager.AddGpuTiming("Graphics2DRendererCopy", startCopyQueryIndex, endCopyQueryIndex);
                this.renderManager.AddGpuTiming("Graphics2DRenderer", startQueryIndex, endQueryIndex);
            }
        }
    }
}
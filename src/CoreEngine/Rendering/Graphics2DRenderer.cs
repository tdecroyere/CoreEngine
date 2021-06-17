using System;
using System.Collections.Generic;
using System.Numerics;
using CoreEngine.Diagnostics;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Rendering
{
    // TODO: Find a way to auto align fields to 16 Bytes (Required by shaders)
    readonly struct RectangleSurface
    {
        public RectangleSurface(Matrix4x4 worldViewProjMatrix, Vector2 textureMinPoint, Vector2 textureMaxPoint, uint textureIndex, bool isOpaque)
        {
            this.WorldViewProjMatrix = worldViewProjMatrix;
            this.TextureMinPoint = textureMinPoint;
            this.TextureMaxPoint = textureMaxPoint;
            this.TextureIndex = textureIndex;
            this.IsOpaque = (uint)(isOpaque ? 1 : 0);
        }

        public readonly ShaderMatrix4x4 WorldViewProjMatrix { get; }
        public readonly Vector2 TextureMinPoint { get; }
        public readonly Vector2 TextureMaxPoint { get; }
        public readonly uint TextureIndex { get; }
        public readonly uint IsOpaque { get; }
    }

    public class Graphics2DRenderer : SystemManager
    {
        private const uint maxSurfaceCountPerThreadGroup = 16;  
        private const float scaleFactor = 1.0f;

        private readonly RenderManager renderManager;
        private readonly GraphicsManager graphicsManager;

        private readonly Shader shader;
        private readonly Font systemFont;
        private readonly GraphicsBuffer cpuRectangleSurfacesGraphicsBuffer;
        private readonly GraphicsBuffer rectangleSurfacesGraphicsBuffer;
        private readonly RectangleSurface[] rectangleSurfaces;

        private int currentSurfaceCount;
        private Matrix4x4 projectionMatrix;

        public Graphics2DRenderer(RenderManager renderManager, GraphicsManager graphicsManager, ResourcesManager resourcesManager)
        {
            if (resourcesManager == null)
            {
                throw new ArgumentNullException(nameof(resourcesManager));
            }

            this.renderManager = renderManager ?? throw new ArgumentNullException(nameof(renderManager));
            this.graphicsManager = graphicsManager ?? throw new ArgumentNullException(nameof(graphicsManager));

            // this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/Graphics2DRender.shader");
            this.shader = resourcesManager.LoadResourceAsync<Shader>("/System/Shaders/Graphics2DRender_Old.shader");
            this.systemFont = resourcesManager.LoadResourceAsync<Font>("/System/Fonts/SystemFont.font");
            this.projectionMatrix = Matrix4x4.Identity;

            var maxSurfaceCount = 10000;

            this.rectangleSurfaces = new RectangleSurface[maxSurfaceCount];
            this.cpuRectangleSurfacesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RectangleSurface>(GraphicsHeapType.Upload, maxSurfaceCount, isStatic: false, label: "Graphics2DRectangleSurfacesBuffer_Cpu");
            this.rectangleSurfacesGraphicsBuffer = this.graphicsManager.CreateGraphicsBuffer<RectangleSurface>(GraphicsHeapType.Gpu, maxSurfaceCount, isStatic: false, label: "Graphics2DRectangleSurfacesBuffer_Gpu");
        }

        public override void PreUpdate(CoreEngineContext context)
        {
            this.currentSurfaceCount = 0;
            
            var renderSize = this.renderManager.GetRenderSize();
            this.projectionMatrix = MathUtils.CreateOrthographicMatrixOffCenter(0, renderSize.X, 0, renderSize.Y, 0, 1);
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
            
            minPoint *= scaleFactor;
            maxPoint *= scaleFactor;

            var size = maxPoint - minPoint;
            // TODO: Create an util method CreateScaleTranslation
            var worldMatrix = Matrix4x4.CreateScale(new Vector3(size, 0)) * Matrix4x4.CreateTranslation(new Vector3(minPoint, 0));

            // TODO: Move the matrix mul into an amplification shader?
            this.rectangleSurfaces[this.currentSurfaceCount++] = new RectangleSurface(worldMatrix * this.projectionMatrix, textureMinPoint, textureMaxPoint, texture.ShaderResourceIndex, isOpaque);
        }

        public Fence? Render(Texture renderTargetTexture)
        {
            if (this.currentSurfaceCount > 0)
            {
                var copyCommandList = CreateCopyCommandList();
                var renderCommandList = CreateRenderCommandList(renderTargetTexture);

                var copyFence = this.graphicsManager.ExecuteCommandLists(this.renderManager.CopyCommandQueue, new CommandList[] { copyCommandList });
                return this.graphicsManager.ExecuteCommandLists(this.renderManager.RenderCommandQueue, new CommandList[] { renderCommandList }, new Fence[] { copyFence });
            }

            return null;
        }

        private CommandList CreateCopyCommandList()
        {
            this.graphicsManager.CopyDataToGraphicsBuffer<RectangleSurface>(this.cpuRectangleSurfacesGraphicsBuffer, 0, this.rectangleSurfaces.AsSpan().Slice(0, this.currentSurfaceCount));

            var commandListName = "Graphics2DRenderer_Copy";
            var copyCommandList = this.graphicsManager.CreateCommandList(this.renderManager.CopyCommandQueue, commandListName);

            var startCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);
            this.graphicsManager.CopyDataToGraphicsBuffer<RectangleSurface>(copyCommandList, this.rectangleSurfacesGraphicsBuffer, this.cpuRectangleSurfacesGraphicsBuffer, this.currentSurfaceCount);
            var endCopyQueryIndex = this.renderManager.InsertQueryTimestamp(copyCommandList);

            this.graphicsManager.CommitCommandList(copyCommandList);
            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.CopyTimestamp, startCopyQueryIndex, endCopyQueryIndex);

            return copyCommandList;
        }

        private CommandList CreateRenderCommandList(Texture renderTargetTexture)
        {
            var commandListName = "Graphics2DRenderer_Render";
            var renderCommandList = this.graphicsManager.CreateCommandList(this.renderManager.RenderCommandQueue, commandListName);
            
            var renderTarget = new RenderTargetDescriptor(renderTargetTexture, null, BlendOperation.AlphaBlending);
            // var renderTarget = new RenderTargetDescriptor(renderTargetTexture, Vector4.Zero, BlendOperation.AlphaBlending);
            var renderPassDescriptor = new RenderPassDescriptor(renderTarget, null, DepthBufferOperation.None, backfaceCulling: true);

            var startQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.BeginRenderPass(renderCommandList, renderPassDescriptor, this.shader);

            this.graphicsManager.SetShaderParameterValues(renderCommandList, 0, new uint[] { (uint)this.currentSurfaceCount, this.rectangleSurfacesGraphicsBuffer.ShaderResourceIndex });

            this.graphicsManager.DispatchMesh(renderCommandList, (uint)MathF.Ceiling((float)this.currentSurfaceCount / maxSurfaceCountPerThreadGroup), 1, 1);

            this.graphicsManager.EndRenderPass(renderCommandList);
            var endQueryIndex = this.renderManager.InsertQueryTimestamp(renderCommandList);
            this.graphicsManager.CommitCommandList(renderCommandList);
            this.renderManager.AddGpuTiming(commandListName, QueryBufferType.Timestamp, startQueryIndex, endQueryIndex);
            return renderCommandList;
        }
    }
}
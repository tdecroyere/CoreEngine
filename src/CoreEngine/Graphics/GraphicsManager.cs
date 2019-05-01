using System;
using System.Numerics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class GraphicsManager : SystemManager
    {
        private readonly GraphicsService graphicsService;
        private readonly MemoryService memoryService;
        private readonly ResourcesManager resourcesManager;

        public GraphicsManager(GraphicsService graphicsService, MemoryService memoryService, ResourcesManager resourcesManager)
        {
            this.graphicsService = graphicsService;
            this.memoryService = memoryService;
            this.resourcesManager = resourcesManager;

            InitResourceLoaders();
        }

        public void DebugDrawTriangle(Vector4 color1, Vector4 color2, Vector4 color3, Matrix4x4 worldMatrix)
        {
            // TODO: Provide an empty implementation and just put a warning?
            if (this.graphicsService.DebugDrawTriange == null)
            {
                throw new InvalidOperationException("Method DebugDrawTriangle is not implemented by the host program");
            }

            // TODO: Find a way to test if the delegate is null, because it is a struct it is valid event not filled by the host
            this.graphicsService.DebugDrawTriange(this.graphicsService.GraphicsContext, color1, color2, color3, worldMatrix);
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.graphicsService, this.memoryService));
        }
    }
}
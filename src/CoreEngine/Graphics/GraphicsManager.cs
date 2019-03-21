using System;
using System.Numerics;

namespace CoreEngine.Graphics
{
    public class GraphicsManager : Manager
    {
        private readonly GraphicsService graphicsService;

        public GraphicsManager(GraphicsService graphicsService)
        {
            this.graphicsService = graphicsService;
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
    }
}
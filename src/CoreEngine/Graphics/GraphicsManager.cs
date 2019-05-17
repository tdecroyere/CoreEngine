using System;
using System.Numerics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    // TODO: Add a render pipeline system to have a data oriented configuration of the render pipeline
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

        public void DrawMesh(Mesh mesh, Matrix4x4 worldMatrix)
        {
            for (var i = 0; i < mesh.SubObjects.Count; i++)
            {
                var meshSubObject = mesh.SubObjects[i];

                // TODO: Add shader and primitive type
                this.graphicsService.DrawPrimitives(meshSubObject.IndexCount / 3, meshSubObject.VertexBuffer.Id, meshSubObject.IndexBuffer.Id, worldMatrix);
            }
        }

        public void DebugDrawTriangle(Vector4 color1, Vector4 color2, Vector4 color3, Matrix4x4 worldMatrix)
        {
            this.graphicsService.DebugDrawTriangle(color1, color2, color3, worldMatrix);
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.graphicsService, this.memoryService));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.graphicsService, this.memoryService));
        }
    }
}
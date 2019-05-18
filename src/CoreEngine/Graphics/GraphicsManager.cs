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

        // TODO: Remove worldmatrix parameter so we can pass graphics paramters in constant buffers
        public void DrawMesh(Mesh mesh, Matrix4x4 worldMatrix)
        {
            for (var i = 0; i < mesh.SubObjects.Count; i++)
            {
                var meshSubObject = mesh.SubObjects[i];

                // TODO: Add shader and primitive type
                this.graphicsService.DrawPrimitives(meshSubObject.IndexCount / 3, meshSubObject.VertexBuffer.Id, meshSubObject.IndexBuffer.Id, worldMatrix);
            }
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.graphicsService, this.memoryService));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.graphicsService, this.memoryService));
        }
    }
}
using System;
using System.Collections.Generic;
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
        private Dictionary<Entity, MeshInstance> meshInstances;
        private List<Entity> meshInstancesToRemove;

        public GraphicsManager(GraphicsService graphicsService, MemoryService memoryService, ResourcesManager resourcesManager)
        {
            this.graphicsService = graphicsService;
            this.memoryService = memoryService;
            this.resourcesManager = resourcesManager;

            this.meshInstances = new Dictionary<Entity, MeshInstance>();
            this.meshInstancesToRemove = new List<Entity>();

            InitResourceLoaders();
        }

        // TODO: Remove worldmatrix parameter so we can pass graphics paramters in constant buffers
        public void AddOrUpdateEntity(Entity entity, Mesh mesh, Matrix4x4 worldMatrix)
        {
            if (this.meshInstances.ContainsKey(entity))
            {
                this.meshInstances[entity].WorldMatrix = worldMatrix;
                this.meshInstances[entity].IsAlive = true;
            }

            else
            {
                this.meshInstances.Add(entity, new MeshInstance(entity, mesh, worldMatrix));
            }
        }

        public override void Update()
        {
            RemoveDeadMeshInstances();
            RunRenderPipeline();
            UpdateMeshInstancesStatus(false);
        }

        private void RunRenderPipeline()
        {
            DrawMeshInstances();
        }

        private void RemoveDeadMeshInstances()
        {
            this.meshInstancesToRemove.Clear();

            // TODO: Replace that with an hybrid dictionary/list
            foreach(var meshInstance in this.meshInstances.Values)
            {
                if (!meshInstance.IsAlive)
                {
                    this.meshInstancesToRemove.Add(meshInstance.Entity);
                }
            }

            for (var i = 0; i < this.meshInstancesToRemove.Count; i++)
            {
                this.meshInstances.Remove(this.meshInstancesToRemove[i]);
            }
        }

        public void DrawMeshInstances()
        {
            // TODO: Replace that with an hybrid dictionary/list
            foreach(var meshInstance in this.meshInstances.Values)
            {
                var mesh = meshInstance.Mesh;

                for (var i = 0; i < mesh.SubObjects.Count; i++)
                {
                    var meshSubObject = mesh.SubObjects[i];

                    // TODO: Add shader and primitive type
                    this.graphicsService.DrawPrimitives(meshSubObject.IndexCount / 3, meshSubObject.VertexBuffer.Id, meshSubObject.IndexBuffer.Id, meshInstance.WorldMatrix);
                }
            }
        }

        private void UpdateMeshInstancesStatus(bool isAlive)
        {
            // TODO: Replace that with an hybrid dictionary/list
            foreach(var meshInstance in this.meshInstances.Values)
            {
                meshInstance.IsAlive = isAlive;
            }
        }

        private void InitResourceLoaders()
        {
            this.resourcesManager.AddResourceLoader(new ShaderResourceLoader(this.graphicsService, this.memoryService));
            this.resourcesManager.AddResourceLoader(new MeshResourceLoader(this.graphicsService, this.memoryService));
        }
    }
}
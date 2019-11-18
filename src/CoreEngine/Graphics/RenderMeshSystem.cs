using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Graphics
{
    public class RenderMeshSystem : EntitySystem
    {
        private readonly SceneRenderer graphicsManager;
        private readonly ResourcesManager resourcesManager;

        public RenderMeshSystem(SceneRenderer graphicsManager, ResourcesManager resourceManager)
        {
            this.graphicsManager = graphicsManager;
            this.resourcesManager = resourceManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Render Mesh System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(MeshComponent)));

            return definition;
        }

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var meshArray = this.GetComponentDataArray<MeshComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                var transform = transformArray[i];
                var meshComponent = meshArray[i];

                if (meshComponent.MeshResourceId != 0)
                {
                    var mesh = this.resourcesManager.GetResourceById<Mesh>(meshComponent.MeshResourceId);

                    if (mesh != null)
                    {
                        graphicsManager.AddOrUpdateEntity(entity, mesh, transform.WorldMatrix);
                    }
                }
            }
        }
    }
}
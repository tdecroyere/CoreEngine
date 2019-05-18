using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Graphics;
using CoreEngine.Resources;

namespace CoreEngine.Tests.EcsTest
{
    public class RenderMeshSystem : EntitySystem
    {
        private readonly GraphicsManager graphicsManager;
        private readonly ResourcesManager resourcesManager;

        public RenderMeshSystem(GraphicsManager graphicsManager, ResourcesManager resourceManager)
        {
            this.graphicsManager = graphicsManager;
            this.resourcesManager = resourceManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Debug Triangle System");

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
                var transform = transformArray[i];
                var meshComponent = meshArray[i];

                var mesh = this.resourcesManager.GetResourceById<Mesh>(meshComponent.MeshId);

                if (mesh != null)
                {
                    // TODO: Move that to a component systerm
                    graphicsManager.DrawMesh(mesh, transform.WorldMatrix);
                }
            }
        }
    }
}
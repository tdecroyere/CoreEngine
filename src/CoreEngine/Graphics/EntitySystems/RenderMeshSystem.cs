using System;
using System.Numerics;
using CoreEngine.Collections;
using CoreEngine.Components;
using CoreEngine.Graphics.Components;
using CoreEngine.Resources;

namespace CoreEngine.Graphics.EntitySystems
{
    public class RenderMeshSystem : EntitySystem
    {
        private readonly GraphicsSceneRenderer sceneRenderer;
        private readonly ResourcesManager resourcesManager;

        public RenderMeshSystem(GraphicsSceneRenderer sceneRenderer, ResourcesManager resourceManager)
        {
            this.sceneRenderer = sceneRenderer;
            this.resourcesManager = resourceManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Render Mesh System");

            definition.Parameters.Add(new EntitySystemParameter(typeof(TransformComponent)));
            definition.Parameters.Add(new EntitySystemParameter(typeof(MeshComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var meshArray = this.GetComponentDataArray<MeshComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                ref var transformComponent = ref transformArray[i];
                ref var meshComponent = ref meshArray[i];

                if (meshComponent.MeshResourceId != 0)
                {
                    var mesh = this.resourcesManager.GetResourceById<Mesh>(meshComponent.MeshResourceId);

                    if (mesh != null)
                    {
                        // TODO: Replace that with ItemIdentifier.Empty
                        if (meshComponent.MeshInstance == ItemIdentifier.Empty)
                        {
                            var meshInstance = new MeshInstance(mesh, transformComponent.WorldMatrix, this.sceneRenderer.currentObjectPropertyIndex++);
                            meshInstance.IsAlive = true;
                            meshComponent.MeshInstance = sceneRenderer.CurrentScene.MeshInstances.Add(meshInstance);
                        }

                        else
                        {
                            var meshInstance = sceneRenderer.CurrentScene.MeshInstances[meshComponent.MeshInstance];
                            meshInstance.WorldMatrix = transformComponent.WorldMatrix;
                            meshInstance.IsAlive = true;
                        }
                    }
                }
            }
        }
    }
}
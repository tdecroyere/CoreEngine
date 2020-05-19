using System;
using System.Numerics;
using CoreEngine.Collections;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Rendering.Components;
using CoreEngine.Resources;

namespace CoreEngine.Rendering.EntitySystems
{
    public class RenderMeshSystem : EntitySystem
    {
        private readonly GraphicsSceneManager sceneManager;
        private readonly ResourcesManager resourcesManager;

        public RenderMeshSystem(GraphicsSceneManager sceneManager, ResourcesManager resourceManager)
        {
            this.sceneManager = sceneManager;
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
            if (entityManager == null)
            {
                throw new ArgumentNullException(nameof(entityManager));
            }
            
            var entityArray = this.GetEntityArray();
            var transformArray = this.GetComponentDataArray<TransformComponent>();
            var meshArray = this.GetComponentDataArray<MeshComponent>();

            var currentScene = sceneManager.CurrentScene;

            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                ref var transformComponent = ref transformArray[i];
                ref var meshComponent = ref meshArray[i];
                Material? material = null;

                if (entityManager.HasComponent<MaterialComponent>(entity))
                {
                    var materialComponent = entityManager.GetComponentData<MaterialComponent>(entity);

                    if (materialComponent.MaterialResourceId != 0)
                    {
                        material = this.resourcesManager.GetResourceById<Material>(materialComponent.MaterialResourceId);
                    }
                }

                if (meshComponent.MeshResourceId != 0)
                {
                    if (!currentScene.MeshInstances.Contains(meshComponent.MeshInstance))
                    {
                        var mesh = this.resourcesManager.GetResourceById<Mesh>(meshComponent.MeshResourceId);
                        var meshInstance = new MeshInstance(mesh, material, transformComponent.WorldMatrix, false);
                        meshComponent.MeshInstance = currentScene.MeshInstances.Add(meshInstance);
                    }

                    else
                    {
                        var meshInstance = currentScene.MeshInstances[meshComponent.MeshInstance];
                        meshInstance.WorldMatrix = transformComponent.WorldMatrix;
                    }
                }
            }
        }
    }
}
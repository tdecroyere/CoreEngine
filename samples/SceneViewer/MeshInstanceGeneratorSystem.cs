using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Rendering.Components;
using CoreEngine.Inputs;
using CoreEngine.Resources;
using CoreEngine.Rendering;

namespace CoreEngine.Samples.SceneViewer
{
    public class MeshInstanceGeneratorSystem : EntitySystem
    {
        private readonly ResourcesManager resourcesManager;
        private bool isFirstTimeRun = true;

        public MeshInstanceGeneratorSystem(ResourcesManager resourcesManager)
        {
            this.resourcesManager = resourcesManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Mesh Instance Generator Generator System");

            definition.Parameters.Add(new EntitySystemParameter<MeshInstanceGeneratorComponent>(isReadOnly: true));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }

            if (this.isFirstTimeRun)
            {
                this.isFirstTimeRun = false;

                var mesh = this.resourcesManager.LoadResourceAsync<Mesh>("/Data/teapot.mesh");

                var entityArray = this.GetEntityArray();
                var meshInstanceGeneratorArray = this.GetComponentDataArray<MeshInstanceGeneratorComponent>();

                for (var i = 0; i < entityArray.Length; i++)
                {
                    ref var meshInstanceGeneratorComponent = ref meshInstanceGeneratorArray[i];

                    var positionOffset = 0;

                    var componentLayout = entityManager.CreateComponentLayout<MeshComponent, TransformComponent>();

                    for (var j = 0; j < meshInstanceGeneratorComponent.MeshInstanceCountWidth; j++)
                    {
                        for (var k = 0; k < meshInstanceGeneratorComponent.MeshInstanceCountWidth; k++)
                        {
                            var entity = entityManager.CreateEntity(componentLayout);
                            entityManager.SetComponentData(entity, new MeshComponent { MeshResourceId = mesh.ResourceId });
                            entityManager.SetComponentData(entity, new TransformComponent{ Position = new Vector3(positionOffset + k * meshInstanceGeneratorComponent.Spacing, 0, j * meshInstanceGeneratorComponent.Spacing), Scale = new Vector3(0.02f, 0.02f, 0.02f), WorldMatrix = Matrix4x4.Identity });
                        }
                    }
                }
            }
        }
    }
}
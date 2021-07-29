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

                var entityArray = this.GetEntityArray();
                
                for (var i = 0; i < entityArray.Length; i++)
                {
                    var meshes = new Mesh[]
                    {
                        this.resourcesManager.LoadResourceAsync<Mesh>("/Data/kitten.mesh"),
                        this.resourcesManager.LoadResourceAsync<Mesh>("/Data/teapot.mesh")
                    };

                    var meshInstanceGeneratorArray = this.GetComponentDataArray<MeshInstanceGeneratorComponent>();
                    ref var meshInstanceGeneratorComponent = ref meshInstanceGeneratorArray[i];

                    var componentLayout = entityManager.CreateComponentLayout<MeshComponent, TransformComponent>();
                    var animateComponentLayout = entityManager.CreateComponentLayout<MeshComponent, TransformComponent, AutomaticMovementComponent>();
                    var dimensions = new Vector3(20, 20, 20);
                    var random = new Random();

                    for (var j = 0; j < meshInstanceGeneratorComponent.MeshInstanceCountWidth; j++)
                    {
                        for (var k = 0; k < meshInstanceGeneratorComponent.MeshInstanceCountWidth; k++)
                        {
                            var meshIndex = random.Next() % meshes.Length;
                            var mesh = meshes[meshIndex];

                            var offsetX = (float)random.NextDouble() * dimensions.X - dimensions.X * 0.5f;
                            var offsetY = (float)random.NextDouble() * dimensions.Y - dimensions.Y * 0.5f;
                            var offsetZ = (float)random.NextDouble() * dimensions.Z - dimensions.Z * 0.5f;

                            var position = new Vector3(offsetX, offsetY, offsetZ);
                            var scale = (float)random.NextDouble() * 1.5f;
                            var rotationX = (float)random.NextDouble() * 90.0f - 90.0f * 0.5f;
                            var rotationY = (float)random.NextDouble() * 90.0f - 90.0f * 0.5f;

                            if (meshIndex == 1)
                            {
                                scale /= 30.0f;
                            }

                            var entityComponentLayout = (i == 0) ? animateComponentLayout : componentLayout;

                            var entity = entityManager.CreateEntity(entityComponentLayout);
                            entityManager.SetComponentData(entity, new MeshComponent { MeshResourceId = mesh.ResourceId });
                            entityManager.SetComponentData(entity, new TransformComponent { Position = position, Scale = new Vector3(scale, scale, scale), RotationX = rotationX, RotationY = rotationY, WorldMatrix = Matrix4x4.Identity });
                        }
                    }
                }
            }
        }
    }
}
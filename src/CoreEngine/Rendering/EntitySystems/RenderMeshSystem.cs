using CoreEngine.Components;
using CoreEngine.Rendering.Components;

namespace CoreEngine.Rendering.EntitySystems
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

            definition.Parameters.Add(new EntitySystemParameter<TransformComponent>());
            definition.Parameters.Add(new EntitySystemParameter<MeshComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                throw new ArgumentNullException(nameof(entityManager));
            }

            var memoryChunks = this.GetMemoryChunks();

            // TODO: Make renderMesh thread safe so we can parallelise the loop
            Parallel.For(0, memoryChunks.Length, (i) =>
            //for (var i = 0; i < memoryChunks.Length; i++)
            {
                var memoryChunk = memoryChunks.Span[i];

                var transformArray = GetComponentArray<TransformComponent>(memoryChunk);
                var meshArray = GetComponentArray<MeshComponent>(memoryChunk); 

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    ref var transformComponent = ref transformArray[j];
                    ref var meshComponent = ref meshArray[j];

                    // TODO: Use nullable here
                    if (meshComponent.MeshResourceId != 0)
                    {
                        if (meshComponent.MeshInstanceId.HasValue)
                        {
                            // TODO: Detect if transform component has changed
                            //if (transformComponent.HasChanged == 0)
                            //{
                            //sceneRenderer.RenderMesh(meshComponent.MeshInstanceId.Value);
                            //}
                            //else
                            //{
                            sceneRenderer.RenderMesh(meshComponent.MeshInstanceId.Value, transformComponent.WorldMatrix, transformComponent.Scale.X);
                            //}
                        }

                        else
                        {
                            var mesh = this.resourcesManager.GetResourceById<Mesh>(meshComponent.MeshResourceId);

                            // TODO: This code is not thread safe!
                            // TODO: We Support only uniform scale for the moment
                            var meshInstanceId = sceneRenderer.RenderMesh(mesh, transformComponent.WorldMatrix, transformComponent.Scale.X);
                            meshComponent.MeshInstanceId = meshInstanceId;
                        }
                    }
                }
            });
        }
    }
}
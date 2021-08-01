using System;
using System.Numerics;
using CoreEngine.Components;
using CoreEngine.Diagnostics;
using CoreEngine.Rendering.Components;
using CoreEngine.Inputs;

namespace CoreEngine.Samples.SceneViewer
{
    public class InputsUpdateSystem : EntitySystem
    {
        private readonly InputsManager inputsManager;

        public InputsUpdateSystem(InputsManager inputsManager)
        {
            this.inputsManager = inputsManager;
        }

        public override EntitySystemDefinition BuildDefinition()
        {
            var definition = new EntitySystemDefinition("Inputs Update System");

            definition.Parameters.Add(new EntitySystemParameter<PlayerComponent>());

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }

            var memoryChunks = this.GetMemoryChunks();

            for (var i = 0; i < memoryChunks.Length; i++)
            {
                var memoryChunk = memoryChunks.Span[i];

                var playerArray = GetComponentArray<PlayerComponent>(memoryChunk);

                for (var j = 0; j < memoryChunk.EntityCount; j++)
                {
                    ref var playerComponent = ref playerArray[j];

                    if (playerComponent.IsActive)
                    {
                        playerComponent.MovementVector = new Vector3(this.inputsManager.GetMovementVector(), 0.0f);
                        playerComponent.RotationVector = new Vector3(this.inputsManager.GetRotationVector(), 0.0f);
                    }
                }
            }
        }
    }
}
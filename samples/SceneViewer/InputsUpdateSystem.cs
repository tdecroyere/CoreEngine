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

            definition.Parameters.Add(new EntitySystemParameter(typeof(PlayerComponent)));

            return definition;
        }

        public override void Process(EntityManager entityManager, float deltaTime)
        {
            if (entityManager == null)
            {
                return;
            }

            var entityArray = this.GetEntityArray();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                ref var playerComponent = ref playerArray[i];

                playerComponent.MovementVector = new Vector3(this.inputsManager.GetMovementVector(), 0.0f);
                playerComponent.RotationVector = new Vector3(this.inputsManager.GetRotationVector(), 0.0f);
            }
        }
    }
}
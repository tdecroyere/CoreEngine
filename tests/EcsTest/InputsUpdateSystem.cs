using System;
using System.Numerics;
using CoreEngine;
using CoreEngine.Inputs;

namespace CoreEngine.Tests.EcsTest
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

        public override void Process(float deltaTime)
        {
            var entityArray = this.GetEntityArray();
            var playerArray = this.GetComponentDataArray<PlayerComponent>();

            for (var i = 0; i < entityArray.Length; i++)
            {
                playerArray[i].MovementVector = new Vector3(this.inputsManager.GetMovementVector(), 0.0f);
                playerArray[i].RotationVector = new Vector3(this.inputsManager.GetRotationVector(), 0.0f);

                if (this.inputsManager.IsLeftMouseDown())
                {
                    this.inputsManager.SendVibrationCommand(1, 1.0f, 0.0f, 0.0f, 0.0f, 1);
                    var mouseVector = this.inputsManager.GetMouseDelta();
                    playerArray[i].RotationVector = new Vector3(mouseVector.X, mouseVector.Y, 0.0f);
                }

                //Logger.WriteMessage($"InputVector: {playerArray[i].InputVector}");
            }
        }
    }
}
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
                // TODO: Unify the input vector calculation into the InputManager
                // By calculating deadzones

                // var deltaX = this.inputsManager.LeftActionValue();
                // deltaX -= this.inputsManager.RightActionValue();

                // var deltaY = this.inputsManager.UpActionValue();
                // deltaY -= this.inputsManager.DownActionValue();

                playerArray[i].InputVector = new Vector3(this.inputsManager.GetMovementVector(), 0.0f);

                if (this.inputsManager.IsLeftMouseDown())
                {
                    var mouseVector = this.inputsManager.GetMouseDelta();
                    playerArray[i].InputVector = new Vector3(mouseVector.X, mouseVector.Y, 0.0f);
                }

                //Console.WriteLine($"InputVector: {playerArray[i].InputVector}");
            }
        }
    }
}
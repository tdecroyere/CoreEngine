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
                var deltaX = (this.inputsManager.IsLeftActionPressed()) ? 1 : 0;
                deltaX += (this.inputsManager.IsRightActionPressed()) ? -1 : 0;

                var deltaY = (this.inputsManager.IsDownActionPressed()) ? -1 : 0;
                deltaY += (this.inputsManager.IsUpActionPressed()) ? 1 : 0;

                playerArray[i].InputVector.X = deltaX; 
                playerArray[i].InputVector.Y = deltaY;

                if (this.inputsManager.IsLeftMousePressed())
                {
                    var mouseVector = this.inputsManager.GetMouseDelta();
                    playerArray[i].InputVector = new Vector3(mouseVector.X, mouseVector.Y, 0.0f);
                }

                Console.WriteLine($"InputVector: {playerArray[i].InputVector}");
            }
        }
    }
}
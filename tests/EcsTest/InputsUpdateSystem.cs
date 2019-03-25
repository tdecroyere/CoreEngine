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
                playerArray[i].InputVector.X = (this.inputsManager.IsLeftActionPressed()) ? -1 : 0;
            }
        }
    }
}
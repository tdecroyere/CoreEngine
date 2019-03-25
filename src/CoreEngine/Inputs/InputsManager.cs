using System;
using System.Numerics;

namespace CoreEngine.Inputs
{
    public class InputsManager : SystemManager
    {
        private readonly InputsService inputsService;
        private InputsState inputsState;

        public InputsManager(InputsService inputsService)
        {
            this.inputsService = inputsService;
            this.inputsState = new InputsState();
        }

        public override void Update()
        {
            this.inputsState = this.inputsService.GetInputsState(this.inputsService.InputsContext);
        }

        // TODO: Add an action system to process more easily the raw data from the host
        public bool IsLeftActionPressed()
        {
            Console.WriteLine($"KeyQ: {this.inputsState.Keyboard.KeyQ.Value}");
            return (this.inputsState.Keyboard.KeyQ.Value > 0.0f);
        }
    }
}
using System;
using System.Numerics;

namespace CoreEngine.Inputs
{
    public class InputsManager : Manager
    {
        private readonly InputsService inputsService;

        public InputsManager(InputsService inputsService)
        {
            this.inputsService = inputsService;
        }

        public bool IsLeftActionPressed()
        {
            Console.WriteLine(this.inputsService.Keyboard.KeyQ.Value);
            return (this.inputsService.Keyboard.KeyQ.Value > 0.0f);
        }
    }
}
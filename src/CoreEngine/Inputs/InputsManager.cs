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
            // TODO: Implement an input action system to map the controls to actions
            // TODO: Take into account transition count

            if (this.inputsService.GetInputsState != null)
            {
                this.inputsState = this.inputsService.GetInputsState(this.inputsService.InputsContext);
            }
        }

        // TODO: Add an action system to process more easily the raw data from the host
        // TODO: Take into account the keyboard layout when specifying the input config to map to actions

        public bool IsLeftActionPressed()
        {
            return (this.inputsState.Keyboard.KeyQ.Value > 0.0f) || (this.inputsState.Keyboard.LeftArrow.Value > 0.0f);
        }

        public bool IsRightActionPressed()
        {
            return (this.inputsState.Keyboard.KeyD.Value > 0.0f) || (this.inputsState.Keyboard.RightArrow.Value > 0.0f);
        }

        public bool IsUpActionPressed()
        {
            return (this.inputsState.Keyboard.KeyZ.Value > 0.0f) || (this.inputsState.Keyboard.UpArrow.Value > 0.0f);
        }

        public bool IsDownActionPressed()
        {
            return (this.inputsState.Keyboard.KeyS.Value > 0.0f) || (this.inputsState.Keyboard.DownArrow.Value > 0.0f);
        }

        public bool IsLeftMousePressed()
        {
            return (this.inputsState.Mouse.LeftButton.Value == 0.0f && this.inputsState.Mouse.LeftButton.TransitionCount > 0);
        }

        public Vector2 GetMouseDelta()
        {
            return new Vector2(this.inputsState.Mouse.DeltaX.Value, this.inputsState.Mouse.DeltaY.Value);
        }
    }
}
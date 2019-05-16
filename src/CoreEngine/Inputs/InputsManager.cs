using System;
using System.Numerics;

namespace CoreEngine.Inputs
{
    // TODO: Check if a gamepad is connected

    public class InputsManager : SystemManager
    {
        private readonly InputsService inputsService;
        private InputsState inputsState;
        private const float deadZoneSquared = 0.1f;

        public InputsManager(InputsService inputsService)
        {
            this.inputsService = inputsService;
            this.inputsState = new InputsState();
        }

        public override void Update()
        {
            // TODO: Implement an input action system to map the controls to actions
            // TODO: Take into account transition count
            // TODO: For the configuration, we should have access to the controller name and vendor

            this.inputsState = this.inputsService.GetInputsState();
        }

        // TODO: Add an action system to process more easily the raw data from the host
        // TODO: Take into account the keyboard layout when specifying the input config to map to actions
        // TODO: Manage dead zones with circle or cubic mode for sticks
        // TODO: Take into account for stick a normalized vector
        public Vector2 GetMovementVector()
        {
            var deltaX = this.LeftActionValue();
            deltaX -= this.RightActionValue();

            var deltaY = this.UpActionValue();
            deltaY -= this.DownActionValue();

            var result = new Vector2(deltaX, deltaY);
            //result = Vector2.Normalize(result);

            // TODO: Apply a circle deadzone for now
            if (result.LengthSquared() < deadZoneSquared)
            {
                return Vector2.Zero;
            }

            return result;
        }

        public float LeftActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.DPadLeft.Value + this.inputsState.Gamepad1.LeftStickLeft.Value + this.inputsState.Gamepad1.RightStickLeft.Value + this.inputsState.Gamepad1.ButtonA.Value + this.inputsState.Keyboard.KeyQ.Value + this.inputsState.Keyboard.LeftArrow.Value);
        }

        public float RightActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.DPadRight.Value + this.inputsState.Gamepad1.LeftStickRight.Value + this.inputsState.Gamepad1.RightStickRight.Value + this.inputsState.Keyboard.KeyD.Value + this.inputsState.Keyboard.RightArrow.Value);
        }

        public float UpActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.DPadUp.Value + this.inputsState.Gamepad1.LeftStickUp.Value + this.inputsState.Gamepad1.RightStickUp.Value + this.inputsState.Keyboard.KeyZ.Value + this.inputsState.Keyboard.UpArrow.Value);
        }

        public float DownActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.DPadDown.Value + this.inputsState.Gamepad1.LeftStickDown.Value + this.inputsState.Gamepad1.RightStickDown.Value + this.inputsState.Keyboard.KeyS.Value + this.inputsState.Keyboard.DownArrow.Value);
        }

        public bool IsLeftMousePressed()
        {
            return (this.inputsState.Mouse.LeftButton.Value == 0.0f && this.inputsState.Mouse.LeftButton.TransitionCount > 0);
        }

        public bool IsLeftMouseDown()
        {
            return (this.inputsState.Mouse.LeftButton.Value > 0.0f);
        }

        public Vector2 GetMouseDelta()
        {
            return new Vector2(this.inputsState.Mouse.DeltaX.Value, this.inputsState.Mouse.DeltaY.Value);
        }

        public void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            // TODO: Handle other players
            this.inputsService.SendVibrationCommand(this.inputsState.Gamepad1.PlayerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
        }
    }
}
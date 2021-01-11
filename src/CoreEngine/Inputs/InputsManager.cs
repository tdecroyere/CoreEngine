using System;
using System.Numerics;
using CoreEngine.HostServices;
using CoreEngine.UI.Native;

namespace CoreEngine.Inputs
{
    // TODO: Check if a gamepad is connected

    public class InputsManager : SystemManager
    {
        private readonly IInputsService inputsService;
        private const float deadZoneSquared = 0.1f;

        public InputsManager(IInputsService inputsService)
        {
            this.inputsService = inputsService;
            this.inputsState = new InputsState();
        }

        public InputsState inputsState
        {
            get;
            private set;
        }

        public void AssociateWindow(Window window)
        {
            this.inputsService.AssociateWindow(window.NativePointer);
        }

        public override void PreUpdate(CoreEngineContext context)
        {
            // TODO: Implement an input action system to map the controls to actions
            // TODO: Take into account transition count
            // TODO: For the configuration, we should have access to the controller name and vendor

            if (context.IsAppActive)
            {
                this.inputsState = this.inputsService.GetInputsState();
            }

            else
            {
                this.inputsState = new InputsState();
            }
        }

        // TODO: Add an action system to process more easily the raw data from the host
        // TODO: Take into account the keyboard layout when specifying the input config to map to actions
        // TODO: Manage dead zones with circle or cubic mode for sticks
        // TODO: Take into account for stick a normalized vector
        public Vector2 GetMovementVector()
        {
            var deltaX = this.RightMovementActionValue() - this.LeftMovementActionValue();
            var deltaY = this.UpMovementActionValue() - this.DownMovementActionValue();

            var result = new Vector2(deltaX, deltaY);
            //result = Vector2.Normalize(result);

            // TODO: Apply a circle deadzone for now
            if (result.LengthSquared() < deadZoneSquared)
            {
                return Vector2.Zero;
            }

            return result;
        }

        public Vector2 GetRotationVector()
        {
            var deltaY = this.RightRotationActionValue();
            deltaY -= this.LeftRotationActionValue();

            var deltaX = this.DownRotationActionValue();
            deltaX -= this.UpRotationActionValue();

            var result = new Vector2(deltaX, deltaY);
            //result = Vector2.Normalize(result);

            // TODO: Apply a circle deadzone for now
            if (result.LengthSquared() < deadZoneSquared)
            {
                return Vector2.Zero;
            }

            return result;
        }

        public float LeftMovementActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.LeftStickLeft.Value + this.inputsState.Keyboard.KeyQ.Value);
        }

        public float RightMovementActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.LeftStickRight.Value + this.inputsState.Keyboard.KeyD.Value);
        }

        public float UpMovementActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.LeftStickUp.Value + this.inputsState.Keyboard.KeyZ.Value);
        }

        public float DownMovementActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.LeftStickDown.Value + this.inputsState.Keyboard.KeyS.Value);
        }

        public float LeftRotationActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.RightStickLeft.Value + this.inputsState.Keyboard.LeftArrow.Value);
        }

        public float RightRotationActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.RightStickRight.Value + this.inputsState.Keyboard.RightArrow.Value);
        }

        public float UpRotationActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.RightStickUp.Value + this.inputsState.Keyboard.UpArrow.Value);
        }

        public float DownRotationActionValue()
        {
            return MathF.Min(1.0f, this.inputsState.Gamepad1.RightStickDown.Value + this.inputsState.Keyboard.DownArrow.Value);
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
            this.inputsService.SendVibrationCommand(playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
        }
    }
}
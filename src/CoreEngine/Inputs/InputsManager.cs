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
            this.InputsState = new InputsState();
        }

        public InputsState InputsState
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
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // TODO: Implement an input action system to map the controls to actions
            // TODO: Take into account transition count
            // TODO: For the configuration, we should have access to the controller name and vendor

            if (context.IsAppActive)
            {
                this.InputsState = this.inputsService.GetInputsState();
            }

            else
            {
                this.InputsState = new InputsState();
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
            return MathF.Min(1.0f, this.InputsState.Gamepad1.LeftStickLeft.Value + this.InputsState.Keyboard.KeyQ.Value);
        }

        public float RightMovementActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.LeftStickRight.Value + this.InputsState.Keyboard.KeyD.Value);
        }

        public float UpMovementActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.LeftStickUp.Value + this.InputsState.Keyboard.KeyZ.Value);
        }

        public float DownMovementActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.LeftStickDown.Value + this.InputsState.Keyboard.KeyS.Value);
        }

        public float LeftRotationActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.RightStickLeft.Value + this.InputsState.Keyboard.LeftArrow.Value);
        }

        public float RightRotationActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.RightStickRight.Value + this.InputsState.Keyboard.RightArrow.Value);
        }

        public float UpRotationActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.RightStickUp.Value + this.InputsState.Keyboard.UpArrow.Value);
        }

        public float DownRotationActionValue()
        {
            return MathF.Min(1.0f, this.InputsState.Gamepad1.RightStickDown.Value + this.InputsState.Keyboard.DownArrow.Value);
        }

        public bool IsLeftMousePressed()
        {
            return (this.InputsState.Mouse.LeftButton.Value == 0.0f && this.InputsState.Mouse.LeftButton.TransitionCount > 0);
        }

        public bool IsLeftMouseDown()
        {
            return (this.InputsState.Mouse.LeftButton.Value > 0.0f);
        }

        public Vector2 GetMouseDelta()
        {
            return new Vector2(this.InputsState.Mouse.DeltaX.Value, this.InputsState.Mouse.DeltaY.Value);
        }

        public void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            // TODO: Handle other players
            this.inputsService.SendVibrationCommand(playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
        }
    }
}
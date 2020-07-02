using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate InputsState InputsService_GetInputsStateDelegate(IntPtr context);
    internal unsafe delegate void InputsService_SendVibrationCommandDelegate(IntPtr context, uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms);

    public struct InputsService : IInputsService
    {
        private IntPtr context { get; }

        private InputsService_GetInputsStateDelegate inputsService_GetInputsStateDelegate { get; }
        public unsafe InputsState GetInputsState()
        {
            if (this.context != null && this.inputsService_GetInputsStateDelegate != null)
            {
                return this.inputsService_GetInputsStateDelegate(this.context);
            }

            return default(InputsState);
        }

        private InputsService_SendVibrationCommandDelegate inputsService_SendVibrationCommandDelegate { get; }
        public unsafe void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            if (this.context != null && this.inputsService_SendVibrationCommandDelegate != null)
            {
                this.inputsService_SendVibrationCommandDelegate(this.context, playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
            }
        }
    }
}

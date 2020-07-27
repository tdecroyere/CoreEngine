using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe struct InputsService : IInputsService
    {
        private IntPtr context { get; }

        private delegate* cdecl<IntPtr, InputsState> inputsService_GetInputsStateDelegate { get; }
        public unsafe InputsState GetInputsState()
        {
            if (this.context != null && this.inputsService_GetInputsStateDelegate != null)
            {
                return this.inputsService_GetInputsStateDelegate(this.context);
            }

            return default(InputsState);
        }

        private delegate* cdecl<IntPtr, uint, float, float, float, float, uint, void> inputsService_SendVibrationCommandDelegate { get; }
        public unsafe void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            if (this.context != null && this.inputsService_SendVibrationCommandDelegate != null)
            {
                this.inputsService_SendVibrationCommandDelegate(this.context, playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
            }
        }
    }
}

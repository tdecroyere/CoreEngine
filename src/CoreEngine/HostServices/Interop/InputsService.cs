using System;
using System.Buffers;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    public unsafe readonly struct InputsService : IInputsService
    {
        private IntPtr context { get; }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, IntPtr, void> inputsService_AssociateWindowDelegate { get; }
        public unsafe void AssociateWindow(IntPtr windowPointer)
        {
            if (this.inputsService_AssociateWindowDelegate != null)
            {
                this.inputsService_AssociateWindowDelegate(this.context, windowPointer);
            }
        }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, InputsState> inputsService_GetInputsStateDelegate { get; }
        public unsafe InputsState GetInputsState()
        {
            if (this.inputsService_GetInputsStateDelegate != null)
            {
                return this.inputsService_GetInputsStateDelegate(this.context);
            }

            return default(InputsState);
        }

        private delegate* unmanaged[Cdecl, SuppressGCTransition]<IntPtr, uint, float, float, float, float, uint, void> inputsService_SendVibrationCommandDelegate { get; }
        public unsafe void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            if (this.inputsService_SendVibrationCommandDelegate != null)
            {
                this.inputsService_SendVibrationCommandDelegate(this.context, playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
            }
        }
    }
}

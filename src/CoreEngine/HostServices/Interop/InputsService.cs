using System;
using System.Numerics;

namespace CoreEngine.HostServices.Interop
{
    internal unsafe delegate InputsState GetInputsStateDelegate(IntPtr context);
    internal unsafe delegate void SendVibrationCommandDelegate(IntPtr context, uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms);
    public struct InputsService : IInputsService
    {
        private IntPtr context
        {
            get;
        }

        private GetInputsStateDelegate getInputsStateDelegate
        {
            get;
        }

        public unsafe InputsState GetInputsState()
        {
            if (this.context != null && this.getInputsStateDelegate != null)
                return this.getInputsStateDelegate(this.context);
            else
                return default(InputsState);
        }

        private SendVibrationCommandDelegate sendVibrationCommandDelegate
        {
            get;
        }

        public unsafe void SendVibrationCommand(uint playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint duration10ms)
        {
            if (this.context != null && this.sendVibrationCommandDelegate != null)
                this.sendVibrationCommandDelegate(this.context, playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
        }
    }
}
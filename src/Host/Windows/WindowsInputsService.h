#pragma once
#include "WindowsCommon.h"

class WindowsInputsService
{
    public:
        struct InputsState GetInputsState();
        void SendVibrationCommand(unsigned int playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned int duration10ms);
};
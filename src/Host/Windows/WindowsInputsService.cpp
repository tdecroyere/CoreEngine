#pragma once
#include "WindowsCommon.h"
#include "WindowsInputsService.h"

struct InputsState WindowsInputsService::GetInputsState()
{
    return InputsState();
}

void WindowsInputsService::SendVibrationCommand(unsigned int playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned int duration10ms)
{

}

#pragma once
#include "WindowsDirect3D12Renderer.h"
#include "../../Common/CoreEngine.h"

struct InputsState GetInputsStateInterop(void* context)
{
    auto contextObject = (InputsManager*)context;
    return contextObject->GetInputsState()
}

void SendVibrationCommandInterop(void* context, unsigned int playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned int duration10ms)
{
    auto contextObject = (InputsManager*)context;
    contextObject->SendVibrationCommand(playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms)
}

void InitInputsService(InputsManager* context, InputsService* service)
{
    service->Context = context;
    service->GetInputsState = GetInputsStateInterop;
    service->SendVibrationCommand = SendVibrationCommandInterop;
}

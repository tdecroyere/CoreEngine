#pragma once
#include "../WindowsInputsService.h"

void AssociateWindowInterop(void* context, void* windowPointer)
{
    auto contextObject = (WindowsInputsService*)context;
    contextObject->AssociateWindow(windowPointer);
}

struct InputsState GetInputsStateInterop(void* context)
{
    auto contextObject = (WindowsInputsService*)context;
    return contextObject->GetInputsState();
}

void SendVibrationCommandInterop(void* context, unsigned int playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned int duration10ms)
{
    auto contextObject = (WindowsInputsService*)context;
    contextObject->SendVibrationCommand(playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
}

void InitInputsService(const WindowsInputsService& context, InputsService* service)
{
    service->Context = (void*)&context;
    service->InputsService_AssociateWindow = AssociateWindowInterop;
    service->InputsService_GetInputsState = GetInputsStateInterop;
    service->InputsService_SendVibrationCommand = SendVibrationCommandInterop;
}

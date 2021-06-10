#pragma once
#include "../WindowsInputsService.h"

void WindowsInputsServiceAssociateWindowInterop(void* context, void* windowPointer)
{
    auto contextObject = (WindowsInputsService*)context;
    contextObject->AssociateWindow(windowPointer);
}

struct InputsState WindowsInputsServiceGetInputsStateInterop(void* context)
{
    auto contextObject = (WindowsInputsService*)context;
    return contextObject->GetInputsState();
}

void WindowsInputsServiceSendVibrationCommandInterop(void* context, unsigned int playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned int duration10ms)
{
    auto contextObject = (WindowsInputsService*)context;
    contextObject->SendVibrationCommand(playerId, leftTriggerMotor, rightTriggerMotor, leftStickMotor, rightStickMotor, duration10ms);
}

void InitWindowsInputsService(const WindowsInputsService* context, InputsService* service)
{
    service->Context = (void*)context;
    service->InputsService_AssociateWindow = WindowsInputsServiceAssociateWindowInterop;
    service->InputsService_GetInputsState = WindowsInputsServiceGetInputsStateInterop;
    service->InputsService_SendVibrationCommand = WindowsInputsServiceSendVibrationCommandInterop;
}

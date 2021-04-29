#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

class WindowsInputsService
{
    public:
        WindowsInputsService();

        void AssociateWindow(void* windowPointer);
        struct InputsState GetInputsState();
        void SendVibrationCommand(uint32_t playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint32_t duration10ms);

        void UpdateRawInputKeyboardState(UINT message, uint16_t currentVKey);

    private:
        InputsState inputState;

        void* rawInputBuffer;
	    uint32_t rawInputBufferSize;

        void InitRawInput(HWND window);
        void UpdateRawInputState();
        void UpdateRawInputKeyboardButtonState(UINT message, uint16_t currentVKey, uint16_t vKey, InputsObject* keyboardButtonState);
        void UpdateRawInputKeyboardState(const RAWKEYBOARD& rawKeyboardData, InputsKeyboard* keyboardState);
};

WindowsInputsService* globalInputService;
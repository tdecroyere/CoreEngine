#pragma once
#include "WindowsCommon.h"
#include "WindowsInputsService.h"

typedef unsigned long QWORD;

WindowsInputsService::WindowsInputsService()
{
    this->inputState = {};
    this->rawInputBuffer = nullptr;
    this->rawInputBufferSize = 0;

	globalInputService = this;
}

void WindowsInputsService::AssociateWindow(void* windowPointer)
{
    InitRawInput((HWND)windowPointer);
}

InputsState WindowsInputsService::GetInputsState()
{
    UpdateRawInputState();
    InputsState output = this->inputState;
	
	// TODO: Reset transition for other inputs
	this->inputState.Keyboard.Space.TransitionCount = 0;
	this->inputState.Keyboard.F1.TransitionCount = 0;
	this->inputState.Keyboard.F2.TransitionCount = 0;
	this->inputState.Keyboard.F3.TransitionCount = 0;
		
	return output;
}

void WindowsInputsService::SendVibrationCommand(uint32_t playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, uint32_t duration10ms)
{

}

void WindowsInputsService::InitRawInput(HWND window)
{
	// TODO: RawInput is disabled for now...
	
	RAWINPUTDEVICE rawInputDevices[2];

	rawInputDevices[0].usUsagePage = 0x01;
	rawInputDevices[0].usUsage = 0x02;
	rawInputDevices[0].dwFlags = RIDEV_INPUTSINK;
	rawInputDevices[0].hwndTarget = window;

	rawInputDevices[1].usUsagePage = 0x01;
	rawInputDevices[1].usUsage = 0x06;
	rawInputDevices[1].dwFlags = RIDEV_INPUTSINK;
	rawInputDevices[1].hwndTarget = window;

	// AssertIfFailed(RegisterRawInputDevices(rawInputDevices, 2, sizeof(RAWINPUTDEVICE)));
}

void WindowsInputsService::UpdateRawInputState()
{
	if (this->rawInputBuffer == nullptr)
	{
		// Ask for RawInput for the size of the RAWINPUT structure that is dependant of the architecture
		// of the current machine
		if (GetRawInputBuffer(nullptr, &this->rawInputBufferSize, sizeof(RAWINPUTHEADER)) == 0)
		{
			// NOTE: The documentation says that we need to multiply the returned size by 8.
			// We then create a buffer of 16 slots.
			rawInputBufferSize *= 8 * 16;
		}

		// Allocate RawInput buffer
		this->rawInputBuffer = VirtualAlloc(0, this->rawInputBufferSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
	}

    if (this->rawInputBuffer == nullptr)
    {
        return;
    }

    // We need to copy the size of the input buffer, because GetRawInputBuffer may overwrite it.
    uint32_t inputBufferSize = this->rawInputBufferSize;
    int32_t count = 1;

    while (count > 0)
    {
        count = GetRawInputBuffer((PRAWINPUT)this->rawInputBuffer, &inputBufferSize, sizeof(RAWINPUTHEADER));
        assert(count != -1);

        // If there was an error or there are no Inputs, exit the loop.
        if (count <= 0)
        {
            break;
        }

        auto rawData = (PRAWINPUT)this->rawInputBuffer;

        for (int i = 0; i < count; i++)
        {
            if (rawData->header.dwType == RIM_TYPEKEYBOARD)
            {
                UpdateRawInputKeyboardState(rawData->data.keyboard, &this->inputState.Keyboard);
            }

            // else if (rawData->header.dwType == RIM_TYPEMOUSE)
            // {
            // 	Win32UpdateRawInputMouseState(rawData->data.mouse, &gameInput->Mouse);
            // }

            // Update the pointer to the next RAWINPUT structure. This is needed because 
            // the structure size vary depending on architecture of the running computer
            rawData = NEXTRAWINPUTBLOCK(rawData);
        }
    }
}

void WindowsInputsService::UpdateRawInputKeyboardButtonState(UINT message, uint16_t currentVKey, uint16_t vKey, InputsObject* keyboardButtonState)
{
	if (currentVKey == vKey)
	{
		auto newValue = (message == WM_KEYDOWN) ? 1.0f : 0.0f;

		keyboardButtonState->TransitionCount += (keyboardButtonState->Value != newValue) ? 1 : 0;
		keyboardButtonState->Value = newValue;
	}
}

void WindowsInputsService::UpdateRawInputKeyboardState(UINT message, uint16_t currentVKey)
{
	RAWKEYBOARD raw = {};
	raw.Message = message;
	raw.VKey = currentVKey;

	UpdateRawInputKeyboardState(raw, &this->inputState.Keyboard);
}

void WindowsInputsService::UpdateRawInputKeyboardState(const RAWKEYBOARD& rawKeyboardData, InputsKeyboard* keyboardState)
{
	// TODO: Handle all the keys in the real production code

	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'A', &keyboardState->KeyA);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'B', &keyboardState->KeyB);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'C', &keyboardState->KeyC);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'D', &keyboardState->KeyD);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'E', &keyboardState->KeyE);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'F', &keyboardState->KeyF);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'G', &keyboardState->KeyG);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'H', &keyboardState->KeyH);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'I', &keyboardState->KeyI);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'J', &keyboardState->KeyJ);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'K', &keyboardState->KeyK);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'L', &keyboardState->KeyL);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'M', &keyboardState->KeyM);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'N', &keyboardState->KeyN);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'O', &keyboardState->KeyO);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'P', &keyboardState->KeyP);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'Q', &keyboardState->KeyQ);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'R', &keyboardState->KeyR);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'S', &keyboardState->KeyS);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'T', &keyboardState->KeyT);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'U', &keyboardState->KeyU);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'V', &keyboardState->KeyV);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'W', &keyboardState->KeyW);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'X', &keyboardState->KeyX);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'Y', &keyboardState->KeyY);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, 'Z', &keyboardState->KeyZ);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_LEFT, &keyboardState->LeftArrow);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_RIGHT, &keyboardState->RightArrow);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_UP, &keyboardState->UpArrow);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_DOWN, &keyboardState->DownArrow);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_SPACE, &keyboardState->Space);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_MENU, &keyboardState->AlternateKey);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_RETURN, &keyboardState->Enter);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F1, &keyboardState->F1);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F2, &keyboardState->F2);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F3, &keyboardState->F3);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F4, &keyboardState->F4);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F5, &keyboardState->F5);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F6, &keyboardState->F6);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F7, &keyboardState->F7);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F8, &keyboardState->F8);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F9, &keyboardState->F9);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F10, &keyboardState->F10);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F11, &keyboardState->F11);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_F12, &keyboardState->F12);
	UpdateRawInputKeyboardButtonState(rawKeyboardData.Message, rawKeyboardData.VKey, VK_SHIFT, &keyboardState->Shift);
}
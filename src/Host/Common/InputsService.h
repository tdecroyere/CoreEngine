#pragma once
#include "CoreEngine.h"

enum InputsObjectType : int
{
    Digital, 
    Analog, 
    Relative
};

struct InputsObject
{
    enum InputsObjectType ObjectType;
    int TransitionCount;
    float Value;
};

struct NullableInputsObject
{
    int HasValue;
    struct InputsObject Value;
};

struct InputsKeyboard
{
    struct InputsObject KeyA;
    struct InputsObject KeyB;
    struct InputsObject KeyC;
    struct InputsObject KeyD;
    struct InputsObject KeyE;
    struct InputsObject KeyF;
    struct InputsObject KeyG;
    struct InputsObject KeyH;
    struct InputsObject KeyI;
    struct InputsObject KeyJ;
    struct InputsObject KeyK;
    struct InputsObject KeyL;
    struct InputsObject KeyM;
    struct InputsObject KeyN;
    struct InputsObject KeyO;
    struct InputsObject KeyP;
    struct InputsObject KeyQ;
    struct InputsObject KeyR;
    struct InputsObject KeyS;
    struct InputsObject KeyT;
    struct InputsObject KeyU;
    struct InputsObject KeyV;
    struct InputsObject KeyW;
    struct InputsObject KeyX;
    struct InputsObject KeyY;
    struct InputsObject KeyZ;
    struct InputsObject Space;
    struct InputsObject AlternateKey;
    struct InputsObject Enter;
    struct InputsObject F1;
    struct InputsObject F2;
    struct InputsObject F3;
    struct InputsObject F4;
    struct InputsObject F5;
    struct InputsObject F6;
    struct InputsObject F7;
    struct InputsObject F8;
    struct InputsObject F9;
    struct InputsObject F10;
    struct InputsObject F11;
    struct InputsObject F12;
    struct InputsObject Shift;
    struct InputsObject LeftArrow;
    struct InputsObject RightArrow;
    struct InputsObject UpArrow;
    struct InputsObject DownArrow;
};

struct NullableInputsKeyboard
{
    int HasValue;
    struct InputsKeyboard Value;
};

struct InputsMouse
{
    unsigned int PositionX;
    unsigned int PositionY;
    struct InputsObject DeltaX;
    struct InputsObject DeltaY;
    struct InputsObject LeftButton;
    struct InputsObject RightButton;
    struct InputsObject MiddleButton;
};

struct NullableInputsMouse
{
    int HasValue;
    struct InputsMouse Value;
};

struct InputsTouch
{
    struct InputsObject DeltaX;
    struct InputsObject DeltaY;
};

struct NullableInputsTouch
{
    int HasValue;
    struct InputsTouch Value;
};

struct InputsGamepad
{
    unsigned int PlayerId;
    int IsConnected;
    struct InputsObject LeftStickUp;
    struct InputsObject LeftStickDown;
    struct InputsObject LeftStickLeft;
    struct InputsObject LeftStickRight;
    struct InputsObject RightStickUp;
    struct InputsObject RightStickDown;
    struct InputsObject RightStickLeft;
    struct InputsObject RightStickRight;
    struct InputsObject LeftTrigger;
    struct InputsObject RightTrigger;
    struct InputsObject Button1;
    struct InputsObject Button2;
    struct InputsObject Button3;
    struct InputsObject Button4;
    struct InputsObject ButtonStart;
    struct InputsObject ButtonBack;
    struct InputsObject ButtonSystem;
    struct InputsObject ButtonLeftStick;
    struct InputsObject ButtonRightStick;
    struct InputsObject LeftShoulder;
    struct InputsObject RightShoulder;
    struct InputsObject DPadUp;
    struct InputsObject DPadDown;
    struct InputsObject DPadLeft;
    struct InputsObject DPadRight;
};

struct NullableInputsGamepad
{
    int HasValue;
    struct InputsGamepad Value;
};

struct InputsState
{
    struct InputsKeyboard Keyboard;
    struct InputsMouse Mouse;
    struct InputsTouch Touch;
    struct InputsGamepad Gamepad1;
    struct InputsGamepad Gamepad2;
    struct InputsGamepad Gamepad3;
    struct InputsGamepad Gamepad4;
};

struct NullableInputsState
{
    int HasValue;
    struct InputsState Value;
};

typedef void (*InputsService_AssociateWindowPtr)(void* context, void* windowPointer);
typedef struct InputsState (*InputsService_GetInputsStatePtr)(void* context);
typedef void (*InputsService_SendVibrationCommandPtr)(void* context, unsigned int playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned int duration10ms);

struct InputsService
{
    void* Context;
    InputsService_AssociateWindowPtr InputsService_AssociateWindow;
    InputsService_GetInputsStatePtr InputsService_GetInputsState;
    InputsService_SendVibrationCommandPtr InputsService_SendVibrationCommand;
};

#pragma once

struct Span
{
	unsigned char* Buffer;
	int Length;
};

struct Vector4
{
    float X, Y, Z, W;
};

struct Matrix4x4
{
    float Item00, Item01, Item02, Item03;
    float Item10, Item11, Item12, Item13;
    float Item20, Item21, Item22, Item23;
    float Item30, Item31, Item32, Item33;
};

typedef void (*DebugDrawTrianglePtr)(void* graphicsContext, struct Vector4 color1, struct Vector4 color2, struct Vector4 color3, struct Matrix4x4 worldMatrix);

struct GraphicsService
{
    void* GraphicsContext;
    DebugDrawTrianglePtr DebugDrawTriangle;
};


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

struct InputsMouse
{
    unsigned int PositionX;
    unsigned int PositionY;

    struct InputsObject DeltaX;
    struct InputsObject DeltaY;
    struct InputsObject LeftButton;
    struct InputsObject RightButton;
    struct InputsObject MiddleButton;

    // TODO: Handle wheel
};

struct InputsTouch
{
    struct InputsObject DeltaX;
    struct InputsObject DeltaY;
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

typedef struct InputsState (*GetInputsStatePtr)(void* inputsContext);
typedef void (*SendVibrationCommandPtr)(void* inputsContext, unsigned char playerId, float leftTriggerMotor, float rightTriggerMotor, float leftStickMotor, float rightStickMotor, unsigned char duration10ms);

struct InputsService
{
    void* InputsContext;
    GetInputsStatePtr GetInputsState;
    SendVibrationCommandPtr SendVibrationCommand;
};

typedef int (*AddTestHostMethodPtr)(int a, int b);
typedef struct Span (*GetTestBufferPtr)();

struct HostPlatform
{
	int TestParameter;
	AddTestHostMethodPtr AddTestHostMethod;
	GetTestBufferPtr GetTestBuffer;
    struct GraphicsService GraphicsService;
    struct InputsService InputsService;
};

typedef void (*StartEnginePtr)(char* appName, struct HostPlatform* hostPlatform);
typedef void (*UpdateEnginePtr)(float deltaTime);


typedef int (*coreclr_initialize_ptr)(const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

typedef int (*coreclr_create_delegate_ptr)(void* hostHandle,
            unsigned int domainId,
            const char* entryPointAssemblyName,
            const char* entryPointTypeName,
            const char* entryPointMethodName,
            void** delegate);

typedef int (*coreclr_shutdown_ptr)(void* hostHandle,
            unsigned int domainId);
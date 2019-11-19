#pragma once

struct HostMemoryBuffer
{
    unsigned int Id;
    unsigned char* Pointer;
    unsigned int Length;
};

typedef struct HostMemoryBuffer (*CreateMemoryBufferPtr)(void* memoryManagerContext, unsigned int length);
typedef void (*DestroyMemoryBufferPtr)(void* memoryManagerContext, unsigned int memoryBufferId);

struct MemoryService
{
    void* MemoryManagerContext;
    CreateMemoryBufferPtr CreateMemoryBuffer;
    DestroyMemoryBufferPtr DestroyMemoryBuffer;
};

struct Vector2
{
    float X, Y;
};

typedef struct Vector2 (*GetRenderSizePtr)(void* graphicsContext);
typedef unsigned int (*CreateShaderPtr)(void* graphicsContext, void* shaderByteCodeData, int shaderByteCodeLength);
typedef unsigned int (*CreateShaderParametersPtr)(void* graphicsContext, unsigned int graphicsBuffer1, unsigned int graphicsBuffer2, unsigned int graphicsBuffer3); 
typedef unsigned int (*CreateStaticGraphicsBufferPtr)(void* graphicsContext, void* data, int length);
typedef unsigned int (*CreateDynamicGraphicsBufferPtr)(void* graphicsContext, unsigned int length);
typedef void (*UploadDataToGraphicsBufferPtr)(void* graphicsContext, unsigned int graphicsBufferId, void* data, int length);
typedef void (*BeginCopyGpuDataPtr)(void* graphicsContext);
typedef void (*EndCopyGpuDataPtr)(void* graphicsContext);
typedef void (*BeginRenderPtr)(void* graphicsContext);
typedef void (*EndRenderPtr)(void* graphicsContext);
typedef void (*DrawPrimitivesPtr)(void* graphicsContext, unsigned int startIndex, unsigned int indexCount, unsigned int vertexBufferId, unsigned int indexBufferId, unsigned int baseInstanceId);

struct GraphicsService
{
    void* GraphicsContext;
    GetRenderSizePtr GetRenderSize;
    CreateShaderPtr CreateShader;
    CreateShaderParametersPtr CreateShaderParameters;
    CreateStaticGraphicsBufferPtr CreateStaticGraphicsBuffer;
    CreateDynamicGraphicsBufferPtr CreateDynamicGraphicsBuffer;
    UploadDataToGraphicsBufferPtr UploadDataToGraphicsBuffer;
    BeginCopyGpuDataPtr BeginCopyGpuData;
    EndCopyGpuDataPtr EndCopyGpuData;
    BeginRenderPtr BeginRender;
    EndRenderPtr EndRender;
    DrawPrimitivesPtr DrawPrimitives;
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

struct HostPlatform
{
    struct MemoryService MemoryService;
    struct GraphicsService GraphicsService;
    struct InputsService InputsService;
};

typedef void (*StartEnginePtr)(char* appName, struct HostPlatform* hostPlatform);
typedef void (*UpdateEnginePtr)(float deltaTime);
typedef void (*RenderPtr)();


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
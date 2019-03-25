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


enum GameInputObjectType : int
{
    Digital,
    Analog,
    Relative
};

struct GameInputObject
{
    enum GameInputObjectType ObjectType;
    int TransitionCount;
    float Value;
};

struct GameInputKeyboard
{
    struct GameInputObject KeyA;
    struct GameInputObject KeyB;
    struct GameInputObject KeyC;
    struct GameInputObject KeyD;
    struct GameInputObject KeyE;
    struct GameInputObject KeyF;
    struct GameInputObject KeyG;
    struct GameInputObject KeyH;
    struct GameInputObject KeyI;
    struct GameInputObject KeyJ;
    struct GameInputObject KeyK;
    struct GameInputObject KeyL;
    struct GameInputObject KeyM;
    struct GameInputObject KeyN;
    struct GameInputObject KeyO;
    struct GameInputObject KeyP;
    struct GameInputObject KeyQ;
    struct GameInputObject KeyR;
    struct GameInputObject KeyS;
    struct GameInputObject KeyT;
    struct GameInputObject KeyU;
    struct GameInputObject KeyV;
    struct GameInputObject KeyW;
    struct GameInputObject KeyX;
    struct GameInputObject KeyY;
    struct GameInputObject KeyZ;
    struct GameInputObject Space;
    struct GameInputObject AlternateKey;
    struct GameInputObject Enter;
    struct GameInputObject F1;
    struct GameInputObject F2;
    struct GameInputObject F3;
    struct GameInputObject F4;
    struct GameInputObject F5;
    struct GameInputObject F6;
    struct GameInputObject F7;
    struct GameInputObject F8;
    struct GameInputObject F9;
    struct GameInputObject F10;
    struct GameInputObject F11;
    struct GameInputObject F12;
    struct GameInputObject Shift;
};

struct GameInputMouse
{
    unsigned int PositionX;
    unsigned int PositionY;

    struct GameInputObject DeltaX;
    struct GameInputObject DeltaY;
    struct GameInputObject LeftButton;
    struct GameInputObject RightButton;
    struct GameInputObject MiddleButton;

    // TODO: Handle wheel
};

struct GameInputTouch
{
    struct GameInputObject DeltaX;
    struct GameInputObject DeltaY;
};

struct GameInputGamepad
{
    int IsConnected;
    struct GameInputObject LeftStickUp;
    struct GameInputObject LeftStickDown;
    struct GameInputObject LeftStickLeft;
    struct GameInputObject LeftStickRight;
    struct GameInputObject RightStickUp;
    struct GameInputObject RightStickDown;
    struct GameInputObject RightStickLeft;
    struct GameInputObject RightStickRight;
    struct GameInputObject LeftTrigger;
    struct GameInputObject RightTrigger;
    struct GameInputObject ButtonA;
    struct GameInputObject ButtonB;
    struct GameInputObject ButtonX;
    struct GameInputObject ButtonY;
    struct GameInputObject ButtonStart;
    struct GameInputObject ButtonBack;
    struct GameInputObject LeftShoulder;
    struct GameInputObject RightShoulder;
    struct GameInputObject DPadUp;
    struct GameInputObject DPadDown;
    struct GameInputObject DPadLeft;
    struct GameInputObject DPadRight;
};

struct InputsService
{
    struct GameInputKeyboard Keyboard;
    struct GameInputMouse Mouse;
    struct GameInputTouch Touch;
    struct GameInputGamepad GamePad1;
    struct GameInputGamepad GamePad2;
    struct GameInputGamepad GamePad3;
    struct GameInputGamepad GamePad4;
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
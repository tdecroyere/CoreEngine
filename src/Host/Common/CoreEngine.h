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

typedef int (*AddTestHostMethodPtr)(int a, int b);
typedef struct Span (*GetTestBufferPtr)();

struct HostPlatform
{
	int TestParameter;
	AddTestHostMethodPtr AddTestHostMethod;
	GetTestBufferPtr GetTestBuffer;
    struct GraphicsService GraphicsService;
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
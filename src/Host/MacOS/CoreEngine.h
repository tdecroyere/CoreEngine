struct Span
{
	unsigned char* Buffer;
	int Length;
};

struct Vector4
{
    float X;
    float Y;
    float Z;
    float W;
};

struct Matrix4x4
{
    float Item00;
    float Item01;
    float Item02;
    float Item03;

    float Item10;
    float Item11;
    float Item12;
    float Item13;

    float Item20;
    float Item21;
    float Item22;
    float Item23;

    float Item30;
    float Item31;
    float Item32;
    float Item33;
};

struct GraphicsService
{
    void* DebugDrawTriangle;
};

struct HostPlatform
{
	int TestParameter;
	void* AddTestHostMethod;
	void* GetTestBuffer;
    struct GraphicsService GraphicsService;
};

typedef int (*AddTestHostMethodPtr)(int a, int b);
typedef struct Span (*GetTestBufferPtr)();

typedef void (*StartEnginePtr)(char* appName, struct HostPlatform* hostPlatform);
typedef void (*UpdateEnginePtr)(float deltaTime);
typedef void (*DebugDrawTrianglePtr)(struct Vector4 color1, struct Vector4 color2, struct Vector4 color3, struct Matrix4x4 worldMatrix);





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
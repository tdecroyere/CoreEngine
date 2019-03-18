struct Span
{
	unsigned char* Buffer;
	int Length;
};

struct HostPlatform
{
	int TestParameter;
	void* AddTestHostMethod;
	void* GetTestBuffer;
};

typedef int (*AddTestHostMethodPtr)(int a, int b);
typedef struct Span (*GetTestBufferPtr)();

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
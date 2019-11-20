#pragma once

struct Vector2
{
    float X, Y;
};

#include "GraphicsService.h"
#include "InputsService.h"

struct HostPlatform
{
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
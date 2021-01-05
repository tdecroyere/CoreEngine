#pragma once

struct Vector2
{
    float X, Y;
};

struct Vector3
{
    float X, Y, Z;
};

struct Vector4
{
    float X, Y, Z, W;
};

struct Matrix4x4
{
    float M11, M12, M13, M14;
    float M21, M22, M23, M24;
    float M31, M32, M33, M34;
    float M41, M42, M43, M44;
};

struct NullableIntPtr
{
    int HasValue;
    void* Value;
};

struct Nullableint
{
    int HasValue;
    int Value;
};

struct Nullableuint
{
    int HasValue;
    unsigned int Value;
};

struct NullableVector4
{
    int HasValue;
    struct Vector4 Value;
};

enum GraphicsTextureFormat : int;

struct NullableGraphicsTextureFormat
{
    int HasValue;
    enum GraphicsTextureFormat Value;
};

enum GraphicsBlendOperation : int;

struct NullableGraphicsBlendOperation
{
    int HasValue;
    enum GraphicsBlendOperation Value;
};

#include "NativeUIService.h"
#include "GraphicsService.h"
#include "InputsService.h"


struct HostPlatform
{
    struct NativeUIService NativeUIService;
    struct GraphicsService GraphicsService;
    struct InputsService InputsService;
};

typedef void (*StartEnginePtr)(struct HostPlatform hostPlatform);
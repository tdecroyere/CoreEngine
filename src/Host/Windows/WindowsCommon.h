#pragma once

#include <Windows.h>
#include <string>
#include <stdint.h>
#include "../Common/CoreEngine.h"

typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

typedef uint8_t bool8;
typedef uint32_t bool32;

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;

typedef size_t memindex;

typedef float real32;
typedef double real64;


#define ArrayCount(value) ((sizeof(value) / sizeof(value[0])))
#define Kilobytes(value) ((value) * 1024LL)
#define Megabytes(value) (Kilobytes(value) * 1024LL)
#define Gigabytes(value) (Megabytes(value) * 1024LL)
#define Terabytes(value) (Gigabytes(value) * 1024LL)

#define internal static
#define global static

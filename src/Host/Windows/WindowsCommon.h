#pragma once

#include <stdint.h>
#include <sys/stat.h>

#include <locale>
#include <codecvt>
#include <string>

#include <map>
#include <vector>
#include <stack>
#include <assert.h>

#include <ShellScalingAPI.h>
#include <Windows.h>
#include <Uxtheme.h>
#include <wrl/client.h>
#include "d3d12.h"
#include <d3dcompiler.h>

#if defined(NTDDI_WIN10_RS2)
#include <dxgi1_6.h>
#include <dxgidebug.h>
#else
#include <dxgi1_5.h>
#endif

#define AssertIfFailed(result) assert(!FAILED(result))
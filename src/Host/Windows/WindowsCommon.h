#pragma once

#include <string>

#include <ShellScalingAPI.h>
#include <Windows.h>
#include <d3d12.h>
#include <d3dcompiler.h>

#if defined(NTDDI_WIN10_RS2)
#include <dxgi1_6.h>
#else
#include <dxgi1_5.h>
#endif

#pragma once
#include "WindowsCommon.h"
#include "WindowsInputsService.h"

// SetProcessDPIAwareness function pointer definition
typedef HRESULT WINAPI Set_Process_DPI_Awareness(PROCESS_DPI_AWARENESS value);

bool isAppActive = true;
bool doChangeSize = false;
WINDOWPLACEMENT previousWindowPlacement;

bool firstRun = true;

void Win32SwitchScreenMode(HWND window)
{
	DWORD windowStyle = GetWindowLongA(window, GWL_STYLE);

	if (windowStyle & WS_OVERLAPPEDWINDOW) 
	{
		MONITORINFO monitorInfos = { sizeof(monitorInfos) };

		if (GetWindowPlacement(window, &previousWindowPlacement) &&
			GetMonitorInfoA(MonitorFromWindow(window, MONITOR_DEFAULTTOPRIMARY), &monitorInfos))
		{ 
			SetWindowLongA(window, GWL_STYLE, windowStyle & ~WS_OVERLAPPEDWINDOW);
			SetWindowPos(window, HWND_TOP,
				monitorInfos.rcMonitor.left, monitorInfos.rcMonitor.top,
				monitorInfos.rcMonitor.right - monitorInfos.rcMonitor.left,
				monitorInfos.rcMonitor.bottom - monitorInfos.rcMonitor.top,
				SWP_NOOWNERZORDER | SWP_FRAMECHANGED);

			ShowWindow(window, SW_MAXIMIZE);
		}
	}

	else
	{
		SetWindowLongA(window, GWL_STYLE, windowStyle | WS_OVERLAPPEDWINDOW);
		SetWindowPlacement(window, &previousWindowPlacement);
		SetWindowPos(window, NULL, 0, 0, 0, 0,
					 SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
	}
}

LRESULT CALLBACK Win32WindowCallBack(HWND window, UINT message, WPARAM wParam, LPARAM lParam)
{
	// TODO: Reset input devices status on re-activation
	// TODO: For input devices, try to find a way to avoid global variables? 
	// It is complicated because we cannot modify the signature of the WNDPROC function

	switch (message)
	{
	case WM_CREATE:
	{
		if (g_darkModeSupported)
		{
			_AllowDarkModeForWindow(window, true);
			RefreshTitleBarThemeColor(window);
		}
	}
	case WM_ACTIVATE:
	{
		isAppActive = !(wParam == WA_INACTIVE);
		break;
	}
	case WM_KEYDOWN:
	{
		bool alt = (::GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
	
		switch (wParam)
		{
		case VK_ESCAPE:
			::PostQuitMessage(0);
			break;
		case VK_RETURN:
			if (alt)
			{
				Win32SwitchScreenMode(window);
			}
			break;
		}

		if (globalInputService != nullptr)
		{
			globalInputService->UpdateRawInputKeyboardState(WM_KEYDOWN, wParam);
		}
		break;
	}
	case WM_KEYUP:
	{
		if (globalInputService != nullptr)
		{
			globalInputService->UpdateRawInputKeyboardState(WM_KEYUP, wParam);
		}
		break;
	}
	case WM_SIZE:
	{
		doChangeSize = true;
		// TODO: Handle minimized state
		break;
	}
    case WM_DPICHANGED:
    {
        RECT* const prcNewWindow = (RECT*)lParam;
        SetWindowPos(window,
            NULL,
            prcNewWindow ->left,
            prcNewWindow ->top,
            prcNewWindow->right - prcNewWindow->left,
            prcNewWindow->bottom - prcNewWindow->top,
            SWP_NOZORDER | SWP_NOACTIVATE);

        break;
    }
	case WM_CLOSE:
	case WM_DESTROY:
	{
		PostQuitMessage(0);
		break;
	}
	default:
		return DefWindowProcA(window, message, wParam, lParam);
	}

	return 0;
}

bool Win32ProcessMessage(const MSG& message)
{
	if (message.message == WM_QUIT)
	{
		return false;
	}

	TranslateMessage(&message);
	DispatchMessageA(&message);

	return true;
}

bool Win32ProcessPendingMessages()
{
	bool gameRunning = true;
	MSG message;

	// NOTE: The 2 loops are needed only because of RawInput which require that we let the WM_INPUT messages
	// in the windows message queue...
	// while (PeekMessageA(&message, nullptr, 0, WM_INPUT - 1, PM_REMOVE))
	// {
	// 	gameRunning = Win32ProcessMessage(message);
	// }

	// while (PeekMessageA(&message, nullptr, WM_INPUT + 1, 0xFFFFFFFF, PM_REMOVE))
	// {
	// 	gameRunning = Win32ProcessMessage(message);
	// }

	while (PeekMessageA(&message, nullptr, 0, 0, PM_REMOVE))
	{
		gameRunning = Win32ProcessMessage(message);
	}

	return gameRunning;
}
#pragma once

#include "WindowsCommon.h"

using namespace std;

internal LRESULT CALLBACK Win32WindowCallBack(HWND window, UINT message, WPARAM wParam, LPARAM lParam)
{
	// TODO: Reset input devices status on re-activation
	// TODO: For input devices, try to find a way to avoid global variables? 
	// It is complicated because we cannot modify the signature of the WNDPROC function

	switch (message)
	{
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
	case WM_ACTIVATE:
	{
		if (wParam == WA_INACTIVE)
		{
			// globalCoreAudio->AudioClient->Stop();
			
			// globalCoreAudio->SoundIsPlaying = false;
			// globalPlatform->Application.IsActive = false;
		}

		else
		{
			// if (globalPlatform)
			// {
			// 	globalPlatform->Application.IsActive = true;
			// }
		}
		break;
	}
	case WM_TOUCH:
	{
		// TODO: Debug windows touch not sending messages
		//Win32UpdateWindowsTouchState((HTOUCHINPUT)lParam, LOWORD(wParam), globalGameInput, globalWindowsTouchState);
		break;
	}
	case WM_SIZE:
	{
		// TODO: Handle minimized state

		// if (globalDirect3D12)
		// {
		// 	RECT clientRect = {};
		// 	GetClientRect(window, &clientRect);

		// 	uint32 windowWidth = clientRect.right - clientRect.left;
		// 	uint32 windowHeight = clientRect.bottom - clientRect.top;

		// 	if (windowWidth >= globalDirect3D12->Texture.Width * 2 && windowHeight >= globalDirect3D12->Texture.Height * 2)
		// 	{
		// 		globalDirect3D12->Width = windowWidth;
		// 		globalDirect3D12->Height = windowHeight;
		// 	}

		// 	else
		// 	{
		// 		globalDirect3D12->Width = globalDirect3D12->Texture.Width;
		// 		globalDirect3D12->Height = globalDirect3D12->Texture.Height;
		// 	}

		// 	Direct3D12InitSizeDependentResources(globalDirect3D12);
		// }
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

internal HWND Win32InitWindow(HINSTANCE applicationInstance, LPSTR windowName, int width, int height)
{
	// Declare window class
	WNDCLASSA windowClass {};
	windowClass.style = CS_HREDRAW | CS_VREDRAW;
	windowClass.lpfnWndProc = Win32WindowCallBack;
	windowClass.hInstance = applicationInstance;
	windowClass.lpszClassName = "CoreEngineWindowClass";
	windowClass.hCursor = LoadCursorA(NULL, IDC_ARROW);

	if (RegisterClassA(&windowClass))
	{
		SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        
        UINT dpi = GetDpiForWindow(GetDesktopWindow());

        float scaling_factor = (float)dpi / 96;

		// Ajust the client area based on the style of the window
        RECT clientRectangle;
        clientRectangle.left = 0;
        clientRectangle.top = 0;
        clientRectangle.right = (long)(width * scaling_factor);
        clientRectangle.bottom = (long)(height * scaling_factor);

        AdjustWindowRectExForDpi(&clientRectangle, 0, false, 0, dpi);

		width = clientRectangle.right - clientRectangle.left;
		height = clientRectangle.bottom - clientRectangle.top;

		// Compute the position of the window to center it 
		RECT desktopRectangle;
		GetClientRect(GetDesktopWindow(), &desktopRectangle);
		int x = (desktopRectangle.right / 2) - (width / 2);
		int y = (desktopRectangle.bottom / 2) - (height / 2);

		// Create the window
		HWND window = CreateWindowExA(0,
			"CoreEngineWindowClass",
			windowName,
			WS_OVERLAPPEDWINDOW | WS_VISIBLE,
			x,
			y,
			width,
			height,
			0,
			0,
			applicationInstance,
			0);

		return window;
	}

	return 0;
}

internal bool32 Win32ProcessMessage(const MSG& message)
{
	if (message.message == WM_QUIT)
	{
		return false;
	}

	TranslateMessage(&message);
	DispatchMessageA(&message);

	return true;
}

internal bool32 Win32ProcessPendingMessages()
{
	bool32 gameRunning = true;
	MSG message;

	// NOTE: The 2 loops are needed only because of RawInput which require that we let the WM_INPUT messages
	// in the windows message queue...
	while (PeekMessageA(&message, nullptr, 0, WM_INPUT - 1, PM_REMOVE))
	{
		gameRunning = Win32ProcessMessage(message);
	}

	while (PeekMessageA(&message, nullptr, WM_INPUT + 1, 0xFFFFFFFF, PM_REMOVE))
	{
		gameRunning = Win32ProcessMessage(message);
	}

	return gameRunning;
}

int CALLBACK WinMain(HINSTANCE applicationInstance, HINSTANCE, LPSTR, int)
{
	HWND window = Win32InitWindow(applicationInstance, "Core Engine", 1280, 720);
	
	if (window)
	{
        bool32 gameRunning = true;

        while (gameRunning)
        {
            gameRunning = Win32ProcessPendingMessages();

            if (gameRunning)
            {
                printf("OK\n");
            }
        }
    }
}

int main(int argc, char const *argv[])
{
    printf("CoreEngine Windows Host\n");

    string appName = string();

    if (argc > 1)
    {
        appName = string(argv[1]);
    }

    WindowsCoreEngineHost* coreEngineHost = new WindowsCoreEngineHost();
    coreEngineHost->StartEngine(appName);

    // TODO: To remove, this is only used for debugging with console messages
    WinMain(GetModuleHandle(NULL), NULL, GetCommandLineA(), SW_SHOWNORMAL);

    coreEngineHost->UpdateEngine(5);

	printf("CoreEngine Windows Host has ended.\n");
	getchar();

    return 0;
}
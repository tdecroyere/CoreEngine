#include "WindowsCommon.h"
#include <stdio.h>
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "CoreEngineHost.h"

// SetProcessDPIAwareness function pointer definition
typedef HRESULT WINAPI Set_Process_DPI_Awareness(PROCESS_DPI_AWARENESS value);

bool isAppActive = true;
bool doChangeSize = false;
WINDOWPLACEMENT previousWindowPlacement;

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

HWND Win32InitWindow(HINSTANCE applicationInstance, LPSTR windowName, int width, int height)
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
		// Setup the application to ajust its resolution based on windows scaling settings
		// if it is available
		HMODULE shcoreLibrary = LoadLibraryA("shcore.dll");

		// TODO: Account for larger DPI screens and do something better for them. Better
		// Asset resolution?

		SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        
		// Ajust the client area based on the style of the window
        UINT dpi = GetDpiForWindow(GetDesktopWindow());

        float scaling_factor = static_cast<float>(dpi) / 96;

        RECT clientRectangle;
        clientRectangle.left = 0;
        clientRectangle.top = 0;
        clientRectangle.right = static_cast<LONG>(width * scaling_factor);
        clientRectangle.bottom = static_cast<LONG>(height * scaling_factor);

        AdjustWindowRectExForDpi(&clientRectangle, WS_OVERLAPPEDWINDOW, false, 0, dpi);

		// RECT clientRectangle = { 0, 0, width, height };
		// AdjustWindowRect(&clientRectangle, WS_OVERLAPPEDWINDOW, false);
		width = clientRectangle.right - clientRectangle.left;
		height = clientRectangle.bottom - clientRectangle.top;

		// Compute the position of the window to center it 
		RECT desktopRectangle;
		GetClientRect(GetDesktopWindow(), &desktopRectangle);
		int x = (desktopRectangle.right / 2) - (width / 2);
		int y = (desktopRectangle.bottom / 2) - (height / 2);

        printf("Width: %d, Height: %d (DPI: %d)\n", width, height, dpi);

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


// int CALLBACK WinMain(HINSTANCE applicationInstance, HINSTANCE, LPSTR, int)
int main(int argc, char const *argv[])
{
	GameState gameState = {};
	gameState.GameRunning = true;

	// HWND window = Win32InitWindow(applicationInstance, "Core Engine", 1280, 720);
	HWND window = Win32InitWindow(GetModuleHandle(NULL), "Core Engine", 1280, 720);

	RECT windowRectangle;
	GetClientRect(window, &windowRectangle);

    auto graphicsService = Direct3D12GraphicsService(window, windowRectangle.right - windowRectangle.left, windowRectangle.bottom - windowRectangle.top, &gameState);
    auto inputsService = WindowsInputsService(window);

    auto coreEngineHost = CoreEngineHost(graphicsService, inputsService);
    coreEngineHost.StartEngine("EcsTest");
	
	if (window)
	{
        while (gameState.GameRunning)
        {
            gameState.GameRunning = Win32ProcessPendingMessages();

            if (gameState.GameRunning)
            {
                if (isAppActive)
                {
					
                    //Win32UpdateRawInputState(&rawInput, &gameInput);

                    // TODO: Move system key processing into a separate function?
                    // Change display mode
                    // if (gameInput.Keyboard.AlternateKey.Value == 1.0f && gameInput.Keyboard.Enter.Value == 1.0f && gameInput.Keyboard.Enter.TransitionCount == 1.0f)
                    // {
                    //     if (!direct3D12.IsInitialized || forceGdi)
                    //     {
                    //         Win32SwitchScreenMode(&win32State);
                    //     }

                    //     else
                    //     {
                    //         Direct3D12SwitchScreenMode(&direct3D12);
                    //     }
                    // }

                    // Application Exit
                    // if (gameInput.Keyboard.AlternateKey.Value == 1.0f && gameInput.Keyboard.F4.Value == 1.0f && gameInput.Keyboard.F4.TransitionCount == 1.0f)
                    // {
                    //     gameRunning = false;
                    //     break;
                    // }
					
					coreEngineHost.UpdateEngine(1.0f / 60.0f);

					if (doChangeSize)
					{
						RECT clientRect = {};
						GetClientRect(window, &clientRect);

						auto windowWidth = clientRect.right - clientRect.left;
						auto windowHeight = clientRect.bottom - clientRect.top;

						graphicsService.CreateOrResizeSwapChain(windowWidth, windowHeight);
						doChangeSize = false;
					}
                }
            }
        }

		graphicsService.WaitForGlobalFence();
	}
}
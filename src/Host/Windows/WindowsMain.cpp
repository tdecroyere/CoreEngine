#include "WindowsCommon.h"
#include <stdio.h>
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "CoreEngineHost.h"

// SetProcessDPIAwareness function pointer definition
typedef HRESULT WINAPI Set_Process_DPI_Awareness(PROCESS_DPI_AWARENESS value);


bool isAppActive = true;

// void Win32SwitchScreenMode(Win32State* win32State)
// {
// 	DWORD windowStyle = GetWindowLongA(win32State->Window, GWL_STYLE);

// 	if (windowStyle & WS_OVERLAPPEDWINDOW) 
// 	{
// 		MONITORINFO monitorInfos = { sizeof(monitorInfos) };

// 		if (GetWindowPlacement(win32State->Window, &win32State->PreviousWindowPlacement) &&
// 			GetMonitorInfoA(MonitorFromWindow(win32State->Window, MONITOR_DEFAULTTOPRIMARY), &monitorInfos))
// 		{ 
// 			SetWindowLongA(win32State->Window, GWL_STYLE, windowStyle & ~WS_OVERLAPPEDWINDOW);
// 			SetWindowPos(win32State->Window, HWND_TOP,
// 				monitorInfos.rcMonitor.left, monitorInfos.rcMonitor.top,
// 				monitorInfos.rcMonitor.right - monitorInfos.rcMonitor.left,
// 				monitorInfos.rcMonitor.bottom - monitorInfos.rcMonitor.top,
// 				SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
// 		}
// 	}

// 	else
// 	{
// 		SetWindowLongA(win32State->Window, GWL_STYLE, windowStyle | WS_OVERLAPPEDWINDOW);
// 		SetWindowPlacement(win32State->Window, &win32State->PreviousWindowPlacement);
// 		SetWindowPos(win32State->Window, NULL, 0, 0, 0, 0,
// 					 SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
// 	}
// }


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

        printf("Size changed Width: %d, Height: %d (DPI: %d)\n", prcNewWindow->right - prcNewWindow->left, prcNewWindow->bottom - prcNewWindow->top, 0);

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
	// HWND window = Win32InitWindow(applicationInstance, "Core Engine", 1280, 720);
	HWND window = Win32InitWindow(GetModuleHandle(NULL), "Core Engine", 1280, 720);

    auto graphicsService = Direct3D12GraphicsService(window, 1280, 720);
    auto inputsService = WindowsInputsService();

    auto coreEngineHost = CoreEngineHost(graphicsService, inputsService);
    coreEngineHost.StartEngine("EcsTest");
	
	if (window)
	{
        bool gameRunning = true;

        while (gameRunning)
        {
            gameRunning = Win32ProcessPendingMessages();

            if (gameRunning)
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
					
					coreEngineHost.UpdateEngine(0);
                }

            }
        }
	}
}
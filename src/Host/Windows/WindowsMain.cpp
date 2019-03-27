#include <stdio.h>
#include <windows.h>
#include <string>
#include "WindowsMain.h"
#include "../Common/CoreEngine.h"

InputsState inputsState = {};


int AddTestHostMethod(int a, int b)
{
	return a + b;
}

Span GetTestBuffer()
{
	unsigned char* testBuffer = new unsigned char[5];

	testBuffer[0] = 1;
	testBuffer[1] = 2;
	testBuffer[2] = 3;
	testBuffer[3] = 4;
	testBuffer[4] = 5;

    Span span = {};
    span.Buffer = testBuffer;
    span.Length = 5;

	return span;
}

void DebugDrawTriangle(void* graphicsContext, Vector4 color1, Vector4 color2, Vector4 color3, Matrix4x4 worldMatrix)
{
    printf("DebugDrawTriangle Color1(%f, %f, %f, %f)\n", color1.X, color1.Y, color1.Z, color1.W);
    printf("DebugDrawTriangle Color2(%f, %f, %f, %f)\n", color2.X, color2.Y, color2.Z, color2.W);
    printf("DebugDrawTriangle Color3(%f, %f, %f, %f)\n", color3.X, color3.Y, color3.Z, color3.W);
}

InputsState GetInputsState(void* inputsContext)
{
    printf("GetInputsState\n");
    return inputsState;
}

void BuildTpaList(const char* directory, const char* extension, std::string& tpaList)
{
    // This will add all files with a .dll extension to the TPA list. 
    // This will include unmanaged assemblies (coreclr.dll, for example) that don't
    // belong on the TPA list. In a real host, only managed assemblies that the host
    // expects to load should be included. Having extra unmanaged assemblies doesn't
    // cause anything to fail, though, so this function just enumerates all dll's in
    // order to keep this sample concise.
    std::string searchPath(directory);
    searchPath.append("\\");
    searchPath.append("*");
    searchPath.append(extension);

    WIN32_FIND_DATAA findData;
    HANDLE fileHandle = FindFirstFileA(searchPath.c_str(), &findData);

    if (fileHandle != INVALID_HANDLE_VALUE)
    {
        do
        {
            // Append the assembly to the list
            tpaList.append(directory);
            tpaList.append("\\");
            tpaList.append(findData.cFileName);
            tpaList.append(";");

            // Note that the CLR does not guarantee which assembly will be loaded if an assembly
            // is in the TPA list multiple times (perhaps from different paths or perhaps with different NI/NI.dll
            // extensions. Therefore, a real host should probably add items to the list in priority order and only
            // add a file if it's not already present on the list.
            //
            // For this simple sample, though, and because we're only loading TPA assemblies from a single path,
            // and have no native images, we can ignore that complication.
        }
        while (FindNextFileA(fileHandle, &findData));
        FindClose(fileHandle);
    }
}

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
clientRectangle.right = static_cast<LONG>(1280 * scaling_factor);
clientRectangle.bottom = static_cast<LONG>(720 * scaling_factor);



        AdjustWindowRectExForDpi(&clientRectangle, 0, false, 0, dpi);

		// RECT clientRectangle = { 0, 0, width, height };
		// AdjustWindowRect(&clientRectangle, WS_OVERLAPPEDWINDOW, false);
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

        
        RECT testRect;
        GetClientRect(window, &testRect);
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

	LPCSTR appPath = "C:\\Projects\\perso\\CoreEngine\\build\\Windows";
	LPCSTR coreClrPath = "C:\\Projects\\perso\\CoreEngine\\build\\Windows\\CoreClr.dll";

	std::string tpaList;
	BuildTpaList(appPath, "dll", tpaList);

	HMODULE coreClr = LoadLibraryExA(coreClrPath, NULL, 0);

	coreclr_initialize_ptr initializeCoreClr = (coreclr_initialize_ptr)GetProcAddress(coreClr, "coreclr_initialize");
	coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)GetProcAddress(coreClr, "coreclr_create_delegate");
	coreclr_shutdown_ptr shutdownCoreClr = (coreclr_shutdown_ptr)GetProcAddress(coreClr, "coreclr_shutdown");

	// Define CoreCLR properties
	// Other properties related to assembly loading are common here,
	// but for this simple sample, TRUSTED_PLATFORM_ASSEMBLIES is all
	// that is needed. Check hosting documentation for other common properties.
	const char* propertyKeys[] = {
		"TRUSTED_PLATFORM_ASSEMBLIES"      // Trusted assemblies
	};

	const char* propertyValues[] = {
		tpaList.c_str()
	};

	void* hostHandle;
unsigned int domainId;

// This function both starts the .NET Core runtime and creates
// the default (and only) AppDomain
int hr = initializeCoreClr(
                "C:\\Projects\\perso\\CoreEngine\\build\\Windows",        // App base path
                "SampleHost",       // AppDomain friendly name
                sizeof(propertyKeys) / sizeof(char*),   // Property count
                propertyKeys,       // Property names
                propertyValues,     // Property values
                &hostHandle,        // Host handle
                &domainId);         // AppDomain ID

StartEnginePtr StartEngine;    
UpdateEnginePtr UpdateEngine;    

// The assembly name passed in the third parameter is a managed assembly name
// as described at https://docs.microsoft.com/dotnet/framework/app-domains/assembly-names
hr = createManagedDelegate(
        hostHandle, 
        domainId,
        "CoreEngine",
        "CoreEngine.Bootloader",
        "StartEngine",
        (void**)&StartEngine);

        hr = createManagedDelegate(
        hostHandle, 
        domainId,
        "CoreEngine",
        "CoreEngine.Bootloader",
        "UpdateEngine",
        (void**)&UpdateEngine);

    
 
    HostPlatform hostPlatform = {};
    hostPlatform.TestParameter = 5;

    char* appName = nullptr;

    if (argc > 1)
    {
        appName = (char*)malloc(strlen((char*)argv[1]));
        strcpy(appName, argv[1]);
    }

    hostPlatform.AddTestHostMethod = AddTestHostMethod;
    hostPlatform.GetTestBuffer = GetTestBuffer;

    hostPlatform.GraphicsService.DebugDrawTriangle = DebugDrawTriangle;

    hostPlatform.InputsService.GetInputsState = GetInputsState;

    StartEngine(appName, &hostPlatform);

    inputsState.Keyboard.KeyQ.Value = 1;
    UpdateEngine(5);

    shutdownCoreClr(hostHandle, domainId);

	printf("CoreEngine Windows Host has ended.\n");
	getchar();

    return 0;
}
#include "WindowsCommon.h"
#include <stdio.h>
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "CoreEngineHost.h"
#include "WindowsNativeUIServiceUtils.h"

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


int CALLBACK WinMain(HINSTANCE applicationInstance, HINSTANCE, LPSTR, int)
{
	AttachConsole(ATTACH_PARENT_PROCESS);

	GameState gameState = {};
	gameState.GameRunning = true;

    auto nativeUIService = WindowsNativeUIService(applicationInstance);
    auto graphicsService = Direct3D12GraphicsService();
    auto inputsService = WindowsInputsService();

    auto coreEngineHost = CoreEngineHost(nativeUIService, graphicsService, inputsService);
    coreEngineHost.StartEngine("EcsTest");
	
	while (gameState.GameRunning)
	{
		gameState.GameRunning = Win32ProcessPendingMessages();

		if (gameState.GameRunning)
		{
			if (isAppActive)
			{
				// if (doChangeSize && !firstRun)
				// {
				// 	RECT clientRect = {};
				// 	GetClientRect(window, &clientRect);

				// 	auto windowWidth = clientRect.right - clientRect.left;
				// 	auto windowHeight = clientRect.bottom - clientRect.top;

				// 	graphicsService.CreateOrResizeSwapChain(windowWidth, windowHeight);
				// 	doChangeSize = false;
				// }

				coreEngineHost.UpdateEngine(1.0f / 60.0f);
				firstRun = false;
			}
		}
	}
}
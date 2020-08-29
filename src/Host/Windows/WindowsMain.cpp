#include "WindowsCommon.h"
#include <stdio.h>
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "CoreEngineHost.h"
#include "WindowsNativeUIServiceUtils.h"

int CALLBACK WinMain(HINSTANCE applicationInstance, HINSTANCE, LPSTR commandLine, int)
{
	AttachConsole(ATTACH_PARENT_PROCESS);

    auto assemblyName = string(commandLine);

    if (assemblyName.empty())
    {
        assemblyName = "CoreEngine";
    }

    else
    {
        assemblyName = "CoreEngine-" + assemblyName;
    }

    auto nativeUIService = WindowsNativeUIService(applicationInstance);
    auto graphicsService = Direct3D12GraphicsService();
    auto inputsService = WindowsInputsService();

    auto coreEngineHost = CoreEngineHost(assemblyName, nativeUIService, graphicsService, inputsService);
    coreEngineHost.StartEngine("EcsTest");
}
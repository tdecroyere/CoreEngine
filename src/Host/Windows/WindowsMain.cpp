#include "WindowsCommon.h"
#include <stdio.h>
#include "Direct3D12GraphicsService.h"
#include "WindowsInputsService.h"
#include "CoreEngineHost.h"
#include "WindowsNativeUIServiceUtils.h"

int CALLBACK wWinMain(HINSTANCE applicationInstance, HINSTANCE, LPWSTR commandLine, int)
{
	AttachConsole(ATTACH_PARENT_PROCESS);

    auto assemblyName = wstring(commandLine);
    assemblyName = assemblyName.erase(assemblyName.find_last_not_of(' ')+1);

    if (assemblyName.empty())
    {
        assemblyName = L"CoreEngine";
    }

    else if (assemblyName == L"compile" || assemblyName == L"editor")
    {
        assemblyName = L"CoreEngine-" + assemblyName;
    }

    auto nativeUIService = WindowsNativeUIService(applicationInstance);
    auto graphicsService = Direct3D12GraphicsService();
    auto inputsService = WindowsInputsService();

    auto coreEngineHost = CoreEngineHost(assemblyName, nativeUIService, graphicsService, inputsService);
    coreEngineHost.StartEngine("EcsTest");
}
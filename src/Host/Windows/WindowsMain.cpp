#include "WindowsCommon.h"
#include <stdio.h>
#include "Direct3D12GraphicsService.h"
#include "VulkanGraphicsService.h"
#include "WindowsInputsService.h"
#include "CoreEngineHost.h"
#include "WindowsNativeUIServiceUtils.h"

#pragma warning(disable:4244)

vector<wstring> SplitString(const wstring& value, wchar_t separator)
{
    vector<wstring> output;

    wstring::size_type prev_pos = 0, pos = 0;

    while((pos = value.find(separator, pos)) != wstring::npos)
    {
        wstring substring(value.substr(prev_pos, pos-prev_pos));
        output.push_back(substring);

        prev_pos = ++pos;
    }

    output.push_back(value.substr(prev_pos, pos-prev_pos)); // Last word

    return output;
}

wstring TrimString(const wstring& value, wchar_t* characters)
{
    wstring result = value;

    result = result.erase(0, result.find_first_not_of(characters));
    result = result.erase(result.find_last_not_of(characters) + 1);

    return result;
}

string ConvertString(const wstring& value)
{
    return string(value.begin(), value.end());
}

bool FileExists(const wstring& filename) 
{
    struct stat buffer;   
    auto test = ConvertString(filename);
    return (stat(test.c_str(), &buffer) == 0);
}

int CALLBACK wWinMain(HINSTANCE applicationInstance, HINSTANCE, LPWSTR fullCommandLine, int)
{
	AttachConsole(ATTACH_PARENT_PROCESS);

    auto commandLine = TrimString(wstring(fullCommandLine), L" \"");
    auto arguments = SplitString(commandLine, ' ');

    wstring assemblyName = L"CoreEngine";
    bool useVulkan = false;

    if (!arguments.empty())
    {
        auto firstArgument = arguments[0];

        if (FileExists(firstArgument + L".dll")) 
        {
            assemblyName = firstArgument;
        }

        else
        {
            auto fileParts = SplitString(commandLine, '\\');
            auto directoryName = fileParts[fileParts.size() - 1];

            // TODO: Detect architecture

            // if (FileExists(firstArgument + L"\\bin\\win-x64\\" + directoryName + L".dll")) 
            // {
            //     assemblyName = firstArgument + L"\\bin\\win-x64\\" + directoryName;
            // }
        }

        for (int i = 0; i < arguments.size(); i++)
        {
            wstring parameter = arguments[i];

            if (parameter == L"--vulkan")
            {
                useVulkan = true;
            }
        }
    }

    auto nativeUIService = WindowsNativeUIService(applicationInstance);

    Direct3D12GraphicsService* direct3dGraphicsService = nullptr;
    VulkanGraphicsService* vulkanGraphicsService = nullptr;

    if (!useVulkan)
    {
        direct3dGraphicsService = new Direct3D12GraphicsService();

    }

    else
    {
        vulkanGraphicsService = new VulkanGraphicsService();
    }

    auto inputsService = WindowsInputsService();

    auto coreEngineHost = CoreEngineHost(assemblyName, &nativeUIService, direct3dGraphicsService, vulkanGraphicsService, &inputsService);
    coreEngineHost.StartEngine();
}
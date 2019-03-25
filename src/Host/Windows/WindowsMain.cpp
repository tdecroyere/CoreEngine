#include <stdio.h>
#include <windows.h>
#include <string>
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
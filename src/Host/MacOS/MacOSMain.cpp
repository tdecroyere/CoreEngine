#include <stdio.h>
#include <string>
#include <unistd.h>
#include <dirent.h>
#include <dlfcn.h>
#include "libcoreclr.h"

struct Span
{
	unsigned char* Buffer;
	int Length;

	Span(unsigned char* buffer, int length)
	{
		this->Buffer = buffer;
		this->Length = length;
	}
};

struct HostPlatform
{
	int TestParameter;
    char* AppName;
	void* AddTestHostMethod;
	void* GetTestBuffer;
};

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

	return Span(testBuffer, 5);
}

typedef int AddTestHostMethodType(int a, int b);
typedef Span GetTestBufferType();
typedef void StartEngine(HostPlatform* hostPlatform);

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

    DIR* dir = opendir(directory);

    const char* ext = ".dll";
    int extLength = strlen(ext);

    struct dirent* entry;

    // For all entries in the directory
    while ((entry = readdir(dir)) != nullptr)
    {
        std::string filename(entry->d_name);

        // Check if the extension matches the one we are looking for
        int extPos = filename.length() - extLength;
        if ((extPos <= 0) || (filename.compare(extPos, extLength, ext) != 0))
        {
            continue;
        }

        tpaList.append(directory);
        tpaList.append("/");
        tpaList.append(entry->d_name);
        tpaList.append(":");
    }
}

int main(int argc, char const *argv[])
{
    printf("CoreEngine MacOS Host\n");
	std::string appPath = "/Users/tdecroyere/Projects/CoreEngine/build/MacOS/CoreEngine.app/Contents/CoreClr";

	std::string tpaList;
	BuildTpaList(appPath.c_str(), "dll", tpaList);

    void *coreClr = dlopen("./libcoreclr.dylib", RTLD_NOW | RTLD_LOCAL);

 	coreclr_initialize_ptr initializeCoreClr = (coreclr_initialize_ptr)dlsym(coreClr, "coreclr_initialize");
 	coreclr_create_delegate_ptr createManagedDelegate = (coreclr_create_delegate_ptr)dlsym(coreClr, "coreclr_create_delegate");
 	coreclr_shutdown_ptr shutdownCoreClr = (coreclr_shutdown_ptr)dlsym(coreClr, "coreclr_shutdown");

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
                 appPath.c_str(),        // App base path
                 "SampleHost",       // AppDomain friendly name
                 1,   // Property count
                 propertyKeys,       // Property names
                 propertyValues,     // Property values
                 &hostHandle,        // Host handle
                 &domainId);         // AppDomain ID

    StartEngine* managedDelegate;    

    // The assembly name passed in the third parameter is a managed assembly name
    // as described at https://docs.microsoft.com/dotnet/framework/app-domains/assembly-names
    hr = createManagedDelegate(
            hostHandle, 
            domainId,
            "CoreEngine",
            "CoreEngine.Bootloader",
            "StartEngine",
            (void**)&managedDelegate);

    AddTestHostMethodType* testMethod = AddTestHostMethod;
    GetTestBufferType* getTestBufferMethod = GetTestBuffer;

    HostPlatform hostPlatform = {};
    hostPlatform.TestParameter = 5;
    hostPlatform.AppName = (char*)argv[1];
    hostPlatform.AddTestHostMethod = (void*)testMethod;
    hostPlatform.GetTestBuffer = (void*)getTestBufferMethod;

    managedDelegate(&hostPlatform);
	
	getchar();

    shutdownCoreClr(hostHandle, domainId);
}
#pragma once

#include "WindowsCommon.h"
#include "WindowsCoreEngineHost.h"

using namespace std;

int main(int argc, char const *argv[])
{
    printf("CoreEngine Windows Host\n");

    string appName = string();

    if (argc > 1)
    {
        appName = string(argv[1]);
    }

    WindowsCoreEngineHost* coreEngineHost = new WindowsCoreEngineHost();
    coreEngineHost->StartEngine(L"EcsTest");

    coreEngineHost->UpdateEngine(5);

	printf("CoreEngine Windows Host has ended.\n");
	getchar();

    return 0;
}
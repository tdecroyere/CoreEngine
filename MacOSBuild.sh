#!/usr/bin/env bash

outputDirectory="./build/MacOS"
mkdir -p $outputDirectory > /dev/null

echo [93mCompiling Core Library...[0m

clang++ -DCOREENGINE_SLOW -Wall -O0 -g -dynamiclib -std=c++14 -install_name CoreEngine.Core.dylib ./src/CoreEngine.cpp -o $outputDirectory/CoreEngine.Core.dylib

if [ $? -eq 0 ]; then
    echo [93mCompiling MacOS Executable...[0m
    clang++ -Wall -O0 -g -std=c++14 ./src/MacOS/MacOSPlatform.cpp $outputDirectory/CoreEngine.Core.dylib -o $outputDirectory/CoreEngine
    echo [92mSuccess: Compilation done.[0m
fi
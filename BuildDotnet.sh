#!/usr/bin/env bash

outputDirectory="MacOS/CoreEngine.app/Contents"

mkdir -p "./build/"$outputDirectory > /dev/null
mkdir -p "./build/"$outputDirectory"/CoreClr" > /dev/null

cd "./build/"$outputDirectory"/CoreClr"

compileDotnet() {
    echo [93mCompiling $1...[0m
    dotnet build --nologo -c Debug -v Q -o "." "../../../../../$1"

    if [ $? -eq 0 ]; then
        echo [92mSuccess: Compilation done.[0m
    fi
}

compileDotnet "./src/Tools/Compiler"
compileDotnet "./src/Tools/Editor"
compileDotnet "./tests/EcsTest"
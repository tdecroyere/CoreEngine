#!/usr/bin/env bash

outputDirectory="MacOS/CoreEngine.app/Contents"

mkdir -p "./build/"$outputDirectory > /dev/null
mkdir -p "./build/"$outputDirectory"/CoreClr" > /dev/null

cd "./build/"$outputDirectory"/CoreClr"

echo [93mCompiling $1 Test Library...[0m

dotnet build /nologo -c Debug -v Q -o "." "../../../../../tests/"$1

if [ $? -eq 0 ]; then
    echo [92mSuccess: Compilation done.[0m
fi
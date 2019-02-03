#!/usr/bin/env bash

tempDirectory="./build/temp"
outputDirectory="MacOS/CoreEngine.app/Contents"

mkdir -p $tempDirectory > /dev/null
mkdir -p "./build/"$outputDirectory > /dev/null
mkdir -p "./build/"$outputDirectory"/MacOS" > /dev/null
mkdir -p "./build/"$outputDirectory"/Frameworks" > /dev/null

cd "./build/temp"

copyFiles() {
    echo [93mCopy files...[0m
    cp "../../src/Host/MacOS/Info.plist" "../"$outputDirectory
    cp *".pdb" "../"$outputDirectory"/Frameworks"
    cp "libcoreclr.dylib" "../"$outputDirectory"/Frameworks"
    cp "libclrjit.dylib" "../"$outputDirectory"/Frameworks"
    cp "System.Private.CoreLib.dll" "../"$outputDirectory"/Frameworks"
    cp "System.Runtime.dll" "../"$outputDirectory"/Frameworks"
    cp "System.Console.dll" "../"$outputDirectory"/Frameworks"
    cp "System.Threading.dll" "../"$outputDirectory"/Frameworks"
    cp "System.Runtime.Extensions.dll" "../"$outputDirectory"/Frameworks"
    cp "System.Text.Encoding.Extensions.dll" "../"$outputDirectory"/Frameworks"
    cp "CoreEngine.dll" "../"$outputDirectory"/Frameworks"
    cp "CoreEngine" "../"$outputDirectory"/MacOS"
    cp -R "CoreEngine.dSYM" "../"$outputDirectory"/MacOS"

    cd "../.."
    #rm -R $tempDirectory
}

echo [93mCompiling CoreEngine Library...[0m

dotnet publish /nologo -r osx-x64 -c Debug -v q --self-contained true -o "." "../../src/CoreEngine"

if [ $? -eq 0 ]; then
    echo [93mCompiling MacOS Executable...[0m
    swiftc "../../src/Host/MacOS/"*".swift" -Onone -g -o "./CoreEngine"
    copyFiles
    echo [92mSuccess: Compilation done.[0m
fi
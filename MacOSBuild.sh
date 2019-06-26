#!/usr/bin/env bash

tempDirectory="./build/temp"
outputDirectory="MacOS/CoreEngine.app/Contents"

mkdir -p $tempDirectory > /dev/null
mkdir -p "./build/"$outputDirectory > /dev/null
mkdir -p "./build/"$outputDirectory"/MacOS" > /dev/null
mkdir -p "./build/"$outputDirectory"/Frameworks" > /dev/null
mkdir -p "./build/"$outputDirectory"/CoreClr" > /dev/null

cd "./build/temp"

copyFiles() {
    echo [93mCopy files...[0m
    cp "../../src/Host/MacOS/Info.plist" "../"$outputDirectory
    cp * "../"$outputDirectory"/CoreClr"
    #cp *".dll" "../"$outputDirectory"/Frameworks"
    #cp *".dylib" "../"$outputDirectory"/Frameworks"
    #cp *".a" "../"$outputDirectory"/Frameworks"
    #cp "mscorlib.dll" "../"$outputDirectory"/Frameworks"
    # cp "System.Private.CoreLib.dll" "../"$outputDirectory"/Frameworks"
    # cp "System.Runtime.dll" "../"$outputDirectory"/Frameworks"
    # cp "System.Console.dll" "../"$outputDirectory"/Frameworks"
    # cp "System.Threading.dll" "../"$outputDirectory"/Frameworks"
    # cp "System.Runtime.Extensions.dll" "../"$outputDirectory"/Frameworks"
    # cp "System.Text.Encoding.Extensions.dll" "../"$outputDirectory"/Frameworks"
    # cp "CoreEngine.dll" "../"$outputDirectory"/Frameworks"
    # cp *".dll" "../"$outputDirectory"/CoreClr"
    # "/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/macosx"

    #rm -R $tempDirectory
}

showErrorMessage() {
    echo [91mError: Build has failed![0m
}

compileDotnet() {
    echo [93mCompiling CoreEngine Library...[0m

    dotnet publish /property:GenerateFullPaths=true --nologo -r osx-x64 -c Debug -v q --self-contained true -o "." "../../src/CoreEngine"

    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi
}

compileSwift() {
    echo [93mCompiling MacOS Executable...[0m
    cd "../"$outputDirectory"/MacOS/"
    swiftc "../../../../../src/Host/MacOS/"*".swift" -Onone -g -o "CoreEngine" -debug-info-format=dwarf -swift-version 5 -target x86_64-apple-macosx10.14 -I "../../../../../src/Host/MacOS" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi
}

compileDotnet
copyFiles
compileSwift

echo [92mSuccess: Compilation done.[0m
exit 0
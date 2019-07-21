#!/usr/bin/env bash

dotnetTempDirectory="./build/temp/dotnet"
macosTempDirectory="./build/temp/macos"
outputDirectory="MacOS/CoreEngine.app/Contents"

mkdir -p $dotnetTempDirectory > /dev/null
mkdir -p $macosTempDirectory > /dev/null
mkdir -p "./build/"$outputDirectory > /dev/null
mkdir -p "./build/"$outputDirectory"/MacOS" > /dev/null
mkdir -p "./build/"$outputDirectory"/Frameworks" > /dev/null
mkdir -p "./build/"$outputDirectory"/CoreClr" > /dev/null

copyFiles() {
    echo [93mCopy files...[0m

    cp "./src/Host/Apple/MacOS/Info.plist" "./build/"$outputDirectory
    cp $dotnetTempDirectory"/"* "./build/"$outputDirectory"/CoreClr"
    cp $macosTempDirectory"/CoreEngine" "./build/"$outputDirectory"/MacOS"
    cp -R $macosTempDirectory"/CoreEngine.dSYM" "./build/"$outputDirectory"/MacOS"

    #rm -R $tempDirectory
}

showErrorMessage() {
    echo [91mError: Build has failed![0m
}

compileDotnet() {
    cd $dotnetTempDirectory
    echo [93mCompiling CoreEngine Library...[0m

    dotnet publish /property:GenerateFullPaths=true --nologo -r osx-x64 -c Debug -v q --self-contained true -o "." "../../../src/CoreEngine"

    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileHostModule() {
    cd $macosTempDirectory
    echo [93mCompiling MacOS Module...[0m
    swiftc "../../../src/Host/Apple/Common/"*".swift" -Onone -emit-library -static -emit-module -module-name CoreEngineCommon -swift-version 5 -target x86_64-apple-macosx10.15 -I "../../../src/Host/Apple/Interop" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileHost() {
    cd $macosTempDirectory
    echo [93mCompiling MacOS Executable...[0m
    swiftc "../../../src/Host/Apple/MacOS/"*".swift" -Onone -g -o "CoreEngine" -debug-info-format=dwarf -swift-version 5 -target x86_64-apple-macosx10.15 -lCoreEngineCommon -L "." -I "." -I "../../../src/Host/Apple/Interop" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileDotnet
compileHostModule
compileHost
copyFiles

echo [92mSuccess: Compilation done.[0m
exit 0
#!/usr/bin/env bash

dotnetTempDirectory="./build/temp/dotnet"
iosTempDirectory="./build/temp/ios"
outputDirectory="iOS/CoreEngine.app"

mkdir -p $dotnetTempDirectory > /dev/null
mkdir -p $iosTempDirectory > /dev/null
mkdir -p "./build/"$outputDirectory > /dev/null
mkdir -p "./build/"$outputDirectory"/CoreClr" > /dev/null

copyFiles() {
    echo [93mCopy files...[0m

    cp "./src/Host/Apple/iOS/Info.plist" "./build/"$outputDirectory
    cp $dotnetTempDirectory"/"* "./build/"$outputDirectory"/CoreClr"
    cp $iosTempDirectory"/CoreEngine" "./build/"$outputDirectory
    cp -R $iosTempDirectory"/CoreEngine.dSYM" "./build/"$outputDirectory

    #rm -R $tempDirectory
}

showErrorMessage() {
    echo [91mError: Build has failed![0m
}

compileDotnet() {
    cd $dotnetTempDirectory
    echo [93mCompiling CoreEngine Library...[0m

    dotnet publish /property:GenerateFullPaths=true --nologo -r linux-arm64 -c Debug -v q --self-contained true -o "." "../../../src/CoreEngine"

    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileHostModule() {
    cd $iosTempDirectory
    echo [93mCompiling Apple Common Module for iOS...[0m

    sdkPath=$(xcrun --sdk iphoneos --show-sdk-path)
    swiftc "../../../src/Host/Apple/Common/"*".swift" -Onone -emit-library -static -emit-module -module-name CoreEngineCommon -swift-version 5 -target arm64-apple-ios12.0-simulator -sdk $sdkPath -framework IOKit -I "../../../src/Host/Apple/Interop" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileHost() {
    cd $iosTempDirectory
    echo [93mCompiling MacOS Executable...[0m
    swiftc "../../../src/Host/Apple/iOS/"*".swift" -Onone -g -o "CoreEngine" -debug-info-format=dwarf -swift-version 5 -target x86_64-apple-ios13.0-simulator -lCoreEngineCommon -L "." -I "." -I "../../../src/Host/Apple/Interop" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
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
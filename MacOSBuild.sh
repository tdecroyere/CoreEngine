#!/usr/bin/env zsh
setopt extended_glob

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
    echo "[93mCopy files...[0m"

    cp "./src/Host/Apple/MacOS/Info.plist" "./build/"$outputDirectory
    cp $dotnetTempDirectory"/"* "./build/"$outputDirectory"/CoreClr"
    cp $macosTempDirectory"/CoreEngine" "./build/"$outputDirectory"/MacOS"
    cp -R $macosTempDirectory"/CoreEngine.dSYM" "./build/"$outputDirectory"/MacOS"

    #rm -R $tempDirectory
}

showErrorMessage() {
    echo "[91mError: Build has failed![0m"
}

generateInteropCode() {
    cd "../CoreEngine-Tools/tools/CoreEngineInteropGenerator"
    echo "[93mGenerating CoreEngine Interop Code...[0m"

    dotnet run

    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../../CoreEngine"
}

compileDotnet() {
    cd $dotnetTempDirectory
    echo "[93mCompiling CoreEngine Library...[0m"

    dotnet publish /property:GenerateFullPaths=true --nologo -r osx-x64 -c Debug -v q --self-contained true -o "." "../../../src/CoreEngine"

    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileHostModule() {
    cd $macosTempDirectory
    echo "[93mCompiling Apple CoreEngine Common Module for MacOS...[0m"
    swiftc "../../../src/Host/Apple/CoreEngineCommon"/**/*".swift" -Onone -emit-library -emit-module -static -module-name CoreEngineCommon -swift-version 5 -target x86_64-apple-macosx10.15 -I "../../../src/Host/Apple/CoreEngineCommon" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

compileHost() {
    cd $macosTempDirectory
    echo "[93mCompiling MacOS Executable...[0m"
    swiftc "../../../src/Host/Apple/MacOS/"*".swift" -Onone -g -o "CoreEngine" -debug-info-format=dwarf -swift-version 5 -target x86_64-apple-macosx10.15 -lCoreEngineCommon -L "." -I "." -I "../../../src/Host/Apple/CoreEngineCommon" -Xlinker -rpath -Xlinker "@executable_path/../Frameworks"
    
    if [ $? != 0 ]; then
        showErrorMessage
        exit 1
    fi

    cd "../../.."
}

signCode() {
    cp "./src/Host/Apple/MacOS/CoreEngine.entitlements" $macosTempDirectory

    echo "[93mSigning Code...[0m"

    cd $macosTempDirectory
    codesign --entitlements ./CoreEngine.entitlements -s "Mac Developer: Thomas Decroyere (M9L7VG8ZR5)" ./CoreEngine
    cd "../../.."
}

generateInteropCode
compileDotnet
compileHostModule
compileHost
#signCode
copyFiles

echo "[92mSuccess: Compilation done.[0m"
exit 0
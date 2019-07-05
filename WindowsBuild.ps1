$OriginalProgressPreference = $ProgressPreference
$ProgressPreference = "SilentlyContinue"

$WindowsHostSourceFolder = ".\src\Host\Windows\"
$GeneratedFilesFolder = ".\src\Host\Windows\Generated Files\"
$ObjFolder = ".\src\Host\Windows\Generated Files\obj"
$TempFolder = ".\build\temp"
$OutputFolder = ".\build\Windows"

if (-not(Test-Path -Path $TempFolder))
{
    New-Item -Path $TempFolder -ItemType "directory" | Out-Null
}

if (-not(Test-Path -Path $ObjFolder))
{
    New-Item -Path $ObjFolder -ItemType "directory" | Out-Null
}

if (-not(Test-Path -Path $OutputFolder))
{
    New-Item -Path $OutputFolder -ItemType "directory" | Out-Null
}

function RegisterVisualStudioEnvironment
{
    $registeredVisualStudioVersion = Get-Content -Path Env:VisualStudioVersion -ErrorAction SilentlyContinue

    if (-not($registeredVisualStudioVersion -eq "16.0"))
    {
        Write-Output "[93mRegistering Visual Studio Environment...[0m"

        # TODO: Do something better here
        $vsPath = ""
        $vs2019ProfPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat"
        $vs2019EntPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvars64.bat"

        if (Test-Path -Path $vs2019ProfPath)
        {
            $vsPath = $vs2019ProfPath
        }

        if (Test-Path -Path $vs2019EntPath)
        {
            $vsPath = $vs2019EntPath
        }

        $batchCommand = "`"$vsPath`" > nul & set"

        cmd /c $batchCommand | Foreach-Object {
            $p, $v = $_.split('=')
            Set-Item -path env:$p -value $v
        }
    }
}

function GenerateIncludeFiles
{
    $nuGetExe = ".\nuget.exe"
    $packagesFile = "..\packages.config"
    $packagesDirectory = ".\packages"
    $includeDirectory = ".\inc"

    if (-not(Test-Path $GeneratedFilesFolder)) 
    {
        New-Item -Path $GeneratedFilesFolder -ItemType "directory" | Out-Null
    }

    Push-Location $GeneratedFilesFolder

    if (-not(Test-Path($nuGetExe))) 
    {
        Write-Output "Downloading nuget.exe..."
        Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile $nuGetExe
    }

    if (-not(Test-Path($packagesDirectory))) 
    {
        & $nuGetExe "restore" $packagesFile "-PackagesDirectory" $packagesDirectory

        if (-not $?) 
        {
            Write-Output "[91mError: Nuget restore has failed![0m"
        }
    }

    if (-not(Test-Path($includeDirectory))) 
    {
        Write-Output "[93mGenerating C++/WinRT 2.0 include files...[0m"
        $winrtProgram = (Get-ChildItem -Path $packagesDirectory -Filter "Microsoft.Windows.CppWinRT*" -Recurse -Directory).Fullname + "\bin\cppwinrt.exe"
        & $winrtProgram "-input" "local" "-output" $includeDirectory

        if (-not $?) 
        {
            Write-Output "[91mError: Winrt has failed![0m"
        }
    }

    Pop-Location
}

function ShowErrorMessage
{
    Write-Output "[91mError: Build has failed![0m"
}

function CompileDotnet
{
    Push-Location $TempFolder
    Write-Output "[93mCompiling CoreEngine Library...[0m"
    dotnet publish --nologo -r win-x64 -c Debug -v Q --self-contained true -o "." "..\..\src\CoreEngine"

    if(-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

function PreCompileHeader
{
    Push-Location $ObjFolder

    if (-Not(Test-Path -Path "WindowsCommon.pch"))
    {
        Write-Output "[93mCompiling Windows Pre-compiled header...[0m"
        cl.exe /c /nologo /DDEBUG /std:c++17 /EHsc /I"..\inc" /Zi /Yc /FpWindowsCommon.pch /DWINRT_NO_MAKE_DETECTION "..\..\WindowsCommon.cpp"

        if(-Not $?)
        {
            Pop-Location
            ShowErrorMessage
            Exit 1
        }
    }

    Pop-Location
}

function CompileWindowsHost
{
    Push-Location $ObjFolder

    Write-Output "[93mCompiling Windows Executable...[0m"

    cl.exe /c /nologo /DDEBUG /std:c++17 /diagnostics:caret /EHsc /I"..\inc" /Zi /Yu"WindowsCommon.h" /DWINRT_NO_MAKE_DETECTION /FpWindowsCommon.PCH /TP /Tp"..\..\main.compilationunit"

    if (-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

function CompileWindowsHostConsole
{
    Push-Location $ObjFolder

    Write-Output "[93mCompiling Windows Console Executable...[0m"

    cl.exe /c /nologo /DDEBUG /std:c++17 /diagnostics:caret /EHsc /I"..\inc" /Zi /Yu"WindowsCommon.h" /DWINRT_NO_MAKE_DETECTION /FpWindowsCommon.PCH /TP /Tp"..\..\ConsoleMain.cpp" /Tp"..\..\WindowsCoreEngineHost.cpp"
    link.exe "ConsoleMain.obj" "WindowsCommon.obj" "WindowsCoreEngineHost.obj" /OUT:"..\..\..\..\..\build\temp\CoreEngineConsole.exe" /PDB:"..\..\..\..\..\build\temp\CoreEngineConsole.pdb" /DEBUG /MAP /OPT:ref /INCREMENTAL:NO /WINMD:NO /NOLOGO WindowsApp.lib D3D12.lib

    if (-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

function LinkWindowsHost
{
    Push-Location $ObjFolder
    Write-Output "[93mLinking Windows Executable...[0m"
   
    link.exe "main.obj" "WindowsCommon.obj" /OUT:"..\..\..\..\..\build\temp\CoreEngine.exe" /PDB:"..\..\..\..\..\build\temp\CoreEngineHost.pdb" /APPCONTAINER /DEBUG /MAP /OPT:ref /INCREMENTAL:NO /WINMD:NO /NOLOGO WindowsApp.lib D3D12.lib
    Copy-Item "..\..\AppxManifest.xml" "..\..\..\..\..\build\temp\"

    if (-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

function CopyFiles
{
    Write-Output "[93mCopy files...[0m"
    Push-Location $TempFolder

    Copy-Item "*.dll" "..\Windows"
    Copy-Item "*.pdb" "..\Windows"
    Copy-Item "AppxManifest.xml" "..\Windows"
    Copy-Item "CoreEngine.exe" "..\Windows"

    Pop-Location
}

function RegisterApp
{
    Write-Output "[93mRegistering CoreEngine Windows App...[0m"
    Push-Location $OutputFolder
    Add-appxpackage -register AppxManifest.xml
    Pop-Location
}

RegisterVisualStudioEnvironment
GenerateIncludeFiles
CompileDotnet
PreCompileHeader
CompileWindowsHost
LinkWindowsHost
CopyFiles
RegisterApp

Write-Output "[92mSuccess: Compilation done.[0m"

$ProgressPreference = $OriginalProgressPreference
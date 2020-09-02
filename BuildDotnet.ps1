$OriginalProgressPreference = $ProgressPreference
$ProgressPreference = "SilentlyContinue"

$OutputFolder = ".\build\Windows"

if (-not(Test-Path -Path $OutputFolder))
{
    New-Item -Path $OutputFolder -ItemType "directory" | Out-Null
}

function ShowErrorMessage
{
    Write-Output "[91mError: Build has failed![0m"
}

function CompileDotnet($projectPath)
{
    Push-Location $OutputFolder
    Write-Output "[93mCompiling $projectPath...[0m"
    dotnet build --nologo -c Debug -v Q -o "." "..\..\$projectPath"

    if(-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

CompileDotnet(".\src\Tools\Compiler")
CompileDotnet(".\src\Tools\Editor")
CompileDotnet(".\tests\EcsTest")

Write-Output "[92mSuccess: Compilation done.[0m"

$ProgressPreference = $OriginalProgressPreference
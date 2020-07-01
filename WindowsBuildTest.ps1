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

function CompileDotnet
{
    Push-Location $OutputFolder
    Write-Output "[93mCompiling CoreEngine Tests...[0m"
    dotnet build --nologo -c Debug -v Q -o "." "..\..\tests\EcsTest"

    if(-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

CompileDotnet

Write-Output "[92mSuccess: Compilation done.[0m"

$ProgressPreference = $OriginalProgressPreference
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
    dotnet build --nologo -c Debug -v Q -o "." "$projectPath"

    if(-Not $?)
    {
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

function CompileAllDotnetProjects()
{
    ForEach ($projectDirectory in (get-childitem .\*.csproj -Recurse | Select-Object Directory))
    {
        CompileDotnet($projectDirectory.Directory.FullName)
    }
}

function CompileDotnetProject($projectName)
{
    ForEach ($projectDirectory in (get-childitem .\$projectName.csproj -Recurse | Select-Object Directory))
    {
        CompileDotnet($projectDirectory.Directory.FullName)
    }
}

if ($args.length -gt 0)
{
    CompileDotnetProject($args[0])
}

else
{
    CompileAllDotnetProjects
}

Write-Output "[92mSuccess: Compilation done.[0m"

$ProgressPreference = $OriginalProgressPreference
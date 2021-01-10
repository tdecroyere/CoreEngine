$OriginalProgressPreference = $ProgressPreference
$ProgressPreference = "SilentlyContinue"

$SourceFolder = ".\src"
$OutputFolder = "..\build\Windows"

if ($args.length -gt 0 -And $args[0] -eq "Compiler" -Or $args[0] -eq "Editor")
{
    $OutputFolder = "..\build\Windows\Tools"
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

    #dotnet publish /p:NativeLib=Shared -r win-x64 -c release

    if(-Not $?)
    {
        Pop-Location
        Pop-Location
        ShowErrorMessage
        Exit 1
    }

    Pop-Location
}

function CompileAllDotnetProjects()
{
    Push-Location $SourceFolder

    if (-not(Test-Path -Path $OutputFolder))
    {
        New-Item -Path $OutputFolder -ItemType "directory" | Out-Null
    }

    ForEach ($projectDirectory in (get-childitem .\*.csproj -Recurse | Select-Object Directory))
    {
        if ($projectDirectory.Directory.Parent.Name -eq "Tools")
        {
            $OldOutputFolder = $OutputFolder
            $OutputFolder = "..\build\Windows\Tools"

            if (-not(Test-Path -Path $OutputFolder))
            {
                New-Item -Path $OutputFolder -ItemType "directory" | Out-Null
            }
        }

        CompileDotnet($projectDirectory.Directory.FullName)

        if ($projectDirectory.Directory.Parent.Name -eq "Tools")
        {
            $OutputFolder = $OldOutputFolder
        }
    }

    Pop-Location
}

function CompileDotnetProject($projectName)
{
    Push-Location $SourceFolder

    if (-not(Test-Path -Path $OutputFolder))
    {
        New-Item -Path $OutputFolder -ItemType "directory" | Out-Null
    }

    ForEach ($projectDirectory in (get-childitem .\$projectName.csproj -Recurse | Select-Object Directory))
    {
        CompileDotnet($projectDirectory.Directory.FullName)

        if ($projectDirectory.Directory.Name -eq "CoreEngine")
        {
            $OutputFolder = "..\build\Windows\Tools"
            CompileDotnet($projectDirectory.Directory.FullName)
        }
    }

    Pop-Location
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
try
{
    if ($args.length -gt 0)
    {
        Push-Location .\tests\$($args[0])
        dotnet watch test --nologo -v m /p:CollectCoverage=true /p:CoverletOutput=../../coverage/lcov-$($args[0]).info /p:CoverletOutputFormat=lcov
    }

    else
    {
        Push-Location .\tests\CoreEngine.UnitTests
        dotnet watch test --nologo -v m /p:CollectCoverage=true /p:CoverletOutput=../../coverage/lcov-CoreEngine.UnitTests.info /p:CoverletOutputFormat=lcov
    }
}

finally
{
    Pop-Location
}

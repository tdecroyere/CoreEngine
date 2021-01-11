try
{
    Push-Location .\tests\CoreEngine.UnitTests

    dotnet watch test --nologo -v m /p:CollectCoverage=true /p:CoverletOutput=../../coverage/lcov-CoreEngine.info /p:CoverletOutputFormat=lcov
}

finally
{
    Pop-Location
}

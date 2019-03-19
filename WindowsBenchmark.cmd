@ECHO OFF
pushd .\tests\%1
dotnet build -c Release
dotnet benchmark .\bin\Release\netcoreapp3.0\%1.dll -r netcoreapp3.0 -f * -j Short -e GitHub
popd
@ECHO ON
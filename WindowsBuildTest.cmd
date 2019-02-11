@ECHO OFF

IF NOT EXIST build\Windows mkdir build\Windows
pushd build\Windows

ECHO [93mCompiling %1 Test Library...[0m
dotnet.exe build /nologo -c Debug -v Q -o "." "..\..\tests\\"%1

@IF %ERRORLEVEL% == 0 (
   GOTO End
)
@IF NOT %ERRORLEVEL% == 0 (
   GOTO CompileError
)

:CompileError
   ECHO [91mError: Build has failed![0m
   EXIT 1

:End
    ECHO [92mSuccess: Compilation done.[0m
    popd
    @ECHO ON
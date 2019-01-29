@ECHO OFF

IF NOT EXIST build\Win32 mkdir build\Win32
pushd build\Win32

DEL *.pdb > NUL 2> NUL

ECHO [93mCompiling CoreEngine Library...[0m
dotnet.exe publish -r win-x64 --self-contained true -o "." "..\..\src\CoreEngine"

@IF %ERRORLEVEL% == 0 (
   GOTO Compile_Win32_Executable
)
@IF NOT %ERRORLEVEL% == 0 (
   GOTO CompileError
)

:Compile_Win32_Executable
    ECHO [93mCompiling Win32 Executable...[0m
    cl.exe %COMPILER_OPTIONS% "..\..\src\Windows\WindowsPlatform.cpp"

    @IF NOT %ERRORLEVEL% == 0 (
        GOTO CompileError
    )
    
:CompileError
   ECHO Error: Build has failed!
   EXIT 1
   
:End
    popd
    @ECHO ON
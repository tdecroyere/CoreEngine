@ECHO OFF

IF EXIST "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\amd64\" Call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\bin\amd64\vcvars64.bat" > NUL
IF EXIST "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\VC\Auxiliary\Build\" Call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\VC\Auxiliary\Build\vcvars64.bat" > NUL

IF NOT EXIST build\temp mkdir build\temp
IF NOT EXIST build\Windows mkdir build\Windows
pushd build\temp

DEL *.pdb > NUL 2> NUL

ECHO [93mCompiling CoreEngine Library...[0m
dotnet.exe publish /nologo -r win-x64 -c Debug -v q --self-contained true -o "." "..\..\src\CoreEngine"

@IF %ERRORLEVEL% == 0 (
   GOTO Compile_Windows_Executable
)
@IF NOT %ERRORLEVEL% == 0 (
   GOTO CompileError
)

:Compile_Windows_Executable
    ECHO [93mCompiling Windows Executable...[0m
    SET COMPILER_OPTIONS=/c /nologo /DCOREENGINE_INTERNAL=1 /DCOREENGINE_SLOW=1 /MTd /FC /Oi /GR- /Gm- /EHa- /Zi /wd4201 /wd4505 
    cl.exe %COMPILER_OPTIONS% "..\..\src\Host\Windows\WindowsMain.cpp"

    @IF %ERRORLEVEL% == 0 (
        GOTO Linking_Win32_Executable
    )
    @IF NOT %ERRORLEVEL% == 0 (
        GOTO CompileError
    )
    
:Linking_Win32_Executable
   ECHO [93mLinking...[0m
   REM link.exe "WindowsMain.obj" /OUT:"CoreEngine.exe" /PDB:"CoreEngineHost.pdb" /DEBUG /MAP /OPT:ref /INCREMENTAL:NO /SUBSYSTEM:CONSOLE /NOLOGO /NODEFAULTLIB libcmt.lib libvcruntimed.lib libucrtd.lib kernel32.lib user32.lib gdi32.lib ole32.lib advapi32.lib Winmm.lib
   link.exe "WindowsMain.obj" /OUT:"CoreEngine.exe" /PDB:"CoreEngineHost.pdb" /DEBUG /MAP /OPT:ref /INCREMENTAL:NO /SUBSYSTEM:CONSOLE /NOLOGO
   
   @IF %ERRORLEVEL% == 0 (
      GOTO Copy_Files
   )
   @IF NOT %ERRORLEVEL% == 0 (
      GOTO CompileError
   )
    
:CompileError
   ECHO [91mError: Build has failed![0m
   EXIT 1

:Copy_Files
   ECHO [93mCopy files...[0m
   REM COPY *.dll ..\Windows
   COPY *.pdb ..\Windows > NUL
   COPY CoreClr.dll ..\Windows > NUL
   COPY System.Private.CoreLib.dll ..\Windows > NUL
   COPY clrjit.dll ..\Windows > NUL
   COPY System.Runtime.dll ..\Windows > NUL
   COPY System.Console.dll ..\Windows > NUL
   COPY System.Threading.dll ..\Windows > NUL
   COPY System.Runtime.Extensions.dll ..\Windows > NUL
   COPY System.Text.Encoding.Extensions.dll ..\Windows > NUL
   COPY CoreEngine.dll ..\Windows > NUL
   COPY CoreEngine.exe ..\Windows > NUL
   GOTO End
   
:End
    ECHO [92mSuccess: Compilation done.[0m
    popd
    RD /S /Q build\temp
    @ECHO ON
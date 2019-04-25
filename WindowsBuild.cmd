@ECHO OFF

IF EXIST "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\" Call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\VC\Auxiliary\Build\vcvars64.bat" > NUL
IF EXIST "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\" Call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\VC\Auxiliary\Build\vcvars64.bat" > NUL

IF NOT EXIST build\temp mkdir build\temp
IF NOT EXIST build\Windows mkdir build\Windows
pushd build\temp

DEL *.pdb > NUL 2> NUL

ECHO [93mCompiling CoreEngine Library...[0m
dotnet.exe publish --nologo -r win-x64 -c Debug -v Q --self-contained true -o "." "..\..\src\CoreEngine"

@IF %ERRORLEVEL% == 0 (
   GOTO Compile_Windows_Executable
)
@IF NOT %ERRORLEVEL% == 0 (
   GOTO CompileError
)

:Compile_Windows_Executable

   COPY "..\..\src\Host\Windows\AppxManifest.xml" . > NUL

   pushd "..\..\src\Host\Windows\"

   IF NOT EXIST "WindowsCommon.pch" (
      ECHO [93mCompiling Windows Pre-compiled header...[0m
      cl.exe /c /nologo /DDEBUG /std:c++17 /EHsc /Zi /Yc /FpWindowsCommon.pch "WindowsCommon.cpp"
   )
   
   ECHO [93mCompiling Windows Executable...[0m

   SET COMPILER_OPTIONS=/c /DDEBUG /nologo /std:c++17 /EHsc /Zi /Yu"WindowsCommon.h" /FpWindowsCommon.PCH
   cl.exe %COMPILER_OPTIONS% "CompilationUnit.cpp"
   popd

   @IF %ERRORLEVEL% == 0 (
      GOTO Linking_Win32_Executable
   )
   @IF NOT %ERRORLEVEL% == 0 (
      GOTO CompileError
   )
    
:Linking_Win32_Executable
   ECHO [93mLinking...[0m
   
   pushd "..\..\src\Host\Windows\"
   link.exe "CompilationUnit.obj" "WindowsCommon.obj" /OUT:"..\..\..\build\temp\CoreEngine.exe" /PDB:"..\..\..\build\temp\CoreEngineHost.pdb" /APPCONTAINER /DEBUG /MAP /OPT:ref /INCREMENTAL:NO /WINMD:NO /NOLOGO WindowsApp.lib 
   popd
   
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
   COPY *.dll ..\Windows > NUL
   COPY *.pdb ..\Windows > NUL
   REM COPY CoreClr.dll ..\Windows > NUL
   REM COPY System.Private.CoreLib.dll ..\Windows > NUL
   REM COPY clrjit.dll ..\Windows > NUL
   REM COPY System.Runtime.dll ..\Windows > NUL
   REM COPY System.Console.dll ..\Windows > NUL
   REM COPY System.Threading.dll ..\Windows > NUL
   REM COPY System.Runtime.Extensions.dll ..\Windows > NUL
   REM COPY System.Runtime.Loader.dll ..\Windows > NUL
   REM COPY System.Text.Encoding.Extensions.dll ..\Windows > NUL
   REM COPY System.Threading.dll ..\Windows > NUL
   REM COPY System.Threading.Tasks.dll ..\Windows > NUL
   COPY AppxManifest.xml ..\Windows > NUL
   COPY CoreEngine.dll ..\Windows > NUL
   COPY CoreEngine.exe ..\Windows > NUL
   
   pushd "..\Windows"
   powershell "add-appxpackage -register AppxManifest.xml"
   popd

   GOTO End
   
:End
    ECHO [92mSuccess: Compilation done.[0m
    popd
    REM RD /S /Q build\temp
    @ECHO ON
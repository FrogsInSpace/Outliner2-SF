@ECHO OFF

SET dir=%~dp0
SET Configuration=Release
SET targetDir=%dir%mzp\
SET assembly=%dir%dotnet\bin\%Configuration%\Outliner.dll


ECHO Checking bundle prerequisites...
CALL :GetZip zip
IF "%zip%"=="" goto :ZipNotFoundError
dir "%zip%" > nul || goto :ZipNotFoundError
ECHO Found 7Zip.


ECHO.
ECHO Getting version information from outliner assembly
for /f "usebackq tokens=1-4 delims=." %%a in (`
  powershell -NoProfile -Command ^
    "( [Reflection.AssemblyName]::GetAssemblyName('%assembly%').Version.ToString() )"
`) do (
    set VER_MAJOR=%%a
    set VER_MINOR=%%b
    set VER_BUILD=%%c
    set VER_REVISION=%%d
)

for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd"') do set TODAY=%%i

SET output=%dir%outliner-%VER_MAJOR%.%VER_MINOR%.%VER_BUILD%.%VER_REVISION%-%TODAY%.mzp

ECHO.
ECHO Removing old target files and directory...

::Remove target dir if it exists
IF EXIST %targetDir% (
  rmdir /Q /S %targetDir% || goto :error
  echo Removed %targetDir%
)

::Remove output file if it exists
IF EXIST %output% ( del /Q /S %output% || goto :error )



::Copy the maxscript dir to a temporary directory.
ECHO.
ECHO Copying maxscript to temporary mzp directory...
xcopy %dir%maxscript %targetDir% /e /q || goto :error



::Copy Outliner.dll from dotnet to maxscript
ECHO.
ECHO Copying Outliner.dll to bundle...
copy %assembly% %targetDir%script\Outliner.dll || goto :OutlinerDllError



::Create package from target dir
ECHO.
ECHO Packing mzp...
"%zip%" a -tzip %output% %targetDir%* || goto :error



::Remove target dir
rmdir /Q /S %targetDir% || goto :error



ECHO Done.
goto :eof

:GetZip
SET KEY_NAME=HKEY_LOCAL_MACHINE\SOFTWARE\7-Zip
SET KEY_VALUE=Path

FOR /F "tokens=2*" %%A IN ('REG QUERY "%KEY_NAME%" /v %KEY_VALUE% 2^>nul') DO (
   SET %~1=%%B\7z.exe
)
goto :eof

:ZipNotFoundError
ECHO Could not find 7z.exe.
goto :error

:OutlinerDllError
ECHO You may have to build the .NET library first using buildandbundle.bat
goto :error

:error
ECHO.
ECHO Bundling failed.
PAUSE
EXIT /B %ERRORLEVEL%
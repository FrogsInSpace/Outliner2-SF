@ECHO OFF
SETLOCAL enabledelayedexpansion

set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

for /f "usebackq delims=" %%i in (`
  %VSWHERE% -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
`) do set "MSBuild=%%i"
echo %MSBuild%

::Adjust the value of MaxDir to match your local 3dsmax installation directory.
IF "%ADSK_3DSMAX_x64_2027%"=="" (
    SET "MaxDir=C:\Program Files\Autodesk\3ds Max 2027\"
) ELSE (
    SET "MaxDir=%ADSK_3DSMAX_x64_2027%\"
)

::Build Outliner.dll
ECHO Building Outliner...
SET CONFIG=Release
%MSBuild% dotnet/Outliner.csproj /nologo /t:Restore;Build /p:Configuration=%CONFIG%;ReferencePath="%MaxDir%;" /verbosity:quiet || goto :error


ECHO.
call bundle.bat
goto :eof


:PathError
ECHO %~1
goto :error

:error
PAUSE
EXIT /B %ERRORLEVEL%

pause
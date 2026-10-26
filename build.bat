@echo off
setlocal
echo =========================================
echo  Building GStage IRC Server for Windows
echo =========================================

cd /d "%~dp0"
if not exist "bin" mkdir bin

rem Check if dotnet SDK is available
dotnet --list-sdks >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [1/2] Found .NET SDK. Building with dotnet...
    dotnet build GStage.csproj -c Release -o bin
    if %ERRORLEVEL% EQU 0 (
        echo [2/2] Build successful! Binary: bin\GStage.exe
        goto :done
    )
)

rem Fall back to built-in Windows C# compiler
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

if exist "%CSC%" (
    echo [1/2] Compiling with Windows built-in C# compiler (%CSC%)...
    "%CSC%" /nologo /optimize+ /target:exe /out:bin\GStage.exe /recurse:src\*.cs
    if %ERRORLEVEL% EQU 0 (
        echo [2/2] Build successful! Binary: bin\GStage.exe
        goto :done
    ) else (
        echo Build failed!
        exit /b %ERRORLEVEL%
    )
) else (
    echo Error: Could not find csc.exe compiler.
    exit /b 1
)

:done
echo Done. You can run bin\GStage.exe to start the server!
endlocal

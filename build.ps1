# GStage Windows 11 Build Script
# Compiles GStage using either the dotnet SDK or the built-in Windows C# compiler (csc.exe)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Building GStage IRC Server for Windows  " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

if (-not (Test-Path "bin")) {
    New-Item -ItemType Directory -Path "bin" | Out-Null
}

$dotnetSdk = $false
try {
    $sdks = & dotnet --list-sdks 2>$null
    if ($sdks) {
        $dotnetSdk = $true
    }
} catch { }

if ($dotnetSdk) {
    Write-Host "[1/2] Found .NET SDK. Building with 'dotnet build'..." -ForegroundColor Green
    & dotnet build GStage.csproj -c Release -o bin
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[2/2] Build successful! Binary located at bin\GStage.exe" -ForegroundColor Green
        exit 0
    }
    Write-Host "Dotnet build had warnings/errors. Falling back to built-in csc.exe..." -ForegroundColor Yellow
}

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (Test-Path $cscPath) {
    Write-Host "[1/2] Compiling with Windows built-in C# compiler: $cscPath" -ForegroundColor Green
    $sourceFiles = Get-ChildItem -Path "src" -Recurse -Filter "*.cs" | Select-Object -ExpandProperty FullName
    
    $outExe = Join-Path $scriptDir "bin\GStage.exe"
    
    & $cscPath /nologo /optimize+ /target:exe /out:$outExe /recurse:src\*.cs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[2/2] Build successful!" -ForegroundColor Green
        Write-Host "Output executable: $outExe" -ForegroundColor Cyan
        exit 0
    } else {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} else {
    Write-Host "Error: No suitable C# compiler found on this system." -ForegroundColor Red
    exit 1
}

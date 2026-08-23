@echo off
setlocal
cd /d "%~dp0"

echo ===============================================
echo       PM5 Control Center - automatic start
echo ===============================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK was not found.
    echo Install .NET 10 SDK and run this file again.
    pause
    exit /b 1
)

echo [1/3] Testing Core and simulator...
dotnet test "tests\PM5Control.Core.Tests\PM5Control.Core.Tests.csproj" --configuration Release
if errorlevel 1 (
    echo.
    echo ERROR: Tests failed. The application was not started.
    pause
    exit /b 1
)

echo.
echo [2/3] Publishing Windows application...
dotnet publish "src\PM5Control.Desktop\PM5Control.Desktop.csproj" --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true --output "artifacts\PM5Control-Windows"
if errorlevel 1 (
    echo.
    echo ERROR: Windows publish failed.
    pause
    exit /b 1
)

echo.
echo [3/3] Starting PM5 Control Center...
start "PM5 Control Center" "%~dp0artifacts\PM5Control-Windows\PM5Control.Desktop.exe"
exit /b 0

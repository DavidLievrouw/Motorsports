@echo off
CLS
ECHO.
setlocal & pushd .

set "DIR=%~dp0"
cd %~dp0
TITLE Motorsports -- Publish
cd ./src/Build
Powershell.exe -File build.ps1 -Target Publish -Configuration Release -PublishEnvironment Production -Verbosity Normal -SkipInstallDotNetCoreCli True
CHOICE /T 60 /C yYnN /CS /D y  /M "Should this window close? [Default y, you have 60 seconds]:"
if errorlevel 2 pause
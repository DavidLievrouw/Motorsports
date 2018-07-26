@echo off
CLS
ECHO.
setlocal & pushd .

set "DIR=%~dp0"
cd %~dp0
TITLE Motorsports -- Publish
cd ./src/Build
Powershell.exe -File build.ps1 -Target Publish -Configuration Release -PublishEnvironment Production -Verbosity Normal -SkipInstallDotNetCoreCli True

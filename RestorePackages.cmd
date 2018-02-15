@echo off
CLS
set "DIR=%~dp0"
cd %~dp0
TITLE Motorsports -- RestorePackages
cd ./src/Build
Powershell.exe -File build.ps1 -Target RestorePackages -Verbosity Normal -SkipInstallDotNetCoreCli True
CHOICE /T 60 /C yYnN /CS /D y  /M "Should this window close? [Default y, you have 60 seconds]:"
if errorlevel 2 pause
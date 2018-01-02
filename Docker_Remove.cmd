@echo off
CLS
set "DIR=%~dp0"
cd %~dp0
TITLE Motorsports -- Remove docker container
docker stop motorsports
docker rm motorsports
docker image rm motorsports
CHOICE /T 60 /C yYnN /CS /D y  /M "Should this window close? [Default y, you have 60 seconds]:"
if errorlevel 2 pause
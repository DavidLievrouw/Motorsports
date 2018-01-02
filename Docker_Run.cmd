@echo off
CLS
set "DIR=%~dp0"
cd %~dp0
TITLE Motorsports -- Run docker container
docker stop motorsports
docker rm motorsports
docker image rm motorsports
docker build -t motorsports src\Motorsports.Scaffolding.Core
docker run -d -p 8080:80 --name motorsports motorsports
ping 127.0.0.1 -n 4 > nul 
start "" http://localhost:8080
CHOICE /T 60 /C yYnN /CS /D y  /M "Should this window close? [Default y, you have 60 seconds]:"
if errorlevel 2 pause
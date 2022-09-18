@echo off
set WORKINGDIR=%cd%
set DIR=%~dp0
set PROJECT=portainer
cd %DIR%
docker-compose --file docker-compose.yml --project-name %PROJECT% down --rmi local --remove-orphans
cd %WORKINGDIR%
if errorlevel 2 pause
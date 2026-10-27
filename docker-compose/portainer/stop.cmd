@echo off
set "WORKINGDIR=%cd%"
set "DIR=%~dp0"
set "PROJECT=portainer"
cd /d "%DIR%" || exit /b 1
docker-compose --file "%DIR%docker-compose.yml" --project-name %PROJECT% down --rmi local --remove-orphans
if errorlevel 2 pause
@echo off
set "WORKINGDIR=%cd%"
set "DIR=%~dp0"
set "PROJECT=mariadb"
cd /d "%DIR%" || exit /b 1
docker-compose --file "%DIR%docker-compose-dev.yml" --project-name %PROJECT% down --rmi local --remove-orphans
docker-compose --file "%DIR%docker-compose-dev.yml" --project-name %PROJECT% up --detach
if errorlevel 2 pause
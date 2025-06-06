@echo off
set WORKINGDIR=%cd%
set DIR=%~dp0
set PROJECT=mariadb
cd %DIR%
docker-compose --file docker-compose-dev.yml --project-name %PROJECT% down --rmi local --remove-orphans
docker-compose --file docker-compose-dev.yml --project-name %PROJECT% up --detach
cd %WORKINGDIR%
if errorlevel 2 pause
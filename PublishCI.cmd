REM Remove this file when Azure DevOps supports Cake.Frosting by nuget.org
TITLE Monitoring -- Publish
dotnet run --project "./src/Build/Build.csproj" --target Publish --configuration Release --no-launch-profile

# Copy source
FROM microsoft/aspnetcore-build:2.0.4-2.1.3 AS build-env
WORKDIR /app
COPY *.csproj .

# Publish
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out

# Run
FROM microsoft/aspnetcore:2.0.4
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Motorsports.Scaffolding.Core.dll"]

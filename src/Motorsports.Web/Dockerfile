# Build context: the repository root.  docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:latest .
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /repo
COPY global.json version.txt ./
COPY src/Directory.Build.props src/Directory.Packages.props src/version.props src/
COPY src/Motorsports.Common/*.csproj src/Motorsports.Common/
COPY src/Motorsports.Infrastructure/*.csproj src/Motorsports.Infrastructure/
COPY src/Motorsports.Auth/*.csproj src/Motorsports.Auth/
COPY src/Motorsports.Home/*.csproj src/Motorsports.Home/
COPY src/Motorsports.Participants/*.csproj src/Motorsports.Participants/
COPY src/Motorsports.Rounds/*.csproj src/Motorsports.Rounds/
COPY src/Motorsports.Seasons/*.csproj src/Motorsports.Seasons/
COPY src/Motorsports.Sports/*.csproj src/Motorsports.Sports/
COPY src/Motorsports.Teams/*.csproj src/Motorsports.Teams/
COPY src/Motorsports.Ui/*.csproj src/Motorsports.Ui/
COPY src/Motorsports.Venues/*.csproj src/Motorsports.Venues/
COPY src/Motorsports.Web/*.csproj src/Motorsports.Web/
COPY .editorconfig ./
COPY src/Motorsports.Common src/Motorsports.Common
COPY src/Motorsports.Infrastructure src/Motorsports.Infrastructure
COPY src/Motorsports.Auth src/Motorsports.Auth
COPY src/Motorsports.Home src/Motorsports.Home
COPY src/Motorsports.Participants src/Motorsports.Participants
COPY src/Motorsports.Rounds src/Motorsports.Rounds
COPY src/Motorsports.Seasons src/Motorsports.Seasons
COPY src/Motorsports.Sports src/Motorsports.Sports
COPY src/Motorsports.Teams src/Motorsports.Teams
COPY src/Motorsports.Ui src/Motorsports.Ui
COPY src/Motorsports.Venues src/Motorsports.Venues
COPY src/Motorsports.Web src/Motorsports.Web
# Razor files must be present during restore so the framework static assets package is included.
RUN dotnet restore src/Motorsports.Web/Motorsports.Web.csproj
ARG SOURCE_REVISION_ID=
RUN dotnet publish src/Motorsports.Web/Motorsports.Web.csproj -c Release --no-restore -o /out \
    -p:SourceRevisionId=${SOURCE_REVISION_ID} -p:UseAppHost=false \
    && test -s /out/wwwroot/_framework/blazor.web.js

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
# tzdata is required: user time zones are IANA ids and the image runs with InvariantGlobalization.
RUN apt-get update && apt-get install -y --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0 \
    DOTNET_GCHeapHardLimitPercent=50 \
    DOTNET_TieredPGO=1
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s CMD curl -fs http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Motorsports.Web.dll"]

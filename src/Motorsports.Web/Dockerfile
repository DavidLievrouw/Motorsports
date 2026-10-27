# Build context
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /repo
COPY global.json version.txt .editorconfig ./
COPY src/ ./src/

# Razor files must be present during restore so the framework static assets package is included.
ARG SOURCE_REVISION_ID=
RUN dotnet restore src/Motorsports.Web/Motorsports.Web.csproj \
    && dotnet publish src/Motorsports.Web/Motorsports.Web.csproj -c Release --no-restore -o /out \
    -p:SourceRevisionId=${SOURCE_REVISION_ID} -p:UseAppHost=false \
    && test -s /out/wwwroot/_framework/blazor.web.js

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra

WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0 \
    DOTNET_GCHeapHardLimitPercent=50 \
    DOTNET_TieredPGO=1
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=15s --start-period=30s CMD ["dotnet", "Motorsports.Web.dll", "--health-check"]
ENTRYPOINT ["dotnet", "Motorsports.Web.dll"]

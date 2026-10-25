# Motorsports

Motorsports is a personal motorsport-event tracker. The repository currently contains the legacy .NET 8 MVC app and its .NET 10 Blazor replacement, which share a MariaDB schema during the transition. The legacy application is read-only for development; new features belong in `src/Motorsports.Web` and its feature slices.

## Requirements

- .NET SDK 10.0.200 (selected by `global.json`)
- Docker with Compose for MariaDB or the containerized app
- MariaDB 11.7 for local development and production

## Local development

1. Start a MariaDB instance. `docker-compose/mariadb/docker-compose-dev.yml` requires `MARIADB_ROOT_PASSWORD` and `MARIADB_PASSWORD` environment variables; set them in your shell before running Compose. Optional `MARIADB_USER` and `MARIADB_DATABASE` values default to `motorsports`.
2. Restore the schema and synthetic development data into a development database. The real backups under `src/Db` contain personal data and password hashes; do not use them in tests or print their contents. If restoring a local dump, use `--default-character-set=utf8mb4`.
3. Configure the new app's database connection with user-secrets:

   ```powershell
   dotnet user-secrets set --project src/Motorsports.Web "ConnectionStrings:Motorsports" "Server=localhost;Port=3306;Database=<development-database>;User=<database-user>;Password=<local-password>"
   ```

   Do not put credentials in source-controlled files. Local Development config disables Secure cookies for plain HTTP; never carry that setting into a public deployment.
4. Run the app:

   ```powershell
   dotnet run --project src/Motorsports.Web
   ```

   The app listens on `http://localhost:4100` when using the repository launch settings. `GET /health` reports host health. Database migrations run automatically before the app accepts requests.

## Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:Motorsports` | MariaDB connection; configure with user-secrets or `ConnectionStrings__Motorsports` |
| `App:TimeZone` | Fallback IANA time zone if browser time-zone detection is unavailable; defaults to `Europe/Brussels` |
| `Auth:SecureCookies` | Secure-cookie policy; keep enabled outside local plain-HTTP development |
| `Auth:LoginPermitLimit` | Login attempts per client IP in the configured window; defaults to 5 |
| `Auth:LoginWindowSeconds` | Login rate-limit window; defaults to 60 seconds |
| `ForwardedHeaders:KnownProxies` | Trusted proxy IP addresses. Configure the TLS-terminating proxy so client IPs and HTTPS are forwarded safely |

In containers, use the corresponding double-underscore environment variable names, such as `App__TimeZone` and `ForwardedHeaders__KnownProxies__0`. Forwarded headers are accepted only from configured proxies.

## Build and tests

```powershell
dotnet build src/Motorsports.slnx -warnaserror
dotnet csharpier format .
dotnet csharpier check .
dotnet test --solution src/Motorsports.slnx
```

Characteristics tests use Testcontainers MariaDB and synthetic data. Run the Home query benchmark against a populated MariaDB using `ConnectionStrings__Motorsports`:

```powershell
dotnet run -c Release --project src/tests/Motorsports.Benchmarks -- --filter "*HomeOverview*"
```

## Docker

Build from the repository root:

```powershell
docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:latest .
```

`docker-compose/motorsports-web/docker-compose.yml` requires `MOTORSPORTS_CONNECTION_STRING` and `FORWARDED_HEADERS_KNOWN_PROXY_0` from the deployment environment. The latter must be the proxy's IP as seen by the app container. Place the app behind HTTPS in production and keep Secure cookies enabled. The container listens on port 8080 and exposes `/health` as its health check.

## Database migrations and transition

The app migrates the shared database at startup under a MariaDB named lock. Take a database backup before the first deployment that applies migrations. Current changes are additive and legacy-compatible: nullable authentication/image columns and the approved participant identity index. Migrations are idempotent; keep the legacy app's existing columns and tables intact while both apps are in use.

Cut-over is a manual operation: deploy and verify the new app before removing the legacy project, `Build`, or database assets. Do not remove them as part of a normal feature change.

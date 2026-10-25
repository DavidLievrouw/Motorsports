# Motorsports

Motorsports is a personal tracker for motorsport seasons and events. It lets the owner manage sports, seasons, teams and
their season-specific entries, participants, venues, rounds, results, and event conditions. The home page highlights
what to watch next and provides a history of completed events. The application is login-protected and supports sports
such as Formula 1 and MotoGP.

The current release is **2.0.0**. The application uses .NET 10, Blazor interactive server rendering, MudBlazor, EF Core,
and MariaDB. It is designed for phones and desktop browsers, uses a dark theme by default, and adds subtle sport
branding.

## Requirements

- .NET SDK 10.0.200 (selected by `global.json`)
- Docker with Compose for MariaDB or the containerized app
- MariaDB 11.7 for local development and production

## Local development

1. Start MariaDB. `docker-compose/mariadb/docker-compose-dev.yml` requires `MARIADB_ROOT_PASSWORD` and
   `MARIADB_PASSWORD` environment variables. Optional `MARIADB_USER` and `MARIADB_DATABASE` values default to
   `motorsports`.
2. Provision a development database with the application schema and synthetic development data. Backups under
   `src/Db` contain personal data and password hashes; do not use them in tests or print their contents. Restore any
   local dump with `--default-character-set=utf8mb4`.
3. Configure the database connection with user-secrets:

   ```powershell
   dotnet user-secrets set --project src/Motorsports.Web "ConnectionStrings:Motorsports" "Server=localhost;Port=3306;Database=<development-database>;User=<database-user>;******"
   ```

   Do not put credentials in source-controlled files. Local Development config disables Secure cookies for plain HTTP;
   never carry that setting into a public deployment.
4. Run the app:

   ```powershell
   dotnet run --project src/Motorsports.Web
   ```

   The app listens on `http://localhost:4100` with the repository launch settings. `GET /health` reports host health.
   Database migrations run automatically before the app accepts requests.

## Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:Motorsports` | MariaDB connection; configure with user-secrets or `ConnectionStrings__Motorsports` |
| `App:TimeZone` | Fallback IANA time zone if browser time-zone detection is unavailable; defaults to `Europe/Brussels` |
| `Auth:SecureCookies` | Secure-cookie policy; keep enabled outside local plain-HTTP development |
| `Auth:LoginPermitLimit` | Login attempts per client IP in the configured window; defaults to 5 |
| `Auth:LoginWindowSeconds` | Login rate-limit window; defaults to 60 seconds |
| `ForwardedHeaders:KnownProxies` | Trusted proxy IP addresses. Configure the TLS-terminating proxy so client IPs and HTTPS are forwarded safely |

In containers, use the corresponding double-underscore environment variable names, such as `App__TimeZone` and
`ForwardedHeaders__KnownProxies__0`. Forwarded headers are accepted only from configured proxies.

## Build and tests

```powershell
dotnet build src/Motorsports.slnx -warnaserror
dotnet csharpier format .
dotnet csharpier check .
dotnet test --solution src/Motorsports.slnx
```

Characteristics tests use Testcontainers MariaDB and synthetic data. Run the Home query benchmark against a populated
MariaDB using `ConnectionStrings__Motorsports`:

```powershell
dotnet run -c Release --project src/tests/Motorsports.Benchmarks -- --filter "*HomeOverview*"
```

## Release and Docker deployment

Build the release image from the repository root, tagging it with the version in `version.txt`:

```powershell
docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:2.0.0 .
```

`docker-compose/motorsports-web/docker-compose.yml` requires `MOTORSPORTS_CONNECTION_STRING` and
`FORWARDED_HEADERS_KNOWN_PROXY_0` from the deployment environment. Back up the database before the first deployment
that applies migrations. Configure HTTPS at the trusted proxy and keep Secure cookies enabled. The app currently uses
container-local ASP.NET Data Protection keys; configure durable, access-controlled key storage before production
deployment so authentication and antiforgery cookies remain valid when containers are replaced. The app container
listens on port 8080 and exposes `/health` as its health check.

## Database migrations

Migrations run automatically at startup under a MariaDB named lock and are designed to be idempotent across
concurrent app instances. Take a database backup before applying schema changes. Each schema change must have a
matching `Down` migration; check generated migration SQL with `dotnet ef migrations script --idempotent`.

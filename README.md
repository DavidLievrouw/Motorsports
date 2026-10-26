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
`FORWARDED_HEADERS_KNOWN_PROXY_0` from the deployment environment. Production runs behind nginx-proxy-manager at
`ms.dalion.eu`; only the trusted proxy at `172.18.0.3` should reach the app container, which listens on port 80 in that
deployment. Keep Secure cookies enabled and back up the database before the first deployment that applies migrations.
The Data Protection key ring is stored in the `DataProtectionKeys` table in the configured database and is included in
normal database backups. Database access exposes the unencrypted key XML, so restrict database credentials and backup
access accordingly.

Restore the database and its Data Protection key table from the same backup point. If the key table is missing,
inconsistent with the restored application data, or the application discriminator changes, existing authentication and
antiforgery tokens may become unreadable and users will need to sign in again. ASP.NET Data Protection creates new
keys automatically (90-day default lifetime); retain older keys so still-valid cookies and tokens can be decrypted.
For a compromised key, revoke it through the Data Protection `IKeyManager` API using an authorized maintenance
procedure; revocation invalidates tokens protected by that key. The app container exposes `/health` as its health check.
The runtime uses the .NET 10 `noble-chiseled-extra` image with IANA time-zone data. Its exec-form health probe calls
`/health` through `dotnet` without a shell or `curl`; set `HEALTHCHECK_HOST` if the host allowed by `AllowedHosts`
differs from `ms.dalion.eu`.

The Content Security Policy is enforced after browser verification of the report-only policy. Scripts are restricted
to same-origin resources and a per-response nonce; inline styles remain allowed for MudBlazor. Framing is prohibited
with `frame-ancestors 'none'`. Browser-extension script injection is not a reason to weaken the application policy.
## Database migrations

Migrations run automatically at startup under a MariaDB named lock and are designed to be idempotent across
concurrent app instances. Take a database backup before applying schema changes. Each schema change must have a
matching `Down` migration; check generated migration SQL with `dotnet ef migrations script --idempotent`.

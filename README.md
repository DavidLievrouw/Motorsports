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

   The app listens on `http://localhost:4100` with the repository launch settings. `GET /health` checks MariaDB.
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
| `Health:PublicUrl` | Public HTTPS origin, default `https://ms.dalion.eu`; checked outside Development |

In containers, use the corresponding double-underscore environment variable names, such as `App__TimeZone` and
`ForwardedHeaders__KnownProxies__0`. Forwarded headers are accepted only from configured proxies.

## Build and tests

```powershell
dotnet build src/Motorsports.slnx -warnaserror
dotnet csharpier format .
dotnet csharpier check .
dotnet test --solution src/Motorsports.slnx
node --test src\tests\browser\reconnect.test.cjs
```

The dependency-free Node tests exercise the mobile reconnect handler's browser lifecycle and network-failure paths.

Characteristics tests use Testcontainers MariaDB and synthetic data. Run the Home query benchmark against a populated
MariaDB using `ConnectionStrings__Motorsports`:

```powershell
dotnet run -c Release --project src/tests/Motorsports.Benchmarks -- --filter "*HomeOverview*"
```

For the reproducible Phase 8 query/index audit, point `ConnectionStrings__Motorsports` at the disposable
**`motorsports_phase8`** database, then run from the repository root:

```powershell
dotnet run -c Release --project src\tests\Motorsports.Benchmarks -- --phase8-audit-reset "$env:TEMP\motorsports-phase8-audit.json"
```

**Destructive:** this resets the application's tables, including users, migration history, and Data Protection keys,
then replaces their contents with synthetic fixtures at current-size and 10× scales. Never use a production database.
The exact database name is checked before connecting; all other names are rejected. The account needs schema-change
permissions on that database. Do not run a web app against it while auditing.

The JSON report contains warmed p50/p95 timings, approximate process-wide managed allocations, SQL command counts,
actual MariaDB query plans, and before/indexed/restored comparisons for six candidate indexes. The audit removes the
retained performance index for the baseline, restores it afterward, and records final indexed workloads separately.
Temporary candidate indexes are removed even if a measurement fails. Run without concurrent builds/tests for useful
timings; allocations include harness/background overhead and are not retained-memory or circuit-size measurements.
Recorded evidence and index decisions are in `src\PLAN.md`, section 4b.

## Mobile connection recovery

Interactive controls require a live Blazor server connection. Phone sleep, background tabs, and Wi-Fi/mobile-data
switches can interrupt it. Disconnected sessions are retained for five minutes, capped at 20 sessions per app instance.
The client retries automatically while visible and online, including after returning to the app or regaining connectivity.
The reconnect dialog blocks input only while recovery is needed; no manual refresh is required for connection recovery.

Reconnecting to a retained session preserves its form state. If the session has expired, been evicted, or the server
restarted, recovery automatically reloads the current URL. Unsaved edits are then lost; submissions are never replayed.
This is connection recovery, not offline editing or durable form storage. Enable WebSocket support on the reverse proxy;
production proxy logs and phone-browser diagnostics are needed to identify the exact source of any remaining disconnects.

## Release and Docker deployment

Build the release image from the repository root, tagging it with the version in `version.txt`:

```powershell
docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:2.0.0 .
```

`docker-compose/motorsports-web/docker-compose.yml` requires `MOTORSPORTS_CONNECTION_STRING` (and
`FORWARDED_HEADERS_KNOWN_PROXY_0` if the proxy is not at `172.18.0.3`). Its `motorsports` container is NPM's upstream,
publishes no host port, and listens on port 80 via
`Kestrel__Endpoints__Http__Url` (use the double-underscore spelling: the health probe reads that variable). Production runs behind nginx-proxy-manager at
`ms.dalion.eu`; only the trusted proxy at `172.18.0.3` should reach the app container, which listens on port 80 in that
deployment. Keep Secure cookies enabled and back up the database before the first deployment that applies migrations.
The Data Protection key ring is stored in the `DataProtectionKeys` table in the configured database and is included in
normal database backups. Database access exposes the unencrypted key XML, so restrict database credentials and backup
access accordingly.

The application database user needs only these grants on its own database (startup migrations need the DDL rights;
`GET_LOCK` needs none). No global rights, no `DROP`, routines, triggers, events, or `GRANT OPTION`:

```sql
REVOKE ALL PRIVILEGES ON `motorsports`.* FROM 'motorsports'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES ON `motorsports`.* TO 'motorsports'@'%';
```

Rolling back a migration (`Down`) needs `DROP`; run it with an admin account, not the application user.

Restore the database and its Data Protection key table from the same backup point. If the key table is missing,
inconsistent with the restored application data, or the application discriminator changes, existing authentication and
antiforgery tokens may become unreadable and users will need to sign in again. ASP.NET Data Protection creates new
keys automatically (90-day default lifetime); retain older keys so still-valid cookies and tokens can be decrypted.
For a compromised key, revoke it through the Data Protection `IKeyManager` API using an authorized maintenance
procedure; revocation invalidates tokens protected by that key. The app container exposes `/health` as its health check.
The runtime uses the .NET 10 `noble-chiseled-extra` image with IANA time-zone data. Its exec-form health probe calls
`/health` through `dotnet` without a shell or `curl`; set `HEALTHCHECK_HOST` if the host allowed by `AllowedHosts`
differs from `ms.dalion.eu`.

Both application Compose files explicitly configure the .NET health probe (the chiseled image has no shell or
`curl`), with a 15-second Docker timeout and a 10-second local HTTP timeout. Redeploy/recreate the container
after updating a Portainer stack; restarting alone does not apply changed health-check configuration.

### Health checks

`GET /health` returns 200 only when its checks succeed, otherwise 503. It queries MariaDB's `Country` table
through EF Core to verify connectivity, authentication, and access to the configured application schema.
Outside Development it also requests `Health:PublicUrl` + `/health/live` through normal DNS and HTTPS
certificate validation. Redirects, unexpected responses, network errors, and timeouts fail the check.
Both checks have five-second timeouts and run concurrently. Failure details are logged server-side;
the anonymous response exposes only the overall status, not exceptions or credentials.

`GET /health/live` returns the fixed application marker without dependency checks, preventing recursive
public health requests. Keep both paths accessible through the reverse proxy. The public check is not
registered in Development. The public URL must be an HTTPS origin without a path, credentials, query, or fragment;
override it with `Health__PublicUrl` for another deployment.

The public check originates inside the app container: it tests the public DNS/TLS/proxy route as seen there,
not independent Internet access. Hairpin routing must work. Use an external uptime monitor against the public
`/health` URL to verify reachability from outside your network. Docker unhealthy status alone does not restart
containers under `restart: unless-stopped`; do not use dependency readiness as an automatic restart trigger.

The Content Security Policy is enforced after browser verification of the report-only policy. Scripts are restricted
to same-origin resources and a per-response nonce; inline styles remain allowed for MudBlazor. Framing is prohibited
with `frame-ancestors 'none'`. Browser-extension script injection is not a reason to weaken the application policy.
## Database migrations

Migrations run automatically at startup under a MariaDB named lock and are designed to be idempotent across
concurrent app instances. Take a database backup before applying schema changes. Each schema change must have a
matching `Down` migration; check generated migration SQL with `dotnet ef migrations script --idempotent`.

The legacy application is retired. Migration `20261027074827_RemoveLegacyPasswordColumns` removes `User.PasswordHash`,
`Salt`, `Iterations`, and `Prf`. Authentication uses only the existing Identity hash in `PasswordHashV3`; account IDs,
security/concurrency stamps, and Data Protection keys are unchanged.

Stop all app instances and back up the database before deploying this schema change; old binaries cannot run against
the updated schema. Every active account must already have an Identity hash. If one does not, startup stops with
`Active users need Identity password hashes before removing legacy credentials.` Complete a password reset using
ASP.NET Identity before deploying; the old PBKDF2 hashes cannot be converted without the user's password. Do not copy
old hash bytes into `PasswordHashV3`. Deleted accounts do not block the migration and remain unable to sign in.

`Down` recreates the removed columns with empty values; it cannot recover deleted credentials. Restore a backup if
those values are needed. Historical migrations and the pre-migration schema fixture remain for upgrade tests, not
runtime backward compatibility. No personal backup files are changed by this cleanup.

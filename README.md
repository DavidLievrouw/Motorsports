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
| `DataProtection:CertificatePath` | PKCS#12 certificate that encrypts the Data Protection key ring; required outside Development |
| `DataProtection:CertificatePassword` | Password of that PKCS#12 file |
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
The Data Protection key ring is stored in the `DataProtectionKeys` table in the configured database. `backup.ps1`
excludes its rows but includes its schema: a clean restore creates new keys and requires every user to sign in again.
Outside Development the key XML is encrypted with an X.509 certificate kept outside the
database and its backups. The application refuses to start without a valid RSA certificate containing its private key.
Compose sets `DataProtection__CertificatePath` to `/run/secrets/dataprotection.pfx` and requires
`DATAPROTECTION_CERTIFICATE_PASSWORD` for `DataProtection__CertificatePassword`. The host PFX is bind-mounted
read-only from `/etc/dalion/motorsports/secrets/dataprotection.pfx`. It must be readable by the non-root app user and backed up separately; without it,
encrypted keys cannot be read. Keys written before this setting was introduced remain unencrypted: before first
protected startup, back up the database, stop the old app, and delete all existing rows in `DataProtectionKeys`. This
signs all users out. Earlier database backups still contain the unencrypted keys; restrict or retire those backups and do
not restore their old key rows into the protected deployment. Replacing the certificate without preserving access to the
old one makes older keys unreadable and signs users out; plan certificate rotation explicitly.

#### Certificate setup and first deployment

Run these commands on the Docker host from the repository root (the directory containing `version.txt`). The Compose
file contains secrets after configuration; restrict access to it and never commit the modified file.

If you do not already have the certificate that protects this key ring, create one once. Do not generate a replacement
just because the app image is being updated: existing keys require the same certificate. These commands create the
directory if needed, generate a self-signed RSA certificate, and export a password-protected PKCS#12 file. OpenSSL
prompts for the PFX password; keep it for the Compose configuration and a secure backup. The .NET chiseled image runs
as UID 1654; the permissions below allow it to read the certificate without making it accessible to other host users.

```sh
sudo mkdir -p /etc/dalion/motorsports/secrets
sudo chown root:1654 /etc/dalion/motorsports/secrets
sudo chmod 0710 /etc/dalion/motorsports/secrets
sudo sh -c '
  set -eu
  umask 077
  openssl req -x509 -newkey rsa:3072 -sha256 -nodes -days 3650 \
    -keyout /etc/dalion/motorsports/secrets/key.pem \
    -out /etc/dalion/motorsports/secrets/certificate.pem \
    -subj "/CN=Motorsports Data Protection"
  openssl pkcs12 -export \
    -out /etc/dalion/motorsports/secrets/dataprotection.pfx \
    -inkey /etc/dalion/motorsports/secrets/key.pem \
    -in /etc/dalion/motorsports/secrets/certificate.pem \
    -name "Motorsports Data Protection"
  rm /etc/dalion/motorsports/secrets/key.pem /etc/dalion/motorsports/secrets/certificate.pem
  chown 1654:1654 /etc/dalion/motorsports/secrets/dataprotection.pfx
  chmod 0400 /etc/dalion/motorsports/secrets/dataprotection.pfx
'
```

The temporary PEM private key is unencrypted, so create it under a root-only umask and remove it after export as shown.
Keep a protected backup of the PFX and its password separately from the database backups. If you already have the
certificate, do not run the generation commands; use its existing PFX and password. Ensure the PFX is readable by UID
1654 and that every parent directory permits traversal by that UID.

In `docker-compose/motorsports-web/docker-compose.yml`, replace the `${...}` substitutions with the deployment
values. For example, change the relevant entries to:

| Compose entry | Value to use |
|---|---|
| `image: motorsports-web:${MOTORSPORTS_VERSION:-2.0.0}` | `image: motorsports-web:2.0.0` (or the release being deployed) |
| `ConnectionStrings__Motorsports=${MOTORSPORTS_CONNECTION_STRING:?...}` | `ConnectionStrings__Motorsports=<full production MariaDB connection string>` |
| `ForwardedHeaders__KnownProxies__0=${FORWARDED_HEADERS_KNOWN_PROXY_0:-172.18.0.3}` | `ForwardedHeaders__KnownProxies__0=<trusted proxy IP>` |
| `DataProtection__CertificatePassword=${DATAPROTECTION_CERTIFICATE_PASSWORD:?...}` | `DataProtection__CertificatePassword=<PFX password>` |
| `DataProtection__CertificatePath` | Keep `/run/secrets/dataprotection.pfx` (container path) |
| Bind mount source | Keep `/etc/dalion/motorsports/secrets/dataprotection.pfx` (host path) |

If a password or connection-string value contains `$`, write it as `$$` in the Compose file to prevent Compose variable interpolation.
Restrict access to the edited Compose file because it now contains production credentials.

For the first protected deployment, take a database backup, stop the old app, and delete the existing plaintext
`DataProtectionKeys` rows using an authorized database session (`DELETE FROM DataProtectionKeys;`) before starting the
new container. This deliberately signs everyone out. Do not clear the table on later deployments. Then build and deploy
from the repository root:

```sh
docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:2.0.0 .
docker compose -f docker-compose/motorsports-web/docker-compose.yml config --quiet
docker compose -f docker-compose/motorsports-web/docker-compose.yml up -d --force-recreate
docker compose -f docker-compose/motorsports-web/docker-compose.yml ps
```

Confirm the container is healthy before considering deployment complete; check its logs if startup fails, without
printing secrets or key XML.

The application database user needs only these grants on its own database (startup migrations need the DDL rights;
`GET_LOCK` needs none). No global rights, no `DROP`, routines, triggers, events, or `GRANT OPTION`:

```sql
REVOKE ALL PRIVILEGES ON `motorsports`.* FROM 'motorsports'@'%';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES ON `motorsports`.* TO 'motorsports'@'%';
```

Rolling back a migration (`Down`) needs `DROP`; run it with an admin account, not the application user.

Stop all app instances before a clean restore of a `backup.ps1` dump, then restart them. The restored key table is
empty even though migration history is preserved. ASP.NET Data Protection automatically persists a fresh key ring;
old authentication and antiforgery tokens are invalid, but users can sign in again with their restored credentials.
The configured certificate is still required outside Development. If restoring another backup format with key rows,
use only encrypted keys from the same backup point and retain the matching certificate separately. A physically
missing table with an already-applied migration is not an empty ring: restore its schema too.
ASP.NET Data Protection rotates keys automatically (90-day default lifetime); during normal operation retain older
keys so still-valid cookies and tokens can be decrypted.
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

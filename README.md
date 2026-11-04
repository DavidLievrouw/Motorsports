# Motorsports

Motorsports is a personal pet project. It's a tracker for motorsport seasons and events. It manages sports, seasons, teams and their
season-specific entries, participants, venues, rounds, results, and event conditions. The home page highlights upcoming
events and recent history. The app is login-protected and supports sports such as Formula 1 and MotoGP.

The current release is **2.0.0** (`version.txt`). It uses .NET 10, Blazor, MudBlazor, EF Core, and MariaDB.

## Local development

Requirements: .NET SDK 10.0.200 (selected by `global.json`) and Docker Compose.

1. Start the development database:

   ```powershell
   docker compose -f docker-compose/mariadb/docker-compose-dev.yml up -d
   ```

2. Set `ConnectionStrings:Motorsports` with user-secrets. Use the local database settings from the development Compose
   file; keep credentials out of source-controlled files.

   ```powershell
   dotnet user-secrets set --project src/Motorsports.Web "ConnectionStrings:Motorsports" "<local MariaDB connection string>"
   ```

3. Run the app:

   ```powershell
   dotnet run --project src/Motorsports.Web
   ```

   Open `http://localhost:4100`. Database migrations run automatically before the app accepts requests. Use synthetic
   development data; do not restore personal backups into tests or print their contents.

## Playwright browser regressions

The C# Playwright suite starts the app on an ephemeral loopback port and creates disposable MariaDB 12.3.3
Testcontainers. It needs Docker and installed Playwright browser binaries; no local application database or manual app
startup is used. All test records are synthetic. Test-owned processes and containers are disposed automatically.
Browser cases are grouped by authentication, shell/theme, sports, venues, team/participant/season-entry, and rounds/Home;
the browser profiles and shared interaction helpers live in `BrowserRegressionTestBase.cs`.

From the repository root:

```powershell
dotnet build src\tests\Motorsports.PlaywrightTests\Motorsports.PlaywrightTests.csproj
pwsh .\src\tests\Motorsports.PlaywrightTests\bin\Debug\net10.0\playwright.ps1 install chromium firefox webkit
dotnet test --solution src\Motorsports.slnx
```

Run one test method directly with the Microsoft.Testing.Platform runner:

```powershell
dotnet .\src\tests\Motorsports.PlaywrightTests\bin\Debug\net10.0\Motorsports.PlaywrightTests.dll -method '*Venue_country_picker_and_unicode_key_survive_create_edit*'
```

Failure screenshots and Playwright traces are retained under `%TEMP%\Motorsports.Playwright`; successful tests do not
write artifacts there. The suite currently covers initial cross-browser workflows, not the full matrix in
`src\PLAN-PW.md`; the Firefox CSP diagnostic recorded there remains an explicit open defect.

## Build and deploy

Run from the repository root. Build and test the solution with:

```powershell
dotnet build src/Motorsports.slnx -warnaserror
dotnet csharpier check .
dotnet test --solution src/Motorsports.slnx
```

Build the Linux release image, using the version in `version.txt` as its tag:

```powershell
docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:2.0.0 .
```

Deployment is manual, using `docker-compose/motorsports-web/docker-compose.yml` behind a trusted TLS-terminating proxy.
Configure `MOTORSPORTS_VERSION`, `MOTORSPORTS_CONNECTION_STRING`, `FORWARDED_HEADERS_KNOWN_PROXY_0`, and
`DATAPROTECTION_CERTIFICATE_PASSWORD` in the deployment environment. The Compose file also expects the external
`motorsports_net` network and a read-only RSA PKCS#12 certificate at
`/etc/dalion/motorsports/secrets/dataprotection.pfx`. Protect and back up that certificate separately from the database;
the app requires it outside Development to decrypt its Data Protection keys.

### Data Protection certificate setup

This certificate encrypts the Data Protection key ring; Do not generate a replacement when updating the app: existing keys must remain decryptable with the same certificate.

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

OpenSSL prompts for the PFX password; set it as `DATAPROTECTION_CERTIFICATE_PASSWORD` in the deployment environment.

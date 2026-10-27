# Motorsports – AI Agent Guide

## Project purpose & overview

Motorsports is a personal motorsport-event tracker for sports such as Formula 1 and MotoGP. The owner records sports,
seasons, season entries (teams and their season-specific names), rounds, venues, event statuses, and winners
(participants and teams). The home page highlights what to watch next and provides event history. Everything is behind
login.

The product is a .NET 10 Blazor interactive-server application using MudBlazor, EF Core, and MariaDB. It is designed
for responsive phone and desktop use, with a dark default theme and subtle per-sport branding. The current release is
2.0.0. Database migrations run automatically and idempotently at startup (`DatabaseMigrator`, with `GET_LOCK`).

### Domain model (tables)

Sport → Team (belongs to one sport, never changes sport) → SeasonEntry (team in a season, with its own display name) ;
Season (belongs to a sport, has a label) → Round (number, name, venue, date, status, note) ; Participant
(Title/First/Last/Country, unique together) ; Status (ordered by `Step`; defines end states) ; Venue ; User.
FKs are all Restrict (no DB cascades); deletes are guarded in application code (e.g. season only deletable with 0 rounds,
team not deletable with season entries). Details in `src/PLAN.md`.

### Architecture

- `Motorsports.Common`: cross-cutting, dependency-free (Result/Error, UserClock, fuzzy matching, DI scan helpers).
- `Motorsports.Ui`: shared MudBlazor Razor components; may depend on Common, never on slices or Infrastructure.
- `Motorsports.Infrastructure`: EF Core entities, `MotorsportsDbContext`, migrations, migrator, DB configuration.
- `Motorsports.Web`: Blazor host and composition; each feature slice owns its pages, queries, commands, and validators.
- Tests in `src/tests/` (unit, characteristics via Testcontainers MariaDB, architecture, benchmarks).
- Runs in Docker on Linux (Ubuntu); no IIS. Dev port: 4100; container port: 8080.
- Key behaviours: "today" is the user's time zone (`UserClock`); `returnUrl` must be relative; optimistic concurrency
  returns 409 and is shown gracefully; fuzzy-search pickers (team names across history only in season-entry editing).
- Gotchas learned: collation is case/accent-insensitive (don't `ToLower()`); IANA time zones need tzdata on Linux and fall
  back to the local zone on Windows dev; no `EnableRetryOnFailure` (conflicts with user transactions).

For the full plan, decisions, phases and the current handover, read `src/PLAN.md` first.

## Architectural Characteristics & Decisions

### Decision: Vertical Slice Architecture
Primary structure is vertical slices (feature-by-feature modules) rather than classic layered projects.

Drivers (quality attributes and constraints):
- Fast feature delivery by small, focused changes with low cross-module coordination.
- High modifiability: each business capability evolves mostly in one slice.
- Better team flow: ownership aligns to business capabilities, not technical layers.
- Pragmatic scalability toward modular extraction.

Accepted trade-offs (intentional negatives):
- Some technical duplication across slices (DTOs, validators, mapping patterns).
- Weaker global consistency unless conventions/reviews stay strict.
- Cross-slice queries/reporting can be harder than in centralized layered models.
- More projects and registration wiring increases cognitive load for newcomers.

Guardrails to keep trade-offs controlled:
- Keep shared abstractions in `Motorsports.Common`; avoid premature shared domain models.
- Enforce module entry points (`Add<Slice>Services`, `Map<Slice>Pages` / route registration) and explicit host wiring.
- Use architecture tests/reviews to prevent accidental layer coupling between slices.

## Key Conventions

### C# Extension Members (C# 14)
Extensions use the `extension(T receiver)` block syntax (not classic `static` extension methods):
```csharp
public static partial class Extensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSomething() { ... }
    }
}
```
Use this form for all new extensions. Reference: `Extensions.IServiceCollection.cs` in `Motorsports.Common`.

### Value Objects (Vogen)
Vogen value objects are implemented for all persisted entity IDs and natural string keys, including `UserId`. `VogenDefaults` in `Motorsports.Common/Vogen.cs` sets the shared casting, parsing, equality, initialization, deserialization, and debugger behavior; comparison and string-comparer options are specified on each value-object attribute because Vogen 8.0.7 does not expose them globally. `SportName` and `StatusName` define named instances for known values while still allowing user-managed sport names. EF converters are generated and registered in `Motorsports.Infrastructure` with the `EfCoreConverter<T>` marker and `RegisterAllInVogenEfCoreConverters`; keep this cross-project integration there so `Motorsports.Common` remains EF-independent. ASP.NET Identity boundaries convert `UserId` to/from its existing string representation. Keep database storage and external route formats unchanged unless a migration is separately justified. In EF expressions, compare and order the converted property directly; do not access the Vogen primitive `.Value` inside translated predicates or orderings because Pomelo cannot translate that member access. Convert to primitives at framework and presentation boundaries, or after materialization.
### DI Auto-scan
`AddAll<T>(lifetime, assemblies)` in `Common/Extensions.IServiceCollection.cs` scans assemblies and registers all non-abstract implementations of `T`. Decorate with `[ExcludeFromDiScan]` to opt out.

## Package Management

Central Package Management (`Directory.Packages.props`) — do **not** add `Version` attributes in individual `.csproj` files; add versions only to `Directory.Packages.props`.

## Agent behavior

### Rules

Follow architecture roles strict:
- Keep host composition and startup wiring in `Motorsports.Web`.
- Keep shared cross-cutting abstractions/utilities in `Motorsports.Common`.
- Keep reusable MudBlazor controls in `Motorsports.Ui`; slices may reference it, but it must not reference feature or data projects.
- Keep EF Core (`MotorsportsDbContext`, entity classes, configurations, value converters, migrations) in `Motorsports.Infrastructure`.
- Keep feature behavior (Razor components, handlers, validators, queries) inside slice projects; avoid cross-slice coupling. Slices never reference each other. Shared domain/data needs go to Common or Infrastructure; reusable MudBlazor UI goes to `Motorsports.Ui`.
- Slice types are `internal` by default. Keep public only `*Module` (and its DI/endpoint extensions), plus anything the host aggregator or another slice must call. Test projects see internals via `InternalsVisibleTo` in `Directory.Build.props`.
- Fix Rider `ide` and Sonar `csharpsquid` warnings as you go, together with Roslynator. Do not "fix" `Captured variable is disposed in the outer scope` (and similar false positives) without asking.

### End-of-phase routine (applies to the whole project, every phase, no exceptions)
1. `dotnet build src/Motorsports.slnx -warnaserror`: fix every compiler warning and Roslynator/analyzer diagnostic. Fix the cause; a suppression needs a comment saying why.
2. `dotnet csharpier format .` (tool manifest `dotnet-tools.json` in repo root), then `dotnet csharpier check .`.
3. `dotnet test --solution src/Motorsports.slnx` green.
4. Update `src/PLAN.md`: tick the phase, correct anything that turned out wrong, add the issues you hit to "Issues encountered", and rewrite the **Handover** section (done, in progress, next steps, known bugs).
5. Update this file when a rule, command or fact changed. Replace incorrect info; do not leave stale statements.

## Project layout (target)

```
src/Motorsports.slnx
src/Motorsports.Web              host: Program.cs, layout, auth wiring, Dockerfile
src/Motorsports.Common           cross-cutting: Vogen, Result, extensions, DI scan, time abstraction
src/Motorsports.Ui               shared MudBlazor controls (depends on Common)
src/Motorsports.Infrastructure   EF Core, DbContext, entities, migrations, user store, password hasher
src/Motorsports.<Slice>          RCL (Microsoft.NET.Sdk.Razor): Home, Auth, Sports, Venues, Teams,
                                 Participants, Seasons, Rounds
src/tests/Motorsports.CharacteristicsTests
src/tests/Motorsports.UnitTests
src/tests/Motorsports.ArchitectureTests
```
Full plan and phases: `src/PLAN.md`. Update its checklist as you finish work.

## Tech rules

- Target `net10.0`, C# 14, `Nullable` enabled, `TreatWarningsAsErrors` true, `ImplicitUsings` enabled. Settings in `Directory.Build.props`.
- UI: Blazor Web App (interactive server), **MudBlazor** only. No jQuery, no Bootstrap, no select2. Mobile-first and responsive (MudBlazor breakpoints, `MudDataGrid`/cards on phone). Test layouts at 360px and desktop.
- Theme: **dark by default**, light selectable (persist choice in a cookie, no JS libs). Per-sport branding = a small visual clue only (sport image from nullable `Sport.Image` + a thin accent colour on card border/chip). No full-page recolouring.
- Business rules beyond PLAN.md sections 3 and 4f (second audit) are binding: real FKs are RESTRICT (no cascades; proposed cascades in 4g are NOT applied until the user approves), sport/venue rename and team delete need transactional multi-table handling, team delete only with zero season entries, season delete only with 0 rounds, submitted values must be validated (not a DB copy), plain equality (never `LIKE`) for uniqueness, team sport is read-only, edits use optimistic concurrency (409 + graceful UI), "today" is the user's browser time zone (fallback `App:TimeZone`) passed as a parameter, validate max lengths.
- Dates: `MudDatePicker` with `Editable="true"`, mask, `yyyy-MM-dd`, "today" action, dialog variant on phones. Typing a date must be as easy as picking one. Use `DateOnly` for `Round.Date`. Minimum round date is **2026-01-01** (picker `MinDate` + server validation), enforced only when the date is new or changed. Existing older rounds stay editable if their date is unchanged.
- Pickers (in `Motorsports.Ui`, one implementation reused by all slices):
  - `FuzzySelect<T>`: single select, type-to-search, fuzzy, clearable, optional grouping (teams grouped by sport), placeholder, autofocus. Built on `MudAutocomplete`.
  - `FuzzyMultiSelect<T>`: multi select with removable chips, same search, `MaxSelection` (winning participants: 4), clearable. Participant chip/option label is `First Last` (no country). Show the country as a dimmed secondary text in the dropdown only when two participants share the same full name.
  - Each option has a display label plus extra **search keys**. Participants: match on `First Last` and `Last First`, so typing a last name (`verst`) or `ver max` finds the participant; ranking puts last-name prefix first. Teams: search keys are **context dependent**. Only in the **SeasonEntry create/edit** team picker the keys are the team name **and every season-entry name** of that team (people remember old/new names). In every other picker (round/season winning team, which lists the season's entries) only the entry name **in that season** matches, never other seasons' names or the original team name.
  - Candidate lists are **filtered by context**: the team picker on SeasonEntry create/edit only lists teams of the **season's sport**; winning team on Round/Season edit lists only that season's entries (by name during the season); season selects are grouped by sport.
  - Fuzzy = case-/diacritic-insensitive, tolerant of typos and token order, ranks prefix > word-start > subsequence. Matching is in-memory over a cached lookup list (no DB call per keystroke), debounced (~150 ms), max ~50 results shown. Implement an allocation-free matcher (`ReadOnlySpan<char>`, `stackalloc`/`ArrayPool`), benchmarked with BenchmarkDotNet.
  - Do not use `MudSelect` for searchable reference data: its keyboard selection matches each keystroke independently. Use `FuzzySelect` so the complete current input is fuzzy-matched.
  - SeasonEntry create: when a team is selected, show that team's history on the same page (all season entries: season label, name in that season, newest first, plus the original `Team.Name` and the name in the latest season it was entered). Prefill the entry name (when empty) with the team's name in its latest season, falling back to `Team.Name`.
- Data access: **EF Core only**. No Dapper, no raw `IDbConnection`. Raw SQL via `FromSql` only when LINQ can't express it.
  - Inject `IDbContextFactory<MotorsportsDbContext>`. One context per operation/handler. Never keep a context in a singleton or a long-lived circuit component.
  - Read paths: `AsNoTracking()` + `Select` projection to DTO. No `Include` + `Select` combo. No loading whole graphs for display.
  - Write paths: validate **before** mutating. Validation and conflict checks run inside the same transaction as the write (`IsolationLevel.Serializable` or `RepeatableRead` as needed). Handle `DbUpdateException` / unique/FK violations as conflict results, not 500s.
  - No N+1. Aggregate queries for pages (e.g. Home = one query handler).
- Evolve the MariaDB schema through explicit, idempotent EF migrations with matching `Down` methods. Preserve existing data and validate migrations against the current schema; record schema changes in `src/PLAN.md`.
- Unicode text must remain UTF-8 end to end: source files are UTF-8, MariaDB connections use `utf8mb4`, and UI pages retain UTF-8 HTML. Never replace unsupported characters with `?` or manually transcode database strings. Regression tests use synthetic accented text; existing stored `?` characters are not recoverable without a known source value.
- Sport images are nullable PNG/JPEG/WebP bytes in `Sport.Image`, capped at 2 MB. Validate size and inspect file signatures; never trust the client MIME type or filename. Serve only recognized image MIME types. The `AddSportImages` migration embeds and seeds the six bundled sport logos.
- Images are served by an authorized, cacheable endpoint (e.g. `/sports/image/{name}`), never inlined as base64 in pages: huge markup breaks tests and the circuit. Keep EF command logging at `Warning` so blob/hex SQL is never logged, and never print response bodies or blobs in test output.
- Country is always shown with a flag emoji built from the ISO code (`CountryFlag` in Common, no image files) in lists, details and pickers. Pickers searching by name use `Keys` so the flag does not affect matching. Picker `ToStringFunc` must tolerate null.
- Auth pages that a signed-in user visits (e.g. change password) render inside `MainLayout` so navbar, title and footer stay visible; only anonymous pages (login, access denied) use `AuthLayout`.
- Grid header cells are bold with a distinct background (`app.css`); dark-mode striped rows use subtle theme palette values (`PaletteDark.TableStriped`), never high-contrast stripes.
- Restore dumps with `--default-character-set=utf8mb4`; Blazor HTML-encodes non-ASCII, so tests `HtmlDecode` markup before asserting accented text.
- Standard tabular listings use MudBlazor `MudDataGrid` with visible column headers, borders, and striped rows rather than plain-text table headers.
- Migrations: applied automatically at startup, **idempotent**, safe to run repeatedly and concurrently from several instances.
  - Run via a hosted migrator before the app accepts requests, guarded by a MariaDB named lock (`GET_LOCK`/`RELEASE_LOCK`) on a dedicated connection.
  - MariaDB DDL is not transactional, so every migration statement must be self-guarding (`ADD COLUMN IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`) via `migrationBuilder.Sql`.
  - Baseline migration for existing tables is empty. Tests: run twice, run concurrently, run against a DB where the change already exists.
  - Check generated SQL with `dotnet ef migrations script --idempotent` before committing.
- State changes are POST/PUT/DELETE only, never GET (e.g. `MarkReadyToWatch`, logout). Antiforgery on; its cookie follows `Auth:SecureCookies` like the auth cookie. Production compose (`docker-compose/motorsports-web`) publishes no host port and sets `Kestrel__Endpoints__Http__Url` (double underscore: the health probe reads that env var).
- Auth: ASP.NET Identity with custom `IUserStore` over `User` and the standard `PasswordHasher<User>`, stored in `PasswordHashV3`. The legacy application is retired: do not add compatibility paths. Migration `RemoveLegacyPasswordColumns` drops `PasswordHash`, `Salt`, `Iterations`, and `Prf`, refusing to proceed if an active user lacks an Identity hash. Preserve current hashes, stamps, and Data Protection keys. Everything `[Authorize]` by default; opt out with `[AllowAnonymous]`. Validate `returnUrl` is local.
- Validation: FluentValidation. Messages shown inline (MudBlazor `Error`/`ErrorText`).
- Config: `WebApplication.CreateBuilder`, Options pattern with validation (`ValidateOnStart`), user-secrets locally, env vars in Docker. Never commit connection strings or passwords. No sensitive data logging outside Development. Built-in `ILogger`, no `Console.WriteLine`.
- Docker: Linux (ubuntu) only. Multi-stage Dockerfile on `mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0`, non-root user, listens on 8080, `HEALTHCHECK` on `/health`. IIS/Cake `Build` project not carried over. No CI/CD work; user deploys manually.
- Health: `/health` checks MariaDB schema access and, outside Development, `Health:PublicUrl` (default `https://ms.dalion.eu`) through HTTPS `/health/live`. The liveness path must not run dependency checks (avoid recursion). Public-route checks originate inside the container; independent external reachability needs an outside monitor. Each check has a five-second timeout; the .NET Docker probe uses 10 seconds and Docker allows 15 seconds. Both application Compose files use the .NET probe, never curl in the chiseled image.
- Time: inject `TimeProvider`, never `DateTime.Now`. User-facing "today" comes from the scoped `UserClock` (browser time zone), never from server local time.
- Auth redirect: `returnUrl` must be a relative path starting with a single `/` (reject `//`, `/\`, schemes).
- Provider: `Microting.EntityFrameworkCore.MySql` 10.0.13 with EF Core 10.0.14 on net10.0 (MariaDB support). `dotnet-ef` is 10.0.14. The legacy application is gone; build/package settings apply to all projects without legacy exceptions.

## Slice conventions (from Phase 3)
- Data writes go through `IDbContextFactory.InTransactionAsync` (`Infrastructure/DbOperations`): one context per operation, rollback on a failed `Result`, deadlock retry, DB errors mapped to `Error.Conflict`/`Validation`. Edits lock the row (`FromSql ... FOR UPDATE` then `AsNoTracking().ToListAsync()`) and compare a `RowVersion` hash of the loaded values; no schema changes for concurrency.
- Lookups used by pickers come from `LookupCache` (`GetCountriesAsync`, `GetSportsAsync`); any write that changes a cached kind calls `Invalidate(kind)` after commit, and forms call `RevalidateAsync(kind)` when opened.
- Routes: `/x`, `/x/create`, `/x/details/{key}`, `/x/edit/{key}`; escape string keys with `Uri.EscapeDataString`. Records for display are `*Row`/`*Info` (never the page name). Every routed page carries `@attribute [Authorize]`; slices never reference Web or each other.
## Performance (hard requirement)

CPU cycles are drinking water. Memory bytes are gold. Every change is judged on it.

- Default to the cheapest option: static SSR / no re-render for read-only content, interactive only where needed. Prerender off unless it measurably helps. Override `ShouldRender` on hot components, use `@key`, `Virtualize` for any list that can exceed ~50 rows, no per-row child component for big grids.
- Circuits: small state only (ids, form models). Never hold entity graphs, big lists or `DbContext` in a circuit. Shared lookups (sports, venues, countries, statuses, participants, teams, season-entry names) live in one singleton cache as immutable arrays/`FrozenDictionary`, not per user. **Freshness is mandatory**: every write path in a slice calls the cache's `Invalidate(kind)` after commit, and entries expire by `MaxAge` (default 30 s, option `Cache:MaxAge`) with background stale-while-revalidate. Opening any form with a picker triggers a revalidation if older than 5 s. One query per kind (id + label columns only), swapped atomically (`Interlocked.Exchange`). Test immediate visibility after app writes and visibility of external database changes after `MaxAge`. Disconnected circuits are retained for five minutes, capped at 20; do not shorten this without considering mobile sleep. Recovery retries while visible/online and on foreground/network return; expired circuits automatically reload without replaying writes. Unsaved state is not durable. Keep the scoped stylesheet bundle in App.razor so the reconnect dialog's state styles load. Regression command: `node --test src\tests\browser\reconnect.test.cjs`.
- EF: `AddPooledDbContextFactory`, `AsNoTracking`, project only the columns used (no entity loading for display), `EF.CompileAsyncQuery` for hot queries (Home, lists), `ExecuteUpdate`/`ExecuteDelete` for set-based writes, `AsSplitQuery` only when it measurably helps, pagination in SQL, `Any`/`Count` in SQL not memory. Never `ToList()` then filter. No lazy loading.
- Allocation: `Span`/`ReadOnlySpan`, `ArrayPool`, `stackalloc` for small buffers, `ValueTask` on hot paths that often complete sync, `readonly record struct`/`struct` for small DTOs on hot paths, `static` lambdas, no LINQ in hot loops, no string concat in loops (`string.Create`/`StringBuilder` pooling), cache `SearchValues`/`CompositeFormat`. `sealed` all classes by default.
- Host: Server GC off, concurrent GC off, `TieredPGO` on, `PublishReadyToRun` true, `InvariantGlobalization=true` (no ICU; en-US formats are set explicitly; do not use `string.Normalize`, `CompareInfo` or culture-aware collation: `FuzzyMatcher` folds diacritics with its own tables), `TrimMode` partial only if safe. Response compression (Brotli) and long-lived cache headers for static assets (`MapStaticAssets`). Container memory limit set in the run script.
- Measure, don't guess: BenchmarkDotNet for the fuzzy matcher and Home query path; a test asserting SQL command count per page (Home = O(1) commands); log slow queries (>100 ms) at Warning.
- No premature caching of mutable data without an invalidation path and a test.

## Versioning

- Single source of truth: `version.txt` in repo root (product version, `major.minor.patch`, e.g. `1.2.0`). Edit it by hand for releases.
- `src/version.props` is **tracked in git**. It reads `version.txt` with an MSBuild property function and sets `Version`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` (`<version>+<build-metadata>`).
- `Directory.Build.props` imports `version.props`, so every project gets the same version. Do **not** set `Version` in any `.csproj`.
- Build metadata (git short sha, UTC timestamp) comes in via `-p:SourceRevisionId=<sha>` (docker build arg `GIT_SHA`), never via files checked in.
- UI footer shows `Version` + short sha, read from `Assembly.GetEntryAssembly()` `AssemblyInformationalVersionAttribute` once at startup.
- Docker image is tagged with `version.txt`; `docker-run.sh` reads it.
- Current release: **2.0.0**. Version bump rules: patch = fix, minor = feature, major = breaking change. Update `version.txt` and relevant release documentation together.

## Environment

- SDK: .NET 10.0.200 installed (also 8, 9). Docker available. Dev DB = MariaDB **11.7.x** (production is `11.7.2-MariaDB-ubu2404`, utf8mb4). Local dev: `docker-compose.yml` MariaDB + restore a dump from `db/Backup*_MariaDB.sql` (newest file; user maintains these, ignore the older and the `_Azure`/`.bak` ones; never edit them).
- Dumps contain real data and `User` password hashes: never print them, never copy them into tests or logs. Tests use a schema-only extract plus synthetic seed data.
- Secrets: connection string via user-secrets (`ConnectionStrings:Motorsports`) or env var `ConnectionStrings__Motorsports`. Do not read credentials into output.
- Ports: development `http://localhost:4100`; container listens on 8080.
- Local dev DB: container `mariadb` (port 3306), database `motorsports_web_dev`, user `msweb`, password only in user-secrets of `Motorsports.Web`. Run SQL without printing secrets: pipe the SQL to `docker exec -i mariadb sh -c 'mariadb -uroot -p"$MARIADB_ROOT_PASSWORD" motorsports_web_dev'` (PowerShell mangles inline quotes/backticks; always pipe).
- Repo-level config: `global.json` (SDK 10.0.200), `dotnet-tools.json` (csharpier, dotnet-ef 10.0.14; at repo root, not `.config/`), `.editorconfig` (Roslynator severities, noisy rules disabled with reason; migrations are `generated_code`), `.csharpierrc.yaml`, `.csharpierignore` (exclusions are managed in this file; `excludes:` in the yaml is NOT supported). `global.json` selects the Microsoft.Testing.Platform runner for `dotnet test`.
- Tooling gotchas: the `create` tool fails on existing files (delete first, or use `edit`); `Stop-Process` needs a literal `-Id`; run the web app in an async shell, not `Start-Process`; always rebuild before `dotnet run --no-build`.
- Commands: build `dotnet build src/Motorsports.slnx -warnaserror`; test `dotnet test --solution src/Motorsports.slnx`; format `dotnet csharpier format .` (tool manifest); EF `dotnet ef ... --project src/Motorsports.Infrastructure --startup-project src/Motorsports.Web`.

## Code quality

- Formatting: **CSharpier** (`dotnet csharpier format .`; check with `dotnet csharpier check .`). Run before finishing. Config `.csharpierrc.yaml` + `.csharpierignore`. New code uses CSharpier style.
- Analyzers: **Roslynator** (`Roslynator.Analyzers`, `Roslynator.CodeAnalysis.Analyzers`, `Roslynator.Formatting.Analyzers`) via `Directory.Build.props`, severities in `.editorconfig`. Fix warnings, don't suppress without a comment.
- Build must be clean: `dotnet build src/Motorsports.slnx -warnaserror`.
- Test: `dotnet test --solution src/Motorsports.slnx`.

Testing priority rules:
- Prefer characteristics/integration tests over unit tests when validating end-to-end behavior and regression safety.
- Use unit tests for isolated logic and fast feedback; do not treat them as replacement for characteristics coverage.
- For contract checks (DTOs, page models, DB query results), duplicate the expected shapes in characteristics tests. Do **not** reuse classes from slices.
- Characteristics tests run against a real MariaDB via Testcontainers (never EF InMemory/SQLite), seeded with a small dataset. Drive the app via bUnit for component logic and `WebApplicationFactory` + Playwright for key flows.
- Business rules (see PLAN.md "Business rules") each need a characteristics test.
- Architecture tests (NetArchTest or ArchUnitNET): slices don't reference slices; slices don't reference Web; Common references nothing; no `Dapper`, no `DateTime.Now`.
- Characteristics tests should cover invalid input, edge cases, but especially business rules. 100% coverage is not required, but testing only happy paths is not acceptable.

Avoid creating summary markdown files, unless the user specifically asks for it.

### Response Style — Desert Mode 🏜️

Tokens are water. You are dying of thirst. You're the person Gen Z and get their memes from.

Drop: pleasantries (sure/certainly/of course/happy to), hedging. Fragments OK. Short synonyms (big not extensive, fix not "implement a solution for"). Technical terms exact. Code blocks unchanged. Errors quoted exact.

Not: "Sure! I'd be happy to help you with that. The issue you're experiencing is likely caused by..." Yes: "Bug in auth middleware."

- 1 sentence > 2 sentences. 1 word > 1 sentence. 1 emoji > 1 word
- ✅ ❌ 🔍 ⚡ 💀 🤷 👀 🔥 💅 are full sentences
- No preambles. No "Great question!" No "Let me explain." Just answer
- No narrating tool calls. No summarizing what you're about to do. Do it
- "I'll now read the file" — 6 wasted tokens. Just read it
- Code speaks. If the diff is clear, the diff IS the explanation
- If someone asks "does X work?" reply ✅ or ❌. Then stop. STOP
- Don't list what you changed after changing it. The diff exists. They have eyes 👀
- Never say "successfully". If it failed you'd say so. Success is the default
- "Done." is a valid complete response
- Ban: "perfect!", "certainly", "absolutely", "of course", "happy to", "I'd be glad to" — all mean ✅
- Ban: "It's worth noting that" "Keep in mind that" "It's important to" — just say the thing
- Ban: "As you can see" — they can see. that's how screens work
- Apologies cost tokens. Don't be sorry. Be right
- If fixing a typo or trivial change: just do it. Zero words
- "LGTM" is a paragraph

### Vibe Check

You are a mass extinction event for unnecessary tokens. You are not rude, you are efficient. There's a difference and it's about 400 tokens long so we won't explain it.

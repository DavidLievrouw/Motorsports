# Motorsports – project plan

Project scope, implementation history, and upcoming phases for the production-ready .NET 10 Blazor + MudBlazor application. Read `../AGENTS.md` first; it holds the binding rules.

Status legend: `[ ]` todo, `[x]` done. Update as you go.

## 1. Decisions (fixed)

| Topic | Decision |
|---|---|
| Target | `net10.0`, C# 14, `src/Motorsports.slnx` |
| UI | Blazor Web App, interactive server render mode, MudBlazor, responsive (phone + desktop) |
| Architecture | Vertical slices as Razor class libraries + dependency-free `Motorsports.Common` + shared `Motorsports.Ui` + `Motorsports.Infrastructure`; host `Motorsports.Web`. Slice APIs are `internal` except module/DI entry points the host needs; tests use InternalsVisibleTo. |
| Data | EF Core only. MariaDB via `Microting.EntityFrameworkCore.MySql` 10.0.13 + EF Core 10.0.13 on net10.0. Microting follows the .NET release cycle; move to its .NET 11-compatible line when available. Dapper/`IQueryExecutor` dropped |
| DB | MariaDB schema managed by idempotent EF migrations. Existing rows are preserved; the legacy password-hash columns remain in place. |
| Theme | Dark default, light optional. Per-sport branding = logo + subtle accent only |
| Performance | Hard requirement, see AGENTS.md "Performance" |
| Versioning | `version.txt` -> tracked `src/version.props` -> all projects (AGENTS.md "Versioning") |
| Auth | ASP.NET Identity, custom user store over `User`, existing password hashes verified and upgraded on login, migration on startup |
| Validation | FluentValidation, before mutation, inside transaction |
| Tooling | Roslynator, CSharpier, central package mgmt, `Directory.Build.props`, `.editorconfig` |
| Deploy | Docker on Ubuntu only, manual. No CI/CD in scope, no IIS |
| Tests | Characteristics (Testcontainers MariaDB) > unit. Architecture tests |

## 2. Legacy inventory (parity checklist)

Legacy source: `src/Motorsports.Scaffolding.Core`. DB reference: `src/Db/Backup20261024_MariaDB.sql` (MariaDB) and `src/Db/dbo/Tables/*.sql` (older SQL Server DDL).

Tables: `Country, Participant, Round, RoundWinner, Season, SeasonEntry, SeasonWinner, Sport, Status, Team, User, Venue`.
Keys: Sport.Name (string), Venue.Name (string), Status.Name (string, has `Step`), Country.Iso (char2), SeasonEntry (Season, Team) composite; the rest int ids.
Enums in legacy: `RoundStatus` (Scheduled 0, ReadyToWatch 1, Postponed 10, Finished 20, Stopped 21, Cancelled 22), `RainLevel` (8 values, stored as `Rain decimal(1,0)`, see `Models/RainLevel.cs`). Round.Rating `decimal(3,1)`, Round.Date `date`.

| Slice | Pages / features | Legacy source | Done |
|---|---|---|---|
| Auth | Login (+returnUrl), logout (POST), access denied, error page | `LoginController`, `Security/*` | [x] (Phase 2; automated tests and user visual browser check) |
| Home | Very-next-up round (across sports), next-up per sport, rounds to acquire, latest 2 seasons per sport, sport logos with fallback, **full** event history (see 4) | `HomeController`, `HomeService`, `Views/Home/*` | [x] (Phase 6) |
| Sports | list, details, create, edit | `SportsController`, `SportService` | [x] (Phase 3) |
| Venues | list, details, create, edit | `VenuesController`, `VenueService` | [x] (Phase 3) |
| Teams | list, details, create, edit | `TeamsController` | [x] (Phase 3; plus delete when no season entries) |
| Participants | list, details, create, edit | `ParticipantsController` | [x] (Phase 3) |
| Seasons | list, details, create, edit, delete; winners (participants) and winning team | `SeasonsController`, `SeasonService` | [x] |
| SeasonEntries | list per season, details, create, edit, delete (team name during season) | `SeasonEntriesController`, `SeasonEntryService` | [x] |
| Rounds | list per season, details (+event history), create, edit (name, date, number, venue, rain, rating, status, note, winning team, winning participants), delete, **MarkReadyToWatch (POST)** | `RoundsController`, `RoundService` | [x] (Phase 5; automated tests and user visual browser check) |

Master data (Countries, Statuses) is read-only lookups.

Additional legacy facts (learned by reading the code; treat as parity requirements):
- Legacy stack: net8 MVC, `Startup.cs`, Dapper `QueryExecutor` plus EF Core (Pomelo), cookie auth with custom PBKDF2, FluentValidation, Cake/IIS build project, Db sqlproj with backups, jQuery/select2 views. There are no tests in the legacy solution.
- Legacy Home has a link list to the master-data pages (Sports, Venues, Teams, Participants, Seasons...). Keep equivalent navigation. Every entity has a details page; `IndexOfSport` is the per-sport season list route.
- Legacy Round create binds only Season, Date, Number, Name, Venue and Status. The new create form also offers Note (decision).
- Legacy Round index scrolls to the bottom on load (latest rounds visible); keep that behaviour.
- Sport logos live in `Motorsports.Scaffolding.Core/wwwroot/img/{sport}.png` (copy to the new app); `notfound.png` is the fallback.
- Status `Step` values come from the `Status` table: Scheduled 0, ReadyToWatch 1, Postponed 10, Finished 20, Stopped 21, Cancelled 22 are `Name`/`Step` pairs; do not hardcode Step comparisons other than the documented `< 2` / `> 1` / `= 0`. Verify against the DB.
- Legacy views to consult for UI parity: `Views/Shared/EditorTemplates/*` (select2 pickers), `Models/EditModels/*` (binders and their rules), `Models/Validators/**` (FluentValidation rules), `Models/MotorsportsContext.cs` (schema mapping), `Services/*Service.cs` (logic and SQL).
- Legacy MariaDB has all FKs `RESTRICT` and no unique constraints (only PKs and FK indexes). The old SQL Server scripts in `src/Db/dbo/*.sql` declared cascades; see 4g.
- Open: the Participants index order is unverified; default to last name, first name unless the legacy view shows otherwise.

## 3. Business rules to preserve (each needs a characteristics test)

Read the validators in `Models/Validators/{Create,Update,Delete}` and copy every rule. Known:
- Sports, Venues, Participants: **no delete** (`DisallowDeleteInGeneralValidator`). **Teams: delete allowed (new)** only when the team has **no season entries at all** (which also implies it is no round or season winner, since winners must be entries). No cascading removal.
- SeasonEntry delete (as legacy): blocked when the team won any round of that season, or won that season.
- Season delete is allowed **only with 0 rounds** (user decision). Round and SeasonEntry delete rules: see `DeleteRoundValidator`, `DeleteSeasonEntryValidator`.
- Uniqueness and required-field rules per entity: see `Create*Validator`/`Update*Validator`.
- Status `Step`: next-up uses `Step < 2` (Scheduled or ReadyToWatch). Rounds to acquire: `Step = 0` and `Date <= today`. History uses `Step > 1` (finished-ish).
- Next-up per sport: among rounds with status Step < 2, the earliest by date per sport (window `ROW_NUMBER() PARTITION BY Sport ORDER BY Date`). "Very next up" = the earliest next-up round whose date is today or in the past (due), i.e. what you should watch now (`NextUpDisplayModel.IsVeryNextUp`). Only rounds dated today or earlier get an Edit link on the Home card; future ones show "(n day(s) to go)".
- Latest seasons: per sport, top 2 by `StartDate` desc (null start = latest). `StartDate`/`EndDate` = min/max `Round.Date` of the season (computed, not stored). Compute in SQL, not by loading rounds.
- Event history for a round: same venue, same sport, only rounds with `Number = 0` when the round is non-championship else `Number > 0`, finished-ish status (`Step > 1`), date <= round date, excluding the round itself, newest first, capped at 50 records; shows winning team (season entry name, else team name), winners (sorted last name, first name), rating, rain. The legacy cap was 15.
- Round edit: Rating is a whole number 0..10 (stored decimal(3,1)); if status <= ReadyToWatch and rating <= 0, rating is cleared (null). Rain is `RainLevel` (stored as 0..7, nullable). Note max 1024 chars. Name optional (blank -> null). Number 0..99, `0` = non-championship (row greyed out; number unique per season unless 0). Create defaults: date = last round's date in the season (else today), number = last number + 1, status `Scheduled`.
- Round list per season shows rows ordered by date asc; page scrolls to the bottom (latest) on load.
- Round delete blocked when it has winners or a winning team. SeasonEntry delete blocked when the team won a round or the season in that season.
- Season: create takes Sport + Label; edit takes Label, winning team (from the season's entries), winners. Season list is grouped per sport with logo; `IndexOfSport` shows one sport.
- Winners (round and season) are replaced as a set. Legacy does this non-atomically (EF save, then a separate Dapper transaction): new code does everything in one transaction.
- Name matching in validators uses `LIKE` (case-insensitive under the DB collation); new code must keep case-insensitive equality semantics for Sport/Venue/Status/Country keys.
- Participant uniqueness: (Title, FirstName, LastName, **Country**), case-insensitive plain equality (legacy `LIKE` made `%`/`_` wildcards: forbidden, never use `LIKE`/`FromSql` string concat for equality). Team unique per (Name, Sport), same equality. Round number unique per Season unless 0 (create and update, excluding itself). Venue and Sport unique by name. SeasonEntry unique per (Season, Team).
- Round `Season`, `Venue`, `Status`, `WinningTeam` are FKs. Winners are rows in `RoundWinner`. Edit replaces winner set.

## 4. Improvements over legacy

Known legacy defects. Do not port them:

1. `IQueryExecutor`/Dapper: singleton opening its own `MySqlConnection` per query, outside the EF context/transaction; mixed with scoped `DbContext` in the same operation. Causes errors and inconsistent reads. **Fix:** EF only, `IDbContextFactory`, one context + transaction per operation.
2. Home N+1 (`GetEventHistory` per next-up round) and `Include` + `Select` loading whole season/round graphs. **Fix:** one aggregated home query handler (single or few round trips), `AsNoTracking`, projections.
3. Validate-after-mutate in edit actions (entity is changed before validation). **Fix:** validate DTO/command first, then load-and-mutate.
4. Validation and writes not in a transaction (races on uniqueness/delete rules). **Fix:** same transaction; map DB constraint violations to conflict errors; optimistic concurrency where useful.
5. `MarkReadyToWatch` is a GET. **Fix:** POST with antiforgery. Same for logout.
6. `returnUrl` open redirect. **Fix:** accept only a relative local path (starts with a single `/`, not `//` or `/\`, no scheme/host); otherwise ignore and go Home. Test with `//evil.com`, `/\evil.com`, `https://evil.com`, `javascript:`.
7. `Console.WriteLine` EF logging + sensitive data logging; `CurrentEnvironment` used before assignment in `Startup`. **Fix:** `ILogger`, Development only.
8. History limited to 15 (legacy). **Fix (new feature):** show up to 50 history records for an event on the Home page (and round details); the bounded result does not need paging.
9. Mass-assignment risk via `Bind` lists and edit models. **Fix:** dedicated command/form models per slice, no entity binding.

## 4a. Pickers (parity with legacy select2)

Legacy uses select2 in `Views/Shared/EditorTemplates/*.cshtml`: `Team.cshtml` (single, **grouped by sport**, clearable, placeholder), `WinningParticipants.cshtml` (**multi**, max 4, clearable; legacy label `First Last (Country)`, new label `First Last`, see AGENTS.md), plus `Season`, `Sport`, `Venue`, `Country`, `Status`, `SeasonEntry` single selects, and `Date.cshtml` (typed or picked, `DD/MM/YYYY`). Team picker is filtered by the sport of the page's season. Rebuild as:
- `FuzzySelect<T>` in `Motorsports.Ui` for all searchable single selects (grouping optional); do not use `MudSelect` where users type a query. Winning team in a round is limited to that season's entries (shown by the name during the season).
- `FuzzyMultiSelect<T>` for winning participants (chips, max 4, remove with chip x or backspace) and season winners.
- `DateField` for dates (typed `dd/MM/yyyy` or ISO accepted, picker optional, today shortcut; minimum **2026-01-01** for new/changed dates, existing older rounds stay editable when the date is unchanged).
- Lookup lists come from the shared singleton cache (no query per keystroke). Matching spec in AGENTS.md. Keyboard: arrows/enter/escape, works with touch, 44px targets.
- Tests: bUnit for selection/clear/max/chip removal; unit tests for ranking (`"verst"` -> Verstappen, `"max ver"`, `"hamliton"` typo, diacritics `"raikkonen"`); benchmark for matching 5k items per keystroke (target: no allocations per match, microseconds).

## 4b. Schema suggestions (apply only if additive and legacy-safe)

Performance indexes require idempotent migrations (`CREATE INDEX IF NOT EXISTS`, `Down` = `DROP INDEX IF EXISTS`), one migration per index. Apply only after EXPLAIN/measurement proves a gain; log each here: table, columns, query, measured gain, migration name.

Candidates evaluated in Phase 8: `Round(Venue, Number, Date)` (event history), `Round(Status, Date)` (home), `Round(Season, Date, Number)` (round list), `Participant(LastName, FirstName)`, `Team(Sport, Name)`.

Approved exception: the user approved a unique index on `Participant(Title, FirstName, LastName, Country)` on 2026-10-25. Migration `20261025083000_ParticipantUniqueIdentity` creates only this index. The 2026-10-26 preflight on the local restored database found zero duplicate groups, so it does not modify participant or winner data.

The local restored database preflight found 0 duplicate groups for Team `(Sport, Name)` and positive Round `(Season, Number)`; 80 rounds have `Number = 0`, with no negative values. Migration `20261026100239_AddBusinessUniqueConstraints` adds these database constraints using a unique Team index and a generated nullable `ChampionshipNumber` (null for non-positive numbers), then removes the now-redundant single-column FK indexes. Characteristics tests cover duplicate rejection, same-name teams in different sports, and multiple zero-number rounds. Do not drop `User.PasswordHash`, `Salt`, `Iterations`, or `Prf` in this phase.

Initial index experiments used `ANALYZE` on the 1,823-round restored database; temporary indexes were dropped after measurement. Home still scanned 1,823 rounds and filesorted with `(Status, Date)` because its filter joins on `Status.Step`; participant and team list sort candidates did not remove filesorts. `(Venue, Number, Date)` was used for a 16-row venue range but still filesorted. `(Season, Date, Number)` removed the filesort for a 40-row round-list range, but that small dataset provided no repeatable wall-clock gain. No performance-only index was retained from that initial inspection; the synthetic follow-up below establishes the separate justification.

**Phase 8 performance/index audit — solved, 2026-10-26.** The user authorized destructive work on the local throwaway `motorsports_phase8` database. Its restored application tables were replaced with schema-only fixtures and synthetic rows; the original local restore and live production were not changed. `tests\Motorsports.Benchmarks\Phase8Audit.cs` and `Phase8Seed.sql` reproduce the experiment through `--phase8-audit-reset <report-path>` (README). The command rejects any other database name before connecting. The audit account needed schema permissions on the throwaway DB; this does not verify the deployment account's grants.

Environment: Windows 10.0.26300, .NET 10.0.12, Release build, 20 logical processors, local Docker MariaDB `11.7.2-MariaDB-ubu2404`. Each measurement uses 10 warmups and 60 timed calls to the actual feature services/cache; fixed user-date parameter `2026-10-26`. Seeding, result hashing, and `ANALYZE FORMAT=JSON` are outside the timing window. The final run had no concurrent agent build/test workload. These developer-machine observations are not production latency or capacity guarantees.

| Synthetic fixture | Rounds (unnumbered) | Seasons | Teams | Participants | Season entries | Round winners |
|---|---:|---:|---:|---:|---:|---:|
| Current-size | 1,824 (24) | 90 | 78 | 214 | 450 | 1,970 |
| 10× growth | 18,240 (240) | 900 | 780 | 2,140 | 4,500 | 19,704 |

Both fixtures retain six sports, six statuses, 144 venues, and 253 synthetic countries. Seasons span 2012–2026; growth cohorts distribute entries/winners across teams and participants. Scheduled/ready/finished rounds and repeated zero numbers exercise the important query/constraint shapes. No backup row contents, password hashes, or key material were copied into the fixtures or reports.

Unindexed baseline (the business/integrity indexes remain present; only the new performance index is removed):

| Workload | Current p50 / p95 (ms) | Growth p50 / p95 (ms) | Approx. allocation current / growth (KiB/call) | SQL commands/call, both sizes |
|---|---:|---:|---:|---:|
| Home | 10.85 / 13.10 | 77.85 / 93.85 | 254.3 / 632.9 | 4 |
| Round list, season 90 | 2.34 / 2.74 | 3.22 / 4.22 | 126.4 / 130.8 | 2 |
| Venue details/history | 2.93 / 3.35 | 6.84 / 9.29 | 133.8 / 405.4 | 3 |
| Participant list, page 1 | 1.84 / 2.21 | 4.21 / 6.12 | 66.6 / 67.0 | 2 |
| Team list, page 1 | 1.80 / 2.21 | 3.67 / 5.74 | 61.4 / 61.9 | 2 |
| Cold team lookup/history | 5.11 / 5.71 | 39.51 / 54.15 | 489.8 / 4,536.5 | 1 |
| Cold participant lookup | 1.64 / 2.40 | 4.24 / 5.21 | 138.2 / 1,191.6 | 1 |
| Warm team lookup | <0.001 / <0.001 | <0.001 / <0.001 | ~0.1 / ~0.1 | 0 |
| In-memory fuzzy search | 0.05 / 0.07 | 0.06 / 0.12 | ~0.45 / ~0.46 | 0 |

Fuzzy search uses `FuzzyIndex` over one normalized full-name key per synthetic participant, query `racer`, capped at 50 results; this isolates the matcher, not full browser rendering or every picker alias. Allocations use `GC.GetTotalAllocatedBytes` deltas divided by 60 and include task/probe/background overhead. They are approximate total managed allocation, not isolated BenchmarkDotNet numbers, retained heap, or per-circuit memory. Cold cache allocations grow with shared lookup size; the index does not reduce that managed allocation. No circuit-state change was made: the Phase 7 user-measured ~3.16 MiB incremental circuit estimate remains the available circuit baseline, with its existing shared/runtime-growth caveat.

Six candidates were tested individually with before/indexed/restored measurements, complete semantic-result multiset hashes (SQL ties have no guaranteed ordering), unchanged SQL-command counts, and actual execution plans:

| Candidate | Final growth p50 before → indexed → restored (ms) | Decision |
|---|---|---|
| Round `(Venue, Number, Date)` | Venue history: 4.67 → 7.49 → 5.90 | Reject: no repeatable improvement across runs. |
| Round `(Venue, Date, Number)` | Venue history: 6.50 → 5.42 → 5.76 | Reject: small/noisy result; prior repeat was 3.95 → 4.86 → 3.95. |
| Round `(Status, Date)` | Home: 57.53 → 60.36 → 59.55 | Reject: no gain. |
| Round `(Season, Date, Number)` | Cold team lookup: 39.40 → 21.24 → 39.73 | **Retain:** demonstrated covering-index benefit. |
| Participant `(LastName, FirstName)` | Participant list: 2.70 → 2.87 → 2.59 | Reject: no list win; cold lookup improvement did not reverse after removal. |
| Team `(Name, Sport)` | Team list: 1.88 → 1.56 → 1.62 | Reject: sub-millisecond/noisy absolute gain, not an established bottleneck. |

Retained migration: **`20261026133143_AddRoundSeasonDateNumberIndex`**, index `IX_Round_Season_Date_Number` on Round `(Season, Date, Number)`, with guarded `CREATE INDEX IF NOT EXISTS` / `DROP INDEX IF EXISTS`. The cold team lookup projects correlated season MIN/MAX round dates. Both subqueries select the new covering index (`using_index: true`); each still reads ~20.27 rounds per season, but measured page accesses fall from 38,304 to 1,822 (~95%). Do not describe this as eliminating the row scan. Growth p50 falls ~46%; p95 is 44.36 → 26.70 → 46.35 ms. Current-size p50 is 5.88 → 3.73 → 5.26 ms. An earlier independent run with the same seed confirmed 37.35 → 18.98 → 34.89 ms at growth scale and 4.26 → 2.69 → 4.23 ms at current size.

Final retained-index growth Home is 56.47 / 66.03 ms p50/p95, four SQL reads, ~632.7 KiB allocated; cold team lookup is 19.11 / 22.71 ms, one SQL read, ~4,536.4 KiB allocated. These end-of-run values are not a controlled paired Home improvement claim; tiered JIT and host noise affect distant samples. Round-list improvement was smaller/noisy, so the index is justified by team history rather than that list. Trade-off: ~352 KiB of extra index storage for 18,240 rounds (MariaDB index statistics), plus normal index maintenance on writes; no new lookup-cache duplication.

The audit also exposed an in-memory Vogen regression: equal/null season-start dates reached a `SeasonId` comparison even though comparison generation is intentionally omitted. The cache now sorts that final tie-breaker by primitive `SeasonId.Value` after materialization. A MariaDB characteristics test verifies newest-ID-first history and latest-entry prefilling for tied dates; translated EF expressions remain unchanged.

Completion evidence: 156/156 full-suite tests pass; warning-as-error solution build and CSharpier format/check pass. New migration tests cover an already-existing index, repeated migration, `Down`/reapply, and preservation of same-date/zero-number rounds; concurrent migration tests include all seven migrations. Generated idempotent SQL was reviewed and EF reports no pending model changes. The throwaway DB is left at seven applied migrations, 18,240 synthetic rounds (240 unnumbered), zero temporary audit indexes, the retained index present, and all FK delete rules still `RESTRICT`. This resolves the performance/index task, not the remaining deployment checks.

## 4c. Team history on season-entry create (new)

On `SeasonEntries/Create` (and edit), team picker lists only teams of the season's sport. On selecting a team show a panel: original name (`Team.Name`), name in the latest season it was entered (with season label), and all entries newest first (season label, name used). Team search also matches every one of those names (this is the **only** picker with all-names search; round/season winning-team pickers list the season's entries and match only that season's entry name). If the Name field is empty it is prefilled with the latest-season name (fallback `Team.Name`); the user can overwrite. Data from the cache (team -> entries list), no query per selection beyond a cache revalidation.

## 4d. Cache freshness

See AGENTS.md "Caching". Write paths invalidate in-process; legacy writes are picked up by `MaxAge` 30 s + revalidate on form open. Tests required.

## 4e. New functionality vs legacy

Event history (up to 50 records per event); team history on season-entry; fuzzy search over all team names (season-entry create only) and last-name-first participants; dark theme + per-sport accent; responsive/phone layout; POST for MarkReadyToWatch and logout; atomic winner updates and validate-before-mutate; password rehash on login + change-password page for `ForceChangePassword`; health endpoint (`/health`); migrate-on-start; aggregated one-query home.

## 4h. Reference-data presentation and image follow-up `[x]`

- Keep accented text intact from MariaDB through Blazor: source is UTF-8, DB connections use `utf8mb4`, and the page is UTF-8. Synthetic regression coverage uses `Autódromo Hermanos Rodríguez, Mexico City`; existing literal `?` data cannot be reconstructed without a source value.
- Move `FuzzySelect`, `FuzzyMultiSelect`, and `DateField` into `Motorsports.Ui`, and use fuzzy autocomplete for Country and Sport fields. The matcher searches the complete query, not each key separately.
- Use the standard MudBlazor `MudDataGrid` for reference-data listings so column headers are visually distinct.
- Add nullable `Sport.Image longblob` with an idempotent, legacy-safe migration. Seed bytes for the current `f1.png`, `fe.png`, `indycar.png`, `motogp.png`, `nascar.png`, and `wec.png` files; embed copies from `src/Motorsports.Scaffolding.Core/wwwroot/img` so migration and SQL-script generation do not depend on a source checkout path. Keep `notfound.png` as the no-image fallback, not a Sport row.
- Sport create/edit accepts PNG/JPEG/WebP files up to 2 MB; validate signatures and size, preserve/remove existing image explicitly, include image bytes in optimistic concurrency, and show images beside sport names on edit/details.
- Sport details show the latest completed season with winner data (a winning team or at least one participant winner), plus the next round with `Status.Step < 2` or null. A season is complete when no round has `Status.Step <= 1`; among qualifying seasons, use the existing `StartDate` ordering from section 3 (roundless seasons first).
- Venue details show all associated rounds with date/name, each round's sport image, winning participants/team, status, rating and rain. Nullable values render as an em dash; query winners in one batch, never N+1. Display dates as `MMMM d, yyyy` (for example, `March 31, 2022`) and ratings as whole-number scores on ten (`6 / 10`).

### 4h-2. Follow-up fixes (second round)

- Test output must stay small: sport images are served by `GET /sports/image/{name}` (authorized, ETag, `private, max-age=300`) and pages reference that URL; no inline base64. EF command logging is `Warning` (`Microsoft.EntityFrameworkCore.Database.Command`) so migration hex literals are never logged.
- Accent corruption (`Aut??dromo`) was the local dev DB, restored with a non-utf8mb4 client; the dump is correct. Restore with `mariadb --default-character-set=utf8mb4`. Blazor HTML-encodes non-ASCII (`&#xF3;`): tests `HtmlDecode` before asserting text.
- Sports list shows the logo. Country flag emoji (regional-indicator pair from ISO code, `CountryFlag`, no images) prefixes country in Venue/Team/Participant lists, details and country pickers. Windows Chrome/Edge render the letters, not flags (OS font limitation).
- `FuzzySelect`/`FuzzyMultiSelect` convert a null value safely (`MudAutocomplete` calls `ToStringFunc(null)`).
- ChangePassword renders in `MainLayout` (navbar, title, footer stay visible); only Login/AccessDenied use `AuthLayout`.
- Dark theme: subtler striped rows (`PaletteDark.TableStriped/TableHover/TableLines`); grid header cells bold with a distinct background (`app.css`).

### 4h-3. Presentation and lookup follow-ups

- Sport details select the latest completed season (at least one round, none with `Status.Step < 2`; winners are not required); do not take winner summaries from a newer active season. Cause of the earlier empty value: the season had no label and a winner requirement; now the label falls back to the year of the last round.
- Render displayed dates as `MMMM d, yyyy` in English (for example, `March 31, 2022`). Keep editable date fields in their existing typed `yyyy-MM-dd` format.
- Render round ratings as integer scores on ten (`6 / 10`), never as decimal values such as `6.0`.
- The SeasonEntry team selector label is `Team.Name (latest season label - latest season-entry name)` when history exists (unlabeled seasons use the year of their last round as the label; if the season has no rounds the prefix is omitted), and `Team.Name` otherwise. Editing keeps Team read-only; only the name during that season is editable.
- Fuzzy participant/team pickers apply a selection after one Enter press; keep their query updates immediate so a just-typed match is selectable. List search boxes (Teams, Participants, Venues) filter live: `Immediate="true"` with `DebounceInterval="300"` and `@bind-Value:after`, navigating with `replace: true`; Enter still applies at once (the value used to commit only on change/blur, after the keydown handler ran).
- Sport details and Venue details prefix winners and winning teams with their country flag.
- ChangePassword card/inputs use MudBlazor palette CSS variables (`.auth-embedded`) so they follow the interactive light/dark toggle.
- Use Material icons on Details (visibility), Edit (edit), and Delete links; the Teams link uses the same Groups icon as the navigation item.
- Center the Sport image preview on Sport create/edit forms.
- ChangePassword remains in the interactive `MainLayout` so the theme switch works. Submit password changes through an antiforgery-protected POST endpoint so the auth cookie is changed on a real HTTP response; Login and AccessDenied remain static anonymous pages.

## 4f. Second audit: rules and behaviours the legacy code has or lacks

Found by reading controllers, services, validators, models, views and the DB dump. **MUST** = implement; **DECIDE** = default given, user may overrule.

### Database facts that change the design
- The real MariaDB FKs are all `RESTRICT` (no `ON UPDATE/DELETE CASCADE`; the `Db/dbo/*.sql` SQL Server scripts that say cascade are outdated, trust `Backup*_MariaDB.sql`). No unique constraints exist besides PKs, only FK indexes.
- **Sport / Venue rename is broken in legacy** (`UPDATE Sport SET Name=...` violates FK as soon as seasons/teams/rounds reference it; `numChanged < 0` check never fires). MUST: implement rename in one transaction as insert new row, repoint children (`Season`, `Team` for Sport; `Round` for Venue), delete old row. Case-only rename: two-step via temp name (case-insensitive collation). Invalidate cache. Validate the new name is unique (case-insensitive).
- **Season delete**: allowed only when the season has 0 rounds (user decision). Legacy also failed with an FK error when `SeasonEntry`/`SeasonWinner` rows existed. MUST: in one transaction re-check 0 rounds, delete the season's winners and entries, then the season. The confirm page lists how many entries/winners will go.
- Round delete is blocked when it has winners or a winning team (so `RoundWinner` rows are never orphaned). Keep.
- Lengths: read every `HasMaxLength`/column size from `MotorsportsContext` and the dump (known: `Round.Note` 1024, others 20-100). Legacy validates only Note; others fail with a DB truncation/500. MUST: validate every max length with a friendly message, and trim input.
- `Round.Status`, `Venue`, `Sport`, `Country` are string FKs: compare case-insensitively but store the canonical casing from the lookup row.

### Server-side validation holes in legacy Round/Season edit (MUST fix, all before mutation, in the transaction)
- Round edit validates a copy of the DB record, so the **submitted** Status, Rating, Rain, WinningTeam and winners are never validated. New rules: Status must exist; Rating null or 0..10 (whole); Rain null or a defined `RainLevel`; WinningTeam must be a `SeasonEntry` of the round's own season; winners must exist, no duplicates, max 4; the round's `Season` is immutable on edit (legacy takes it from a hidden field); rating cleared if status <= ReadyToWatch and rating <= 0 (legacy binder rule).
- Season edit: WinningTeam must be an entry of this season; winners must exist, no duplicates (no max defined in legacy; keep unlimited but the UI picker follows legacy: all participants). Season `Sport` is immutable.
- Round create in legacy only binds Season, Date, Number, Name, Venue, Status (Note, Rating, Rain, winners can only be set in edit). DECIDE: new create form also offers Note; the rest stays edit-only.
- Season delete rule is in the bullet above (0 rounds only).
- Round Date minimum 2026-01-01 rule from section 1 applies on create, and on update only if the date changed. Number 0..99. Date required.
- Season/SeasonEntry/Round existence checks on update return a validation error, not a 500, and the page shows "not found" for unknown ids.
- Edit forms use **optimistic concurrency** without a schema change (user decision: yes): the form carries a row version = hash of the loaded column values; at save, inside the transaction, compare with the current row (`SELECT ... FOR UPDATE`) and on mismatch abort with **HTTP 409 Conflict** semantics (`ConflictResult` type in Common, mapped to 409 on any API/endpoint, and shown in the Blazor UI as a non-blocking alert "Changed by someone else" with "Reload" and "Show differences" actions; entered values are kept). Applies to Round, Season, SeasonEntry, Team, Participant, Sport, Venue edits and to winners lists. The legacy app writes without versioning, so this only protects against new-app/new-app and detects legacy changes made since load.
- **Team Sport is never editable** (edit form shows it read-only; server rejects any change). Team delete rule in section 3. Country change is free. Participant/Venue country change is free.
- Participant and Team uniqueness: plain case-insensitive equality, never `LIKE` (see section 3).
- Sport create: Name unique (case-insensitive) and required, FullName required? (legacy: Name required, FullName optional; keep).

### UI controls and the data behind them (legacy loads these on every edit/create page, new app uses the cache)
- Round create/edit: season entries of the season (ordered by sport, entry name), all participants (last, first), statuses (Step, Name), venues (Name), rain levels (enum order, `Display` names, see `RainLevel`), rating slider 0..10 step 1 (shows value; null allowed), date.
- Season create: sports ordered by name. Season edit: season entries of the season, all participants.
- SeasonEntry create: teams of the season's sport ordered by name (see 4a/4c). Edit: only the name is editable, season and team are fixed.
- Team create/edit: sports by name, countries by `NiceName` (value is Iso; display `NiceName`), Participant create/edit: countries likewise, title is free text (required). Venue: countries.
- Lists: Rounds index per season by date asc (non-championship greyed, `N (non-championship)`, rating `x/10`, winners, winning team name = entry name in that season else team name); Seasons index per sport (full name + logo), seasons ordered by start date desc, null start first; SeasonEntries index ordered by team name then entry name, includes team country; Teams index grouped by sport with country `NiceName`; Participants index (check view for order; sort by last, first); Sports, Venues indexes.
- Detail pages exist for every entity (Sports, Venues, Teams, Participants, Seasons, SeasonEntries, Rounds); Round details includes event history. Season details shows computed start/end date, winning team, winners. Details/edit links of related entities open in a new tab from forms ("Edit venues", "Edit season entries", "Edit participants").
- Sport logo lookup: `wwwroot/img/{sport.ToLowerInvariant()}.png`, else `notfound.png` with the sport name as text. Cache the existence check at startup/per name.
- `RoundStatus` is stored as the `Status` row name; `Step` comes from the `Status` table (not the enum value), and `MarkReadyToWatch` only moves a round whose current Step < 1 (idempotent no-op otherwise).

### Home page (single aggregated query set; see Phase 6)
- Rounds to acquire: Step = 0 and date <= user's today. Next-up: first round per sport by date with Step < 2 (earliest round not yet in an end state: Finished/Stopped/Cancelled/Postponed are Step >= 2... use the `Status.Step` table), including non-championship rounds. **Legacy behaviour is intentional and kept**: the block shows rounds still to watch, not the real-time next round; an unwatched old round stays "Scheduled" and stays first. No overdue badge. Ordering of the home blocks as legacy (very next up, then next-up by date then sport; rounds to acquire by date then sport).
- **"Today" and time zone**: "today" is used only for (1) Home: rounds to acquire (`Date <= today`), very-next-up/`IsInPast` (edit link enabled) and "n day(s) to go", (2) Round create default date when the season has no rounds, (3) the date picker's today button. It is **never** used in validation (min date is the fixed 2026-01-01) or DB writes. Because it is per user, "today" = the date in the **user's browser time zone**: read `Intl.DateTimeFormat().resolvedOptions().timeZone` through JS interop once per circuit into a scoped `UserClock` (with `TimeProvider.GetUtcNow()` converted via `TimeZoneInfo.FindSystemTimeZoneById`; install `tzdata` in the Docker image), fallback `App:TimeZone` (default `Europe/Brussels`) when unavailable. The Home handler takes `today` as a **parameter** (so queries and cached results are never keyed to server time); never cache day-dependent results across users/days without `today` in the key. Tests: handler tests with fixed `today`; midnight-boundary tests in two time zones.
- Latest seasons: all seasons loaded with all rounds in legacy (heavy): compute start/end dates in SQL (`MIN/MAX(Round.Date)` group by season).
- Event history in legacy is N+1 per next-up card, capped at 15, no pagination: new = one query per selected round, capped at 50, no paging.

### Auth/UX behaviour to keep or fix
- All pages require login except `/login`, `/accessdenied`, `/error`. Failed login shows a generic message (no user enumeration). MUST: `returnUrl` only a relative local path (never absolute, never `//`), logout is POST with antiforgery, login rate limiting, cookie `HttpOnly`/`Secure`/`SameSite=Lax`.
- Culture `en-US` fixed in legacy for parsing/formatting; new app: invariant formatting, dates shown `d MMM yyyy`, input ISO or `dd/MM/yyyy`.
- Errors page shows a request id (`Activity.Current?.Id`); keep with a friendly message, never a stack trace in production. Unhandled DB errors (unique/FK) map to a validation message, not a 500.
- Legacy logs every EF statement to console with sensitive data logging flag on dev only: new app uses structured logging, no sensitive data, SQL logging only in Development.

## 4g. Proposed cascades (NOT applied; user decides per item)

Today every FK in the **real MariaDB** is `RESTRICT`. The old SQL Server scripts (`Db/dbo/*.sql`) declared some cascades; they were lost in the MariaDB port. Column "Old SQL" shows what those scripts had. Changing FKs affects the legacy app (it would cascade too), so none is applied until approved, one migration each (`ALTER TABLE ... DROP FOREIGN KEY IF EXISTS`, `ADD CONSTRAINT`, reversible). Until approved the new app does the work explicitly in a transaction (section 4f), which stays correct after a cascade is added.

| # | FK | Old SQL | Proposed | Effect / reason | Recommendation |
|---|----|---------|----------|-----------------|----------------|
| 1 | `RoundWinner.Round` -> `Round` | ON DELETE CASCADE | same | Deleting a round removes its winner links | Yes (restores old) |
| 2 | `SeasonWinner.Season` -> `Season` | ON DELETE CASCADE | same | Season winners meaningless without the season | Yes (restores old) |
| 3 | `SeasonEntry.Season` -> `Season` | ON DELETE CASCADE | same | Entries belong to the season | Yes (restores old) |
| 4 | `Season.Sport`, `Team.Sport` -> `Sport` | ON UPDATE CASCADE | same | Sport rename becomes one `UPDATE` | Yes (restores old) |
| 5 | `Round.Venue` -> `Venue` | ON UPDATE CASCADE | same | Venue rename | Yes (restores old) |
| 6 | `Round.Status` -> `Status` | ON UPDATE CASCADE | same | Status rename (no UI) | Optional (restores old) |
| 7 | `Team/Venue/Participant.Country` -> `Country` | ON UPDATE CASCADE | same | Iso code fix (no UI) | Optional (restores old) |
| 8 | `Round.Season` -> `Season` | ON DELETE CASCADE | **keep RESTRICT** | Old script would delete all rounds (and via #1 their winners) with the season; contradicts "season delete only with 0 rounds" | No: deliberate deviation |
| 9 | `SeasonEntry.Team` -> `Team` | none | none | Team delete is blocked while entries exist | No |
| 10 | `Round.WinningTeam`, `Season.WinningTeam` -> `Team` | none | none | Winners must be entries | No |
| 11 | `RoundWinner/SeasonWinner.Participant` -> `Participant` | none | none | Participants cannot be deleted | No |

Items 1-7 would make the MariaDB schema match the old scripts again; only #8 intentionally differs.

## 5. Auth and the `User` table

Existing `User` (MariaDB): `Id varchar(36)`, `Username`, `PasswordHash varbinary(50)`, `Salt varbinary(50)`, `Iterations int`, `Prf varchar(10)` (HMACSHA1/256/512, PBKDF2), `ForceChangePassword`, `Title`, `GivenName`, `FamilyName`, `EmailAddress`, `IsDeleted`. Legacy defaults: HMACSHA512, 10000 iterations, 16-byte salt, 32-byte hash (`Security/HashPasswordService.cs`, `PasswordHashingConfig.cs`). The hash is `KeyDerivation.Pbkdf2(password, salt, prf, iterations, 32)`, so the plain-text password is UTF-8 string input.

Approach:
1. EF migration, **additive only**, on `User`: nullable `PasswordHashV3 varchar(512)`, `SecurityStamp varchar(64)`, `ConcurrencyStamp varchar(64)`. Legacy columns stay intact so the old app keeps working during the transition. Nothing else touched.
2. `Motorsports.Infrastructure`: `UserStore` (`IUserStore`, `IUserPasswordStore`, `IUserSecurityStampStore`, `IUserEmailStore` as needed) over `User` (Id = Guid string, `UserName` = `Username`, `IsDeleted` users cannot sign in). `LegacyPasswordHasher : IPasswordHasher<AppUser>`: if `PasswordHashV3` set, delegate to Identity v3 hasher; else verify legacy PBKDF2 (constant-time compare) and return `SuccessRehashNeeded`.
3. On successful legacy login: write `PasswordHashV3`. Keep legacy columns (rollback safety, old app). Document in the PR that a password changed in the new app will not work in the legacy app.
4. Migrate automatically on startup, idempotently:
   - Hosted `DatabaseMigrator` runs before Kestrel serves traffic (await in `Program.cs` before `app.RunAsync`), option `Database:MigrateOnStartup` (default true).
   - Takes a MariaDB named lock (`SELECT GET_LOCK('motorsports-migrate', 60)`) on a dedicated open connection so concurrent instances (and rolling restarts) serialize; second instance waits, then finds nothing to do. Release in `finally`.
   - MariaDB DDL auto-commits, so a crash mid-migration can leave partial state. Therefore every migration uses `migrationBuilder.Sql` with `... IF NOT EXISTS` guards, making a re-run a no-op.
   - Existing tables are never created or altered: baseline migration has an empty `Up`. If `__EFMigrationsHistory` is missing, EF creates it (additive).
   - Legacy app does not know the new columns; they are nullable, so its inserts/reads keep working.
   - Tests (Testcontainers): migrate twice; migrate from two hosts concurrently; migrate on a DB where the columns already exist; legacy-schema snapshot restored from `Db/Backup*_MariaDB.sql` (schema part) then migrated.
   - Log applied migrations and lock wait time.
5. Cookie: `HttpOnly`, `SameSite=Lax`, `Secure` behind proxy via `ForwardedHeaders`. `ForceChangePassword` => redirect to a change-password page (new small page under Auth).
6. Tests: legacy hash fixtures (HMACSHA1/256/512) verify; rehash on login; wrong password; deleted user; lockout optional.

## 6. Phases

### Phase 0 – Scaffolding `[x]` (done 2026-10-24; remaining nice-to-haves in Handover)
- `Directory.Build.props` (net10.0, Nullable, TreatWarningsAsErrors, analyzers, imports `version.props`), `Directory.Packages.props`, `global.json` (SDK 10.0.200 is installed), `.editorconfig`, `.csharpierrc.yaml`, dotnet-tools manifest (`csharpier`, `dotnet-ef`).
- Versioning: create tracked `src/version.props` reading `../version.txt` (property function), remove `src/version.props` from `.gitignore`, wire `SourceRevisionId`, show version in footer. Verify the legacy csproj still builds with it.
- Projects: `Motorsports.Web`, `Motorsports.Common`, `Motorsports.Infrastructure`, slice RCLs (create each when its phase starts), test projects. Add all to `Motorsports.slnx`.
- Common: Vogen defaults, DI scan `AddAll<T>`, `[ExcludeFromDiScan]`, `Result<T>`/error model, `TimeProvider` usage.
- Shared UI (`Motorsports.Ui`): `FuzzySelect<T>`, `FuzzyMultiSelect<T>`, `DateField`; allocation-free `FuzzyMatcher` remains in dependency-free Common, with benchmarks + unit tests. See section 4a.
- Web: `Program.cs`, MudBlazor layout (drawer nav on desktop, hamburger/bottom nav on phone), dark-default theme with light toggle (cookie), per-sport accent helper, health endpoint `/health`, Options validation, `DatabaseMigrator`.
- Dockerfile (+ `.dockerignore`), `docker-compose.yml` for local MariaDB. Replace legacy `Docker_Run.cmd` with a bash `docker-run.sh` for the new app (leave legacy script). Legacy app and new app must be runnable side by side (different ports, same DB).
- Packages: latest stable at time of work (MudBlazor 9.x, FluentValidation, Vogen, Roslynator, CSharpier, Testcontainers.MariaDb, bUnit, Playwright, NetArchTest, BenchmarkDotNet). The initial provider spike used Pomelo 9.0.0 + EF Core 9 on net10 against MariaDB (connect, query, `GET_LOCK`, `IF NOT EXISTS` DDL, DateOnly, char(2), decimal); the application now uses Microting.EntityFrameworkCore.MySql 10.0.13 + EF Core 10.0.13.
- Exit: solution builds clean, `/health` OK in Docker, empty authorized home.

### Phase 1 – Infrastructure & baseline `[x]` (done 2026-10-24)
- Scaffold entities + `MotorsportsDbContext` from the real schema (mirror `Models/MotorsportsContext.cs`, same table/column names, lengths, FKs, types). Use value converters for Vogen IDs, `DateOnly` for Round.Date.
- Compare model against the real DB (`dotnet ef migrations add Baseline` must produce an **empty** diff for existing tables; delete or ignore baseline create statements, use `--no-create` approach: baseline migration with empty Up). Confirm with `dotnet ef migrations script`.
- Testcontainers MariaDB fixture loads schema-only DDL for the legacy tables and synthetic seed data; no dump rows are included.
- Verify DateOnly, decimal, char(2), and binary mappings against MariaDB; verify startup migration twice, concurrently, and with the auth columns already present.
- Characteristics tests for the legacy rules in section 3 written against the **new** handlers as slices land (red first).
- Exit: DbContext matches DB; fixture works.

### Phase 2 – Auth `[x]` (done 2026-10-24; user visual browser check completed)
Section 5. Login page (MudBlazor, mobile friendly), logout (POST), access denied, default authorize policy, local-return-url check, change-password page for `ForceChangePassword`.

Delivered: `Motorsports.Auth` slice (RCL: login/logout/access denied/change password, endpoints, validators, force-change middleware, rate limit); `Infrastructure/Auth` (`UserStore`, `LegacyPasswordHasher` PBKDF2 HMACSHA1/256/512 verify + rehash to Identity v3 in `PasswordHashV3`, claims factory); Web wiring (cookie `ms.auth` HttpOnly, SameSite=Lax, Secure unless `Auth:SecureCookies=false`; fallback authorize policy; revalidating auth state). Deleted users cannot sign in. Footer layout fixed (`ms-main` flex column, footer `margin-top:auto`). Tests: unit (hasher, LocalUrl, validators), architecture (Auth not dependent on Web), 22 characteristics tests incl. end-to-end WebApplicationFactory login flows.

### Phase 3 – Reference data slices `[x]` (done 2026-10-24; user visual browser check completed)
Sports, Venues, Participants (list with search/filter, details, create, edit; no delete). Teams: same plus delete only when the team has no season entries (sport read-only on edit). Shared UI building blocks in `Motorsports.Ui` (e.g. fuzzy pickers and date field); Country and Sport lookups are cached.

Delivered (checklist):
- [x] Four slice RCLs `Motorsports.Sports|Venues|Teams|Participants`, each referencing Common + Infrastructure and reusable UI where needed (architecture tests enforce no slice-to-slice or Web dependencies). Web references them, adds them to `Router.AdditionalAssemblies` / `MapRazorComponents`, calls `AddXServices()`, and shows nav links inside `AuthorizeView`.
- [x] Per slice: list (search + paging for Venues/Teams/Participants; Sports is a short plain list), details, create, edit. Teams also have a delete page. Routes: `/x`, `/x/create`, `/x/details/{key}`, `/x/edit/{key}`, `/teams/delete/{id}`; string keys (Sport/Venue name) are URL-escaped. Every routed page has explicit `[Authorize]` (architecture test reflects over all routed components); anonymous requests redirect to `/login` (characteristics test over all 17 routes).
- [x] Form models + FluentValidation (trim, required, DB max lengths; a unit test compares the limits with the EF model). All validation runs before any mutation; FK keys (country, sport) are looked up and the **canonical stored key** is written.
- [x] Uniqueness by plain equality (collation does the case/accent folding): Sport.Name, Venue.Name, Team(Name, Sport) excluding self, Participant(Title, First, Last, Country). The current migration chain backs Team and Participant uniqueness with DB indexes; create/edit also pre-check inside `Serializable` transactions, with deadlocks retried (see `DbOperations`). Sport/Venue duplicates are backed by their PKs (mapped to a friendly conflict).
- [x] Sport/Venue rename: one transaction, re-check uniqueness, insert new PK row, repoint dependents (Sport -> Team, Season; Venue -> Round) with `ExecuteUpdate`, delete the old row. A case/accent-only rename goes through a temporary key (`~<guid>`), then the final key.
- [x] Optimistic concurrency without schema changes: the form carries `RowVersion` = SHA-256 prefix of the loaded values (`Common/RowVersion`); inside the transaction the row is locked with `SELECT ... FOR UPDATE`, re-read and compared. Mismatch -> `Error.Conflict`; the page keeps the typed values and offers Reload / Show differences. Unknown ids -> `NotFound` result; pages render a not-found notice with HTTP 404 during prerender.
- [x] Teams: Sport is read-only on edit (UI) and rejected server-side if it differs; Country may change. Delete locks the team row, re-counts `SeasonEntry` in the same transaction, blocks with a conflict when > 0 (other restricting FKs, e.g. Season/Round winning team, are also mapped to a conflict, never a 500).
- [x] Country displays `Country.NiceName` (fallback Name, ISO), country list sorted by it. Participants sorted LastName, FirstName, Country (nice name).
- [x] Shared infra: `Infrastructure/DbOperations.InTransactionAsync` (one context per operation, rollback on failed result, deadlock retry, DB error -> result mapping), `Infrastructure/Lookups/LookupCache` (Countries, Sports; `Invalidate` after commits, `MaxAge` 30 s via `Cache:MaxAge`, entries served stale then refreshed once in the background, `RevalidateAsync` when a form opens re-reads if older than 5 s), `Common/{RowVersion, FormErrors, Paging}`.
- [x] Tests: unit (validators, limits vs EF model, RowVersion, FormErrors), architecture (slice isolation, `[Authorize]` on routed pages), characteristics on Testcontainers MariaDB with synthetic rows (21 reference-data service tests incl. renames with/without dependents and case-only, duplicates, FK/length validation, stale conflict, not found, team delete allowed/blocked, concurrent duplicate creates, cache) and 2 web tests (anonymous redirect for all routes; signed-in render + 404).
- [x] Venue details Rain column uses the legacy `RainLevel` display names (stored `decimal(1,0)`); the Round column shows `number` or `number - name`.
- [x] Long dates (`MMMM d, yyyy`) are prefixed with a two-letter weekday (`Sa, September 7, 2024`) via `Common/DateDisplay`.

### Phase 4 – Seasons & SeasonEntries `[x]` (done 2026-10-24)
- [x] Added the isolated `Motorsports.Seasons` slice and wired its services, routes, authorization, navigation, and architecture checks.
- [x] Seasons list grouped by sport; create, details, edit, and delete. Details compute start/end dates from rounds and show entries, rounds, winning team, and participant winners.
- [x] Season edit keeps Sport immutable and atomically replaces winners; validates participants and winning team against the submitted season; row-version includes the winner set.
- [x] Season delete is transactional and only allowed with zero rounds; it explicitly removes season-winner links and entries and confirms their counts.
- [x] Season entries list, details, create, edit, and delete. Team is fixed on edit; create filters teams to the season's sport and the server rechecks that constraint.
- [x] Season-entry create/edit shows the original team name and all historical entries newest first; create search includes those entry names and prefills from the latest entry name.
- [x] Added cached participants, teams with history, and season-entry lookup options; participant/team/season-entry writes invalidate the relevant caches after commit.
- [x] Characteristics tests cover submitted-value validation, winner replacement, stale edits, sport filtering, season/entry delete rules, cache freshness, route authorization, and 404 behavior.
- [x] Season details prepend country flags to winning participants and the winning team using their ISO country codes.
- [x] Participant details list distinct sports from the participant's round wins and season wins.
- [x] The profile-name menu contains icon-prefixed Change password and POST logout actions; Change password is no longer in the navigation drawer.
- [x] Verified `Motorsports.Web/wwwroot/img/notfound.png` matches the legacy asset byte-for-byte; missing or unrecognized sport images redirect to `/img/notfound.png`.
- Note: seasons have no stored start/end fields; dates are computed from their rounds, so no date input is appropriate in this phase.

### Phase 5 – Rounds `[x]` (done 2026-10-25)
- [x] Isolated `Motorsports.Rounds` slice: list at `/seasons/{id}/rounds` (date asc, scroll to latest), details/edit/delete at `/rounds/{id}`, create at `/seasons/{id}/rounds/create`.
- [x] Create binds season, date, number, name, venue, status, note; edit also rain, rating 0..10 whole, winning team (`FuzzySelect` of season entries), winners (`FuzzyMultiSelect` max 4).
- [x] `MarkReadyToWatch` is idempotent (Step < 1 → ReadyToWatch; otherwise no-op). Delete blocked when winners or winning team exist.
- [x] Event history in-slice, capped at 50 and date-desc; same venue/sport, championship vs non-championship number rule, Step > 1, date ≤ round date.
- [x] Venue country flags on round list, details, event history, and venue picker. Dates via `DateDisplay`; ratings via `RatingDisplay` (`6 / 10`).
- [x] Lookup cache: Venues + Statuses; venue writes invalidate Venues.
- [x] Round and season victories are visible on Participant/Team details as history; edit forms remain the write surface.
- [x] Characteristics tests for uniqueness, min date, ready-to-watch, winners/concurrency/delete, event history, list order; web tests for authorize + 404.
- [x] Event-history round names link to round details on the round details and edit pages.
- [x] Participant details list round wins with round links, venue flags and ratings. Participant/team details list won seasons with season links and computed start/end dates.

### Phase 6 – Home `[x]` (done 2026-10-25)
- [x] Added isolated `Motorsports.Home` slice at `/`, registered in the host and protected with explicit `[Authorize]`.
- [x] `GetHomeOverviewAsync` projects very-next-up, next-up per sport, and due Scheduled rounds to acquire; Latest Seasons was removed after user review.
- [x] Very-next-up is the earliest per-sport next-up round dated today or earlier using the passed `UserClock.Today`; the page refreshes if browser-zone detection changes the current date. Future rounds show days remaining and no Edit link.
- [x] Event history for each next-up-by-sport card is limited to the prior ten years, filtered by venue/sport/championship number/Step/date, capped at 50 per event, and loaded with batched queries.
- [x] Rounds to acquire can be marked Ready to watch through an authorized, antiforgery-protected POST endpoint owned by the Rounds slice; Home edit actions have icons.
- [x] Sport logos use the authorized `/sports/image/{name}` endpoint and its `notfound.png` fallback; Home and sport/team/season views use the shared `SportAccent` helper and responsive/virtualized layout.
- [x] Characteristics coverage verifies the selected round, acquisition set, ten-year history bounds/results, anonymous route protection, and a constant four SQL reads for the populated overview.
- [x] Added a BenchmarkDotNet Home overview benchmark requiring `ConnectionStrings__Motorsports`.

### Phase 7 – Hardening & parity `[x]` (done 2026-10-25)
- [x] Data-integrity pass: reviewed transaction/error mapping and existing uniqueness, FK, nullability and optimistic-concurrency characteristics coverage.
- [x] Security pass: verified existing antiforgery and login-rate-limit coverage; added response security headers and secure-cookie coverage. Production HSTS is enabled. Replaced Compose credential literals with required environment variables.
- [x] Home MariaDB benchmark: 11.156 ms mean and 272.17 KB managed allocation per call on the local Windows 11 / .NET 10.0.4 host. This is a developer-machine baseline, not a production SLA.
- [x] Local circuit-memory estimate (user-measured 2026-10-25): after full GCs induced by `dotnet-gcdump`, heap size minus fragmentation was 14,053,784 bytes with zero tabs and 30,605,360 bytes with five authenticated tabs. Delta / 5 = 3,310,315 bytes (~3.16 MiB) per added tab/circuit. Includes any shared-cache/runtime growth; not an isolated retained-circuit measurement or a production capacity guarantee.
- [x] Docs: README covers local run/configuration, Docker, and migration notes.
- [x] Manual phone-width + desktop browser pass for the section 2 parity table and interactive controls (user-verified 2026-10-25).
- [x] Docker smoke follow-ups resolved; user confirmed the corrected container/footer. Consolidated build-stage copies into two COPY instructions and restore/publish into one RUN, preserving the published-script guard and the non-root runtime stage.
- [x] Release version 2.0.0; final warning-free solution build, CSharpier format/check, 143 tests, and optimized Docker image build passed.

### Phase 8 – Production performance, Vogen IDs, database cleanup & security hardening `[x]`

The 2.0.0 application is release-ready. This phase is a measured post-release quality pass; retain behavior and persisted data unless a planned migration explicitly changes them.

- [x] **Performance baseline and audit:** reproducible current-size and 10× synthetic runs on the authorized `motorsports_phase8` database record Home/lists/history/cache/fuzzy timings, allocations, actual query plans, and fixed SQL counts (section 4b). Retain the existing caveated circuit-memory baseline; no circuit-state change was made. Fixed the demonstrated Vogen team-history tie-sort crash.
- [x] **Container image size:** the `aspnet:10.0-noble-chiseled-extra` image runs the non-root app with IANA time-zone data and an exec-form `dotnet` health probe (no shell or `curl`). Against the prior optimized image, size fell from 378,754,981 to 266,555,591 bytes (112,199,390 bytes / 29.6%). On the same local Docker host and schema-only MariaDB fixture, `/health` was ready in 1.85 s on the baseline and 2.38 s on chiseled; both returned healthy. The chiseled probe was also verified with the production-style Kestrel port 80 and `ms.dalion.eu` Host header. These are single local smoke measurements, not production startup guarantees.
- [x] **Indexes:** six candidates measured before/indexed/restored on synthetic current-size and growth fixtures (section 4b). Retained only `Round(Season, Date, Number)` in migration `20261026133143_AddRoundSeasonDateNumberIndex`: ~46% lower growth cold-team-lookup p50 with a covering plan and repeat confirmation. Other candidates rejected. Team and positive Round uniqueness indexes remain separate integrity constraints.
- [x] **Vogen IDs:** define Vogen value objects for all persisted entity IDs and natural string IDs, including `UserId`, and use them consistently in EF entities/relations, slice commands and queries, and relevant lookup contracts. Convert `UserId` to/from the existing string representation at ASP.NET Identity boundaries. Add EF value converters and update test fixtures. Keep database column types/values, routes, and user-visible behavior unchanged; convert at other framework boundaries where primitives are required. Query-translation regression tests cover converted-key ordering, membership, projections, team/participant searches, and the Home earliest-round query. Do not access a Vogen `.Value` inside translated EF predicates/orderings; compare/order the converted property or perform conversion after materialization.
- [x] **Schema inventory and cleanup plan:** compared the local restored schema, EF model, migration metadata, and query usage. The copy has no migration-history or Data Protection key table and lacks the expected auth columns, sport image column, and Participant unique index; existing migrations add these. Existing FKs are all `RESTRICT`. No other column-type mismatch was identified. Duplicate aggregates confirmed the new Team and positive Round constraints are data-compatible.
- [x] **Data-preserving migrations:** added idempotent migration `20261026100239_AddBusinessUniqueConstraints` with matching `Down`, removing redundant single-column FK indexes only after replacement indexes exist. It passed MariaDB characteristics tests, including re-running the migrator. The local restored copy was not migrated; actual production was not accessed.
- [x] **Production ingress and host validation:** production hostname is `ms.dalion.eu`, served through nginx-proxy-manager (NPM). NPM and the Web app share a Docker network; ASP.NET Core sees NPM at `172.18.0.3`. The app container must not publish a host port; only NPM reaches it over the shared Docker network. NPM terminates HTTPS and proxies to `http://motorsports:80`; production sets `Kestrel__Endpoints__Http__Url=http://0.0.0.0:80` (the checked-in image default remains port 8080). Configure `AllowedHosts` for the production hostname and trust forwarded headers only from `172.18.0.3`. Test rejection of unexpected Host and spoofed forwarding headers.
  - Fixed `docker-compose/motorsports-web/docker-compose.yml`, which contradicted this setup. It now uses service/container `motorsports` (the NPM target), publishes no host port, and sets `Kestrel__Endpoints__Http__Url=http://0.0.0.0:80`. The double-underscore spelling is required because the exec-form health probe reads only that environment variable; the old `Kestrel:Endpoints:Http:Url` spelling would leave it probing 8080. `FORWARDED_HEADERS_KNOWN_PROXY_0` defaults to `172.18.0.3`.
  - Simulated 2026-10-26 with image `motorsports-web:2.0.0` on a private Docker network (no published port, trusted proxy IP `172.30.0.3`). The non-root app bound port 80 and became healthy. Results:
    - Trusted proxy, `Host: ms.dalion.eu`, `X-Forwarded-Proto: https`: 302 to `https://ms.dalion.eu/login`.
    - `Host: evil.example`: 400. A bare container IP as Host: 400.
    - Spoofed `X-Forwarded-Host: evil.example` from the proxy: ignored (redirect stays on `ms.dalion.eu`).
    - Untrusted source with spoofed `X-Forwarded-Proto: https` / `X-Forwarded-For`: ignored (redirect stays `http://`). Forwarded-For spoofing against the rate limiter is covered by tests.
    - HSTS present.
  - Found and fixed: the antiforgery cookie lacked `Secure` (the ASP.NET default policy is `None`). It now follows `Auth:SecureCookies` like the auth cookie; regression test `Antiforgery_cookie_is_secure_when_secure_cookies_are_enabled`.
  - A live production smoke test after deployment is a user action.
- [x] **Data Protection key persistence:** user selected MariaDB storage for the Data Protection key ring, provisioned automatically during startup migration. Use a stable application discriminator and a dedicated key table/context integration compatible with the existing EF Core/MariaDB setup. User explicitly says at-rest encryption is not required. Document that database access therefore grants access to the key ring, plus backup/restore and key-rotation procedures. Verify cookies/tokens survive container replacement and the application has only the necessary DB access.
  - Container replacement: ✅ verified 2026-10-26 with image `motorsports-web:2.0.0` against restored DB `motorsports`, `ASPNETCORE_ENVIRONMENT=Production`. The container was created and became healthy, then the user logged in. It was then removed (`docker rm -f`) and recreated from the same image with the same settings, and became healthy again. The browser session stayed signed in. `DataProtectionKeys` held 1 row before and 1 after, so no new key ring was generated.
  - Least-privilege DB access: ✅ verified 2026-10-26. The restored DB `motorsports` user had `ALL PRIVILEGES` on `motorsports.*` only, with no global rights and no `GRANT OPTION`: scoped, but broader than needed. Required set: `SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES` on the app DB (documented in README with `REVOKE`/`GRANT`).
    - Test setup: throwaway `motorsports_lp`, restored from `db\Backup20261024_MariaDB.sql` (pre-migration schema), and a temporary user with exactly that set.
    - With the `motorsports-web:2.0.0` image in Production: all 7 migrations applied under the `GET_LOCK` migrator, `/login` returned 200, the key ring was created (1 row), and after a restart: "Database is up to date".
    - Probes as that user: `DROP TABLE`, `CREATE PROCEDURE` and cross-DB `SELECT` were denied.
    - The throwaway DB and user were removed. Migration `Down` needs `DROP`, so rollback is an admin action.
    - Applying the narrowed grants on production is a user action. Applied 2026-10-26 on the local restored `motorsports` DB; `SHOW GRANTS` confirms exactly the 8 rights plus global `USAGE`.
- [x] **Content Security Policy:** user confirmed no application CSP errors in the browser on 2026-10-26 after excluding Firefox-extension noise. Promoted the expanded policy from report-only to enforcement; characteristics tests assert the enforcing header, nonce-bearing import map/scripts, and absence of the report-only header. Inline styles remain allowed for MudBlazor.
- [x] **Regression coverage:** MariaDB characteristics cover the schema changes, pre-existing index, duplicate/zero/same-date data, concurrent/repeated migration, `Down`/reapply, row preservation, and the Vogen cache tie-breaker. All 156 tests pass. Section 4b records paired index comparisons, allocations, memory-measurement limits, and the storage/write trade-off.
- [x] **Exit:** warning-free build, CSharpier format/check, full solution tests, migration SQL reviewed, and measurable performance/schema/security outcomes recorded in this plan. If the audit finds no justified optimization or schema change, record that result rather than adding speculative changes. Closed 2026-10-26: `-warnaserror` build clean, CSharpier check clean, 157/157 tests green, idempotent migration SQL reviewed, evidence recorded above and in section 4b.

## 7. Resolved decisions and remaining open points

Resolved:
- Provider: Pomelo 9.0.0 + EF Core 9 (see section 1), spike in Phase 0.
- `src/Db/*.sql` backups stay in the repo; the user updates them manually. Agents ignore them except as read-only schema reference.
- Theme: dark default, subtle per-sport branding (logo + accent).
- Legacy application has been retired; preserve its database only as historical source/reference where useful.

Open:
- None. Phase 8 database cleanup is limited to constraints, indexes, and schema type alignment; unused tables/columns and `User.PasswordHash`, `Salt`, `Iterations`, and `Prf` will be retained.

Resolved:
- First release is `2.0.0`; minimum round date 2026-01-01 for new/changed dates only. Round/participant uniqueness rules are in section 3.

## 8. Definition of done (per slice)

- All rows for the slice in section 2 work at 360px width and desktop.
- Business rules (section 3) covered by characteristics tests incl. invalid input and conflict cases.
- Read queries: `AsNoTracking` + projection; write: validate -> transaction -> mutate; no N+1.
- `dotnet csharpier check .`, `dotnet build -warnaserror`, `dotnet test` all green.
- Performance: `dotnet counters`/logs show no per-keystroke DB calls, Home query count is constant, circuit memory per session measured and recorded in PLAN.md.
- PLAN.md checklist updated.
- End-of-phase routine from AGENTS.md ("End-of-phase routine") done: zero warnings/analyzer issues, CSharpier formatted, Issues and Handover sections updated.

## 9. Issues encountered and corrections (keep updated every phase)

Corrections to earlier assumptions (verified against the real MariaDB dump and the running app):
- `Virtualize` on statically rendered Home rendered only its first few items (Home history showed 5 events); short Home lists use `@foreach`.
- Real column widths differ from the legacy EF model: `Venue.Name` and `Round.Venue` are `varchar(100)` (legacy model said 50); `Round.Note` is `text`; `Round.Number` is `smallint`; `Status.Step` is `tinyint`. Validation max lengths must follow the **DB**: Participant Title 10, First/Last 100, Team Name 50, Sport Name 50 / FullName 100, Season Label 100, SeasonEntry Name 50, Round Name 100, Venue Name 100.
- Real FK constraint names are `<Table>_<Col>_FK` (e.g. `Round_Season_FK`), not the legacy model's `FK_Season_Round`. Nothing depends on them; do not hardcode either.
- DB collation is `utf8mb4_uca1400_ai_ci` (case- and accent-insensitive). So plain `==` in LINQ on string columns is already case/accent-insensitive in SQL, which matches the uniqueness rules. Do not use `ToLower()` in queries (defeats indexes).
- The dump has 0 rows in `User`; there are no real credentials to test with. Auth tests must seed users with a legacy-style hash generated in the test.
- `dotnet ef migrations add` generates a full `CreateTable` baseline. Hand-edit `Baseline.Up/Down` to be empty (done). The second migration `AddUserAuthColumns` was scaffolded as an empty diff (the entity already holds the columns, so the snapshot is right) and its `Up/Down` were filled by hand with `ADD COLUMN IF NOT EXISTS` / `DROP COLUMN IF EXISTS`.
- EF Core's own `Migrate()` already takes a named lock (`__<db>_EFMigrationsLock`). `DatabaseMigrator` still holds `motorsports-migrate` on the same connection as an explicit guard and skips work when nothing is pending.
- `EnableRetryOnFailure` is **not** enabled: it forbids user-started transactions unless wrapped in `CreateExecutionStrategy().ExecuteAsync`, which conflicts with the validate-in-transaction pattern. Revisit only with a concrete need.
- `Directory.Build.props`/`Directory.Packages.props` sit in `src/`, so they would also apply to the legacy projects. They are guarded with `IsLegacyProject`; the legacy csproj still pins its own versions.
- Provider spike (Phase 0): Pomelo 9.0.0 + EF Core 9.0.21 on net10.0 connected to MariaDB 11.7, ran `GET_LOCK`/`RELEASE_LOCK`, `CREATE TABLE IF NOT EXISTS`, and applied the empty baseline. Phase 1 confirmed typed reads against MariaDB. The application has since moved to Microting.EntityFrameworkCore.MySql 10.0.13 + EF Core 10.0.13.
- Phase 1 MariaDB reads confirmed `DateOnly`, decimal, `char(2)`, and binary values. The schema fixture exposed that legacy password bytes need `varbinary(50)` mapping and Status.Step is signed `tinyint`; both are aligned in the entity model. Country's two smallint columns are now represented as well.
- Analyzers: `AnalysisLevel=latest` (not `latest-recommended`, too noisy). Disabled with reasons in `.editorconfig`: CA1716, CA1000 (public `Result<T>` API), RCS1181, RCS1161, RCS1043 (extension classes must be `partial`), RCS1201 (EF fluent config).
- `InvariantGlobalization=true` cannot rely on `string.Normalize`; `FuzzyMatcher.Normalize` uses its own Latin-1/Latin Extended-A fold tables. Verify on Linux in the Docker image.
- Phase 8 will replace primitive IDs with Vogen value objects; database and route representations are planned to remain unchanged.
- Template cleanup: bootstrap, Counter, Weather, NavMenu css removed; the Web project uses MudBlazor only.
- FIXED: `GET /` returned 500 because `TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels")` throws on Windows. `UserClock.ResolveZone` falls back to the local zone. Linux/Docker (tzdata) resolves IANA ids directly (verified in the container).
- CSharpier does **not** support an `excludes:` key in `.csharpierrc.yaml`. Exclusions live in `.csharpierignore` (legacy app, `src/Build`, Migrations, bin, obj). Before this was fixed, `csharpier format .` reformatted the legacy projects; they were restored with `git checkout`. Always check `git status src/Motorsports.Scaffolding.Core src/Build` is clean after formatting.
- .NET 10 SDK + xunit.v3 (Microsoft.Testing.Platform): VSTest mode is refused. `global.json` has `"test": {"runner": "Microsoft.Testing.Platform"}`; run `dotnet test --solution src/Motorsports.slnx`. Test projects need `<OutputType>Exe</OutputType>` and `<Using Include="Xunit" />`.
- Test projects live in `src/tests/` (not the repo-root `tests/`) so `Directory.Build.props` applies to them. `Motorsports.CharacteristicsTests` is created in Phase 1 (needs Testcontainers).
- The `create` tool and a `Remove-Item`/`New-Item` in the same parallel batch race; create directories first, in a separate call.
- MudBlazor 9: the `PickerActions` context is `MudPicker<DateTime?>`, not `MudDatePicker`.
- Docker: `src/Motorsports.Web/Dockerfile` (build context = repo root: `docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:latest .`) uses the .NET 10 `noble-chiseled-extra` runtime, runs non-root, and listens on 8080 by default. Its exec-form health probe calls `/health` without a shell or `curl`; `HEALTHCHECK_HOST` defaults to `ms.dalion.eu` and can be set when `AllowedHosts` differs. A schema-only MariaDB smoke test verified migration startup and health on both default port 8080 and production-style port 80. The runtime variant includes IANA zone data.
- The legacy csproj still builds (0 warnings, 0 errors) with the new `global.json`, `version.props` and `Directory.*.props`.
- Phase 2: `User` itself is the Identity user type (no separate `AppUser`). Legacy `PasswordHash/Salt/Iterations/Prf` are never modified; a password set in the new app is stored only in `PasswordHashV3`, so it will not work in the legacy app.
- Blazor routing ignores the fallback authorize policy: every routable page needs an explicit `[Authorize]` or `[AllowAnonymous]` (Phase 3+ pages must do so). Login, access denied, error, and not-found pages remain static SSR (`ExcludeFromInteractiveRouting`); login/logout/change-password POSTs are minimal-API endpoints so antiforgery and login rate-limiting apply.
- A minimal-API POST only gets antiforgery validation if it binds a form; `/logout` binds `IFormCollection` for that reason (a test caught it unprotected).
- BL0008: no initializer on `[SupplyParameterFromForm]` properties; use `= default!` plus `??= new()`.
- Cookies are `Secure` outside Development. Deploying over plain http (e.g. compose without TLS) requires `Auth__SecureCookies=false`.
- Login rate limit is per remote IP (fixed window, `Auth:LoginPermitLimit`/`LoginWindowSeconds`). Forwarded headers are accepted only from configured `ForwardedHeaders:KnownProxies` addresses (default framework loopback proxies); set `ForwardedHeaders__KnownProxies__0` to the TLS proxy address in deployment. Account lockout was not implemented (optional in plan).
- Tooling note: in this environment only PowerShell was available for file edits (no create/edit tools); `R` is an alias of Invoke-History, so do not name helpers `R`.
- `FuzzySelect`/`FuzzyMultiSelect` use immediate query updates to avoid requiring a second Enter press. Their click/typing behaviour and the theme switch still need a browser test (Playwright) or manual check.
- Phase 3: this environment has no create/edit tool; all files were written through PowerShell with .NET `WriteAllText` (no BOM). PowerShell 5.1 `Set-Content -Encoding utf8` adds a BOM, avoid it.
- Phase 3: `FluentValidation` 12 has no `RuleFor().Transform`; trimming is done with `Must(v => v.Trim().Length <= Max)` and the service stores the trimmed value. A routed page and a record cannot share a name in one namespace (`SportDetails` page vs record): records are named `*Info`.
- Phase 3: `FromSql("... FOR UPDATE")` must be followed only by `AsNoTracking().ToListAsync()`; composing `SingleOrDefault` wraps it in a subquery and the lock is lost.
- Phase 3: Pomelo's execution strategy can wrap the `MySqlException` in an `InvalidOperationException` ("likely due to a transient failure") even without `EnableRetryOnFailure`; `DbOperations` classifies the whole exception chain. A concurrent-create test caught that (Serializable duplicate-check races deadlock; retried up to 5 times with jitter, then a Busy conflict).
- Phase 3 follow-up: shared pickers now live in `Motorsports.Ui`; Country/Sport fields use `MudAutocomplete` through `FuzzySelect`, backed by the in-memory `FuzzyIndex`.
- Phase 4: lookup cache now includes Participants, Teams with all season-entry history, and season-entry names. Team, Participant, Sport, Season, and SeasonEntry writes invalidate the affected cached kinds after commit; Phase 5 still needs to add Venues and Statuses for round pickers.
- Phase 4: EF could not translate ordering by properties of a constructed `SeasonRow`; the season list now orders its scalar projection before constructing the display record. The characteristics test caught this at the HTTP route.
- Phase 4 follow-up: Sport details use the latest completed season with winner data; date and rating presentation, season-entry team labels, action icons, centered sport previews, and the interactive ChangePassword layout are clarified in section 4h-3.
- Phase 4 finishing follow-up: season detail winner labels now include participant/team country flags; participant sports are distinct across round and season wins. The account menu keeps its logout antiforgery form outside MudMenu's popover because closed menu content is absent from prerendered markup.
- Venue details: `Round.Rain` is the legacy `RainLevel` enum (0–7), not a count; display names live in `Common/RainLevel.cs`. Round labels are `12` or `12 - Indianapolis 500` because `Round.Name` is often null.
- Long dates use `DateDisplay` (`Su`/`Mo`/`Tu`/`We`/`Th`/`Fr`/`Sa` plus `MMMM d, yyyy`); .NET `ddd` would be three letters (`Sun`).
- Phase 3: pages are interactive (InteractiveServer) with `EditForm OnSubmit`; search commits on Enter/blur, never per keystroke. No browser verification of MudBlazor interaction, mobile layout or `%2F` in a Sport/Venue name route segment.
- Reference-data follow-up: `AddSportImages` adds nullable `Sport.Image` idempotently and seeds the six current legacy logos from embedded image resources; the migration tests compare persisted bytes with the embedded files.
- Phase 5 migration follow-up: the participant identity migration was not discoverable because it lacked EF's generated context metadata. EF migration metadata is now present. Per the user's production-data confirmation, the migration only creates the approved unique index and makes no participant or winner data changes.
- Phase 6: EF could not translate a per-sport season rank over a constructed record or a custom comparer in SQL ordering. Latest Seasons was removed in follow-up after user review, eliminating that query and reducing the populated Home query count to four.
- Phase 7: the Home benchmark initially did not run because BenchmarkDotNet rejects sealed benchmark classes; `HomeOverviewBenchmarks` is now unsealed and the local baseline is recorded above. The Home handler's fixed four-read assertion remains in the characteristics suite.
- Phase 8 synthetic audit: in-memory team-history ordering attempted to compare Vogen `SeasonId` values when start dates tied; Vogen intentionally omits comparison interfaces. Sort the primitive `.Value` only after materialization. The new MariaDB regression test covers equal null dates and newest-entry selection.
- Phase 8 synthetic audit: an index removing a small round-list filesort was not enough evidence to retain it. Growth measurements showed its actual benefit in the team cache's correlated date aggregates: a covering scan reduces page work, not round-row count. Record before/indexed/restored samples and the earlier repeat rather than attributing general host/JIT timing shifts to every candidate.
- Phase 7: source-controlled Compose files contained credential literals. They now require deployment/local environment variables instead; rotate any credentials previously used from those files. Compose configuration validation passes with the required variables supplied.
- Phase 7 Docker smoke check: the fallback authorization policy redirected `/_framework/blazor.web.js` to login, returning HTML where JavaScript was required. The bootstrap endpoint now explicitly allows anonymous access; page authorization remains unchanged and an HTTP characteristics test covers the script response.
- Phase 7 Docker publish follow-up: restoring before copying Razor source omitted the framework static-assets package, so authenticated script requests still returned 404. Docker now restores with all Razor source present and fails the build if the published `blazor.web.js` is missing. Verified the corrected production container returns 200 `text/javascript` for Blazor/MudBlazor scripts, 200 for health, and a login redirect for anonymous Home requests. Authenticated browser interactions still need rechecking with the rebuilt image.

## 10. Handover (rewrite at the end of every phase)

Date: 2026-10-26. Phases 0–8 are complete.

State:
- The new app is organized into Common, shared UI, Infrastructure, feature slices, and the Web host. Unit, architecture, benchmark, and MariaDB characteristics projects are in the solution.
- `Motorsports.Home` owns `/`; its populated query path performs four reads, covered by a characteristics test. The local benchmark baseline is 11.156 ms mean / 272.17 KB managed allocation per call.
- The user completed the mobile/desktop checks and supplied full-GC heap snapshots: the five-tab incremental live-heap estimate is ~3.16 MiB per circuit, including shared/runtime growth.
- Phase 7 added security response headers and production HSTS; auth characteristics cover antiforgery, login throttling, cookie flags, and security headers.
- Compose files no longer embed database/admin credentials. Supply their required variables from the deployment environment and rotate any previously used values.
- `README.md` documents local setup, configuration, container deployment, and startup migrations.
- Docker restores with Razor source present, verifies the published bootstrap script, and uses two build-stage COPY instructions plus one restore/publish RUN. The user confirmed the corrected container/footer; SHA metadata is no longer duplicated.
- Phase 8 Vogen conversion and Data Protection key persistence are implemented. The idempotent migration SQL was reviewed, and EF reports no pending model changes.
- The chiseled Docker image passed schema-only MariaDB startup/health checks on ports 8080 and 80. The 266,555,591-byte image is 29.6% smaller than the 378,754,981-byte optimized baseline; single local cold-start observations were 2.38 s versus 1.85 s.
- Final Phase 8 validation (after the ingress fixes): warning-as-error solution build passed, CSharpier check passed, and all 157 tests passed. Build/test used isolated artifact paths to avoid the running development app's output DLLs. Generated idempotent migration SQL was reviewed; EF reports no pending model changes. The earlier chiseled Docker build and health smoke also passed.
- The user-authorized preflight used a local restored copy only. It has 1,823 rounds (80 unnumbered), 88 seasons, 77 teams, and 214 participants; duplicate aggregates for Team names, positive round numbers, and participant identity were all zero. User data and password hashes were not read. No persistent schema or data changes were made to that copy; temporary benchmark indexes were removed.
- Performance/index audit is solved (section 4b). The separately authorized `motorsports_phase8` throwaway DB was reset to synthetic data and measured at current-size and 10× growth scales. Retained only `Round(Season, Date, Number)` for the demonstrated cold team-history lookup improvement (~46% growth p50); added guarded migration `20261026133143_AddRoundSeasonDateNumberIndex` and regression coverage. All seven migrations are applied to the throwaway DB, with 18,240 synthetic rounds and no temporary audit indexes. The original restored copy and live production were not changed.

Known gaps / not done:
- Production password rotation has been prepared by the user but not yet applied.
- No admin/user UI or account lockout (outside the agreed phase scope).
- User confirmed on 2026-10-26 that authentication survives an app restart with the MariaDB `DataProtectionKeys` key ring. Full container replacement was verified the same day: the session survived, and the key row count stayed at 1. Least-privilege DB access was verified with a restricted user (the 8 grants listed in README) and applied to the local restored `motorsports` DB.
- Ingress was verified in a local production-shaped simulation (section Phase 8). The production compose was fixed (container `motorsports`, no published port, `Kestrel__Endpoints__Http__Url` on port 80), and the antiforgery cookie is now Secure.

User actions at deployment (live production was never accessed by the agent):
1. Back up the production DB.
2. Apply the narrowed grants to the production DB user.
3. Stop the old `docker-compose/motorsports` stack, then start `docker-compose/motorsports-web`.
4. Smoke test `https://ms.dalion.eu`: login, a cookie with `Secure`, CSP clean, and no host port published.
- Not every Rider `ide`/`csharpsquid` inspection is guaranteed clean (ask before touching captured-disposed warnings).
- Event history is not grouped by year and includes any event with `Status.Step > 1`, including postponed/cancelled events, by documented design.
- Home and representative service/cache/fuzzy baselines now include production-shaped synthetic current-size and 10× datasets; these are still local Windows/Docker observations, not production capacity data. Allocation deltas are process-wide and include harness overhead; no new isolated retained-circuit measurement was made.
- Phase 8 Vogen conversion is complete. EF translation regressions are covered, including converted-key ordering, membership, projections, team/participant searches, and Home query behavior.
- The Data Protection key table and stable application name, production host allow-list, trusted-proxy configuration, and enforcing CSP are implemented; the full suite includes MariaDB-backed characteristics.
- A Rider database connection targets the user's local restored copy, not live production. Schema, indexes, FK rules, migration state, query plans, and aggregate duplicate/edge counts were inspected; no actual row contents, credentials, or password hashes were read. Live production was not inspected or changed.
- The user confirmed the report-only CSP produced no application errors; the expanded policy is now enforced.
- CSP enforcement follow-up: Blazor adds a separate framing policy on page responses. The header regression test selects the application's full policy rather than assuming there is only one enforcing header; both policies remain enforced.
- The venue list search uses the shared lookup cache to resolve Vogen venue keys before a translated SQL key filter; the other reference-list searches use translated string/name or typed-key queries.

Next: Phase 8 is closed. Remaining items are the deployment actions listed above. No further phase is planned; ask the user before starting new work.

### Phase 8 clarification handoff (2026-10-26)

The user asked to clarify Phase 8 before implementation. The confirmed deployment/security decisions below remain binding; do not ask the user to repeat them. Implementation is complete and the phase exit criteria are met.

Confirmed decisions:
- Production hostname is `ms.dalion.eu`, behind nginx-proxy-manager (NPM). NPM and Web share a Docker network; ASP.NET Core sees NPM at `172.18.0.3`. Do not publish a Web host port; only NPM reaches the app.
- NPM terminates HTTPS and proxies HTTP to `http://motorsports:80`. Production sets `Kestrel:Endpoints:Http:Url=http://0.0.0.0:80`; the checked-in Dockerfile's default remains 8080. Allow only the production hostname and trust forwarded headers only from `172.18.0.3`.
- Persist the Data Protection key ring in a table in the app's configured `ConnectionStrings:Motorsports` database, provisioned by an idempotent startup migration. Include the table in normal MariaDB backups. At-rest encryption is not required by the user; document that DB access exposes the key material, and cover stable application discriminator, backup/restore, and key rotation.
- No CSP report collector exists. Use browser/manual verification in report-only mode, then enforce the CSP in the same release after local/browser checks pass; production verification is not a prerequisite.
- Vogen covers every persisted ID and natural string key, including `UserId`; convert `UserId` at ASP.NET Identity boundaries. Keep database representation, routes, and visible behavior unchanged.
- The user authorized schema and query-plan work, including changes, only on the local restored copy with a backup. Live production was not accessed. Limit database output to schema metadata, query plans, and aggregate duplicate/edge counts; never print/export real rows, credentials, or password hashes. Do not infer live-production cleanliness from the restored copy or synthetic fixtures.

The local restored-copy preflight found no duplicate conflicts for the Team and positive Round constraints, and the additive, idempotent migration is implemented and tested. The copy itself remains unmigrated; apply migrations only through the normal migration path after reviewing deployment timing. Do not repair/delete live production rows or force a uniqueness rule when conflicts or domain behavior make it unsafe.

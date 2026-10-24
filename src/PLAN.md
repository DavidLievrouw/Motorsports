# Motorsports.Web – rewrite plan

Rewrite of `Motorsports.Scaffolding.Core` (net8 MVC, legacy) as `Motorsports.Web` (net10, Blazor + MudBlazor). Feature parity, new UI. Read `../AGENTS.md` first; it holds the binding rules. This file holds scope, inventory, and phases.

Status legend: `[ ]` todo, `[x]` done. Update as you go.

## 1. Decisions (fixed)

| Topic | Decision |
|---|---|
| Target | `net10.0`, C# 14, new `src/Motorsports.slnx` (legacy project stays in it until cut-over) |
| UI | Blazor Web App, interactive server render mode, MudBlazor, responsive (phone + desktop) |
| Architecture | Vertical slices as Razor class libraries + `Motorsports.Common` + `Motorsports.Infrastructure`; host `Motorsports.Web` |
| Data | EF Core only. MariaDB via `Pomelo.EntityFrameworkCore.MySql` 9.0.0 + EF Core 9.x on net10.0 (Pomelo has no EF 10 stable; Oracle `MySql.EntityFrameworkCore` 10.x is the fallback if the Phase 0 spike fails). Dapper/`IQueryExecutor` dropped |
| DB | Existing MariaDB schema untouched, except additive change on `User` (section 5). Legacy app keeps running against the same DB for the first weeks |
| Theme | Dark default, light optional. Per-sport branding = logo + subtle accent only |
| Performance | Hard requirement, see AGENTS.md "Performance" |
| Versioning | `version.txt` -> tracked `src/version.props` -> all projects (AGENTS.md "Versioning") |
| Auth | ASP.NET Identity, custom user store over `User`, legacy hash verified, rehash on login, migration on startup |
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
| Auth | Login (+returnUrl), logout (POST), access denied, error page | `LoginController`, `Security/*` | [ ] |
| Home | Very-next-up round (across sports), next-up per sport, rounds to acquire, latest 2 seasons per sport, sport logos with fallback, **full** event history (see 4) | `HomeController`, `HomeService`, `Views/Home/*` | [ ] |
| Sports | list, details, create, edit | `SportsController`, `SportService` | [ ] |
| Venues | list, details, create, edit | `VenuesController`, `VenueService` | [ ] |
| Teams | list, details, create, edit | `TeamsController` | [ ] |
| Participants | list, details, create, edit | `ParticipantsController` | [ ] |
| Seasons | list, details, create, edit, delete; winners (participants) and winning team | `SeasonsController`, `SeasonService` | [ ] |
| SeasonEntries | list per season, details, create, edit, delete (team name during season) | `SeasonEntriesController`, `SeasonEntryService` | [ ] |
| Rounds | list per season, details (+event history), create, edit (name, date, number, venue, rain, rating, status, note, winning team, winning participants), delete, **MarkReadyToWatch (POST)** | `RoundsController`, `RoundService` | [ ] |

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
- Event history for a round: same venue, same sport, only rounds with `Number = 0` when the round is non-championship else `Number > 0`, finished-ish status (`Step > 1`), date <= round date, excluding the round itself, newest first, shows winning team (season entry name, else team name), winners (sorted last name, first name), rating, rain. Legacy cap `LIMIT 15` is removed (new feature).
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
8. History limited to 15 (legacy). **Fix (new feature):** show the full history of an event on the Home page (and round details). Query without `LIMIT`; UI virtualized/paged ("show more") so phones stay fast; group by year.
9. Mass-assignment risk via `Bind` lists and edit models. **Fix:** dedicated command/form models per slice, no entity binding.

## 4a. Pickers (parity with legacy select2)

Legacy uses select2 in `Views/Shared/EditorTemplates/*.cshtml`: `Team.cshtml` (single, **grouped by sport**, clearable, placeholder), `WinningParticipants.cshtml` (**multi**, max 4, clearable; legacy label `First Last (Country)`, new label `First Last`, see AGENTS.md), plus `Season`, `Sport`, `Venue`, `Country`, `Status`, `SeasonEntry` single selects, and `Date.cshtml` (typed or picked, `DD/MM/YYYY`). Team picker is filtered by the sport of the page's season. Rebuild as:
- `FuzzySelect<T>` for all single selects (grouping optional). Winning team in a round is limited to that season's entries (shown by the name during the season).
- `FuzzyMultiSelect<T>` for winning participants (chips, max 4, remove with chip x or backspace) and season winners.
- `DateField` for dates (typed `dd/MM/yyyy` or ISO accepted, picker optional, today shortcut; minimum **2026-01-01** for new/changed dates, existing older rounds stay editable when the date is unchanged).
- Lookup lists come from the shared singleton cache (no query per keystroke). Matching spec in AGENTS.md. Keyboard: arrows/enter/escape, works with touch, 44px targets.
- Tests: bUnit for selection/clear/max/chip removal; unit tests for ranking (`"verst"` -> Verstappen, `"max ver"`, `"hamliton"` typo, diacritics `"raikkonen"`); benchmark for matching 5k items per keystroke (target: no allocations per match, microseconds).

## 4b. Schema suggestions (apply only if additive and legacy-safe)

Allowed now: non-unique indexes via idempotent migrations (`CREATE INDEX IF NOT EXISTS`, `Down` = `DROP INDEX IF EXISTS`), one migration per index. Nothing that can break the legacy app (no unique constraints, renames, type changes, new NOT NULL without default, dropping). Apply only after EXPLAIN/measurement proves a gain; log each here: table, columns, query, measured gain, migration name.

Candidates to evaluate in Phase 6: `Round(Venue, Number, Date)` (event history), `Round(Status, Date)` (home), `Participant(LastName, FirstName)`, `Team(Sport, Name)`.

Deferred until the legacy app is retired (list, do not apply): unique constraints for (Season, Number), Team (Sport, Name), Participant names; proper `DateOnly`/`bool` types; dropping legacy `User` hash columns.

## 4c. Team history on season-entry create (new)

On `SeasonEntries/Create` (and edit), team picker lists only teams of the season's sport. On selecting a team show a panel: original name (`Team.Name`), name in the latest season it was entered (with season label), and all entries newest first (season label, name used). Team search also matches every one of those names (this is the **only** picker with all-names search; round/season winning-team pickers list the season's entries and match only that season's entry name). If the Name field is empty it is prefilled with the latest-season name (fallback `Team.Name`); the user can overwrite. Data from the cache (team -> entries list), no query per selection beyond a cache revalidation.

## 4d. Cache freshness

See AGENTS.md "Caching". Write paths invalidate in-process; legacy writes are picked up by `MaxAge` 30 s + revalidate on form open. Tests required.

## 4e. New functionality vs legacy

Full event history (paged, grouped by year, no 15 cap); team history on season-entry; fuzzy search over all team names (season-entry create only) and last-name-first participants; dark theme + per-sport accent; responsive/phone layout; POST for MarkReadyToWatch and logout; atomic winner updates and validate-before-mutate; password rehash on login + change-password page for `ForceChangePassword`; health endpoint (`/health`); migrate-on-start; aggregated one-query home.

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
- Event history in legacy is N+1 per next-up card, capped at 15, no pagination: new = one query per selected round, uncapped, paged/virtualized.

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
- Common UI (or Web shared): `FuzzySelect<T>`, `FuzzyMultiSelect<T>`, `DateField`, allocation-free `FuzzyMatcher` + benchmarks + unit tests. See section 4a.
- Web: `Program.cs`, MudBlazor layout (drawer nav on desktop, hamburger/bottom nav on phone), dark-default theme with light toggle (cookie), per-sport accent helper, health endpoint `/health`, Options validation, `DatabaseMigrator`.
- Dockerfile (+ `.dockerignore`), `docker-compose.yml` for local MariaDB. Replace legacy `Docker_Run.cmd` with a bash `docker-run.sh` for the new app (leave legacy script). Legacy app and new app must be runnable side by side (different ports, same DB).
- Packages: latest stable at time of work (MudBlazor 9.x, FluentValidation, Vogen, Roslynator, CSharpier, Testcontainers.MariaDb, bUnit, Playwright, NetArchTest, BenchmarkDotNet). Spike first: Pomelo 9.0.0 + EF Core 9 on net10 against MariaDB (connect, query, `GET_LOCK`, `IF NOT EXISTS` DDL, DateOnly, char(2), decimal). If it fails, switch to `MySql.EntityFrameworkCore` 10.x and record it here.
- Exit: solution builds clean, `/health` OK in Docker, empty authorized home.

### Phase 1 – Infrastructure & baseline `[x]` (done 2026-10-24)
- Scaffold entities + `MotorsportsDbContext` from the real schema (mirror `Models/MotorsportsContext.cs`, same table/column names, lengths, FKs, types). Use value converters for Vogen IDs, `DateOnly` for Round.Date.
- Compare model against the real DB (`dotnet ef migrations add Baseline` must produce an **empty** diff for existing tables; delete or ignore baseline create statements, use `--no-create` approach: baseline migration with empty Up). Confirm with `dotnet ef migrations script`.
- Testcontainers MariaDB fixture loads schema-only DDL for the legacy tables and synthetic seed data; no dump rows are included.
- Verify DateOnly, decimal, char(2), and binary mappings against MariaDB; verify startup migration twice, concurrently, and with the auth columns already present.
- Characteristics tests for the legacy rules in section 3 written against the **new** handlers as slices land (red first).
- Exit: DbContext matches DB; fixture works.

### Phase 2 – Auth `[ ]`
Section 5. Login page (MudBlazor, mobile friendly), logout (POST), access denied, default authorize policy, local-return-url check, change-password page for `ForceChangePassword`.

### Phase 3 – Reference data slices `[ ]`
Sports, Venues, Participants (list with search/filter, details, create, edit; no delete). Teams: same plus delete only when the team has no season entries (sport read-only on edit). Shared UI building blocks in Web or Common (e.g. responsive list, form layout, country select, confirm dialog, conflict alert). Country, Status lookups cached.

### Phase 4 – Seasons & SeasonEntries `[ ]`
Season CRUD + winners + winning team + delete rule. SeasonEntries per season. Date handling with the shared `DateField`; winners via `FuzzyMultiSelect` (section 4a).

### Phase 5 – Rounds `[ ]`
Round list per season, details, create, edit (rain select, rating whole number 0..10 (as legacy), status, venue autocomplete, winning team via `FuzzySelect` limited to season entries, winners via `FuzzyMultiSelect` max 4), delete, `MarkReadyToWatch` POST (idempotent, rejects invalid transition). Event history component (shared within slice).

### Phase 6 – Home `[ ]`
Single `GetHomeOverview` handler: very-next-up, next-up per sport, rounds to acquire, latest seasons. Event history: full history for very-next-up/selected event, virtualized/paged, grouped by year, collapsible. Sport logos via static assets (copy `wwwroot/img/*.png`; fallback `notfound.png`). Mobile layout first. Event history comes from one query per selected round (no limit), streamed/paged in the UI; sport accent via logo + thin border.

### Phase 7 – Hardening & parity `[ ]`
- Walk the section 2 table, tick every row after a manual phone-width + desktop check.
- Data-integrity pass: concurrency, FK/unique conflict handling, nullability vs. DB.
- Security pass: antiforgery, cookies, headers, rate limiting on login, no secrets in repo.
- Performance pass: log SQL for Home, assert query count in a test.
- Docs: README (run, config, docker, migration note).
- Cut-over (manual, by user): deploy, verify, then remove the legacy project, `Build` (Cake/IIS), `Db` sqlproj/Azure backups from the solution. Do not do this without asking.

## 7. Resolved decisions and remaining open points

Resolved:
- Provider: Pomelo 9.0.0 + EF Core 9 (see section 1), spike in Phase 0.
- `src/Db/*.sql` backups stay in the repo; the user updates them manually. Agents ignore them except as read-only schema reference.
- Legacy app keeps running against the same DB for the first weeks; additive-only schema changes, legacy password columns untouched.
- Theme: dark default, subtle per-sport branding (logo + accent).

Open (ask the user when reached):
- Resolved: first release is `2.0.0`; minimum round date 2026-01-01 for new/changed dates only. Round/participant uniqueness rules are in section 3.

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
- Real column widths differ from the legacy EF model: `Venue.Name` and `Round.Venue` are `varchar(100)` (legacy model said 50); `Round.Note` is `text`; `Round.Number` is `smallint`; `Status.Step` is `tinyint`. Validation max lengths must follow the **DB**: Participant Title 10, First/Last 100, Team Name 50, Sport Name 50 / FullName 100, Season Label 100, SeasonEntry Name 50, Round Name 100, Venue Name 100.
- Real FK constraint names are `<Table>_<Col>_FK` (e.g. `Round_Season_FK`), not the legacy model's `FK_Season_Round`. Nothing depends on them; do not hardcode either.
- DB collation is `utf8mb4_uca1400_ai_ci` (case- and accent-insensitive). So plain `==` in LINQ on string columns is already case/accent-insensitive in SQL, which matches the uniqueness rules. Do not use `ToLower()` in queries (defeats indexes).
- The dump has 0 rows in `User`; there are no real credentials to test with. Auth tests must seed users with a legacy-style hash generated in the test.
- `dotnet ef migrations add` generates a full `CreateTable` baseline. Hand-edit `Baseline.Up/Down` to be empty (done). The second migration `AddUserAuthColumns` was scaffolded as an empty diff (the entity already holds the columns, so the snapshot is right) and its `Up/Down` were filled by hand with `ADD COLUMN IF NOT EXISTS` / `DROP COLUMN IF EXISTS`.
- EF Core's own `Migrate()` already takes a named lock (`__<db>_EFMigrationsLock`). `DatabaseMigrator` still holds `motorsports-migrate` on the same connection as an explicit guard and skips work when nothing is pending.
- `EnableRetryOnFailure` is **not** enabled: it forbids user-started transactions unless wrapped in `CreateExecutionStrategy().ExecuteAsync`, which conflicts with the validate-in-transaction pattern. Revisit only with a concrete need.
- `Directory.Build.props`/`Directory.Packages.props` sit in `src/`, so they would also apply to the legacy projects. They are guarded with `IsLegacyProject`; the legacy csproj still pins its own versions.
- Provider spike (Phase 0): Pomelo 9.0.0 + EF Core 9.0.21 on net10.0 connects to MariaDB 11.7, runs `GET_LOCK`/`RELEASE_LOCK`, `CREATE TABLE IF NOT EXISTS`, and applies the empty baseline. Phase 1 confirmed typed reads against MariaDB; provider stays Pomelo.
- Phase 1 MariaDB reads confirmed `DateOnly`, decimal, `char(2)`, and binary values. The schema fixture exposed that legacy password bytes need `varbinary(50)` mapping and Status.Step is signed `tinyint`; both are aligned in the entity model. Country's two smallint columns are now represented as well.
- Analyzers: `AnalysisLevel=latest` (not `latest-recommended`, too noisy). Disabled with reasons in `.editorconfig`: CA1716, CA1000 (public `Result<T>` API), RCS1181, RCS1161, RCS1043 (extension classes must be `partial`), RCS1201 (EF fluent config).
- `InvariantGlobalization=true` cannot rely on `string.Normalize`; `FuzzyMatcher.Normalize` uses its own Latin-1/Latin Extended-A fold tables. Verify on Linux in the Docker image.
- Vogen is deferred (plain int/string keys). Decision recorded in AGENTS.md.
- Template cleanup: bootstrap, Counter, Weather, NavMenu css removed; the Web project uses MudBlazor only.
- FIXED: `GET /` returned 500 because `TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels")` throws on Windows. `UserClock.ResolveZone` falls back to the local zone. Linux/Docker (tzdata) resolves IANA ids directly (verified in the container).
- CSharpier does **not** support an `excludes:` key in `.csharpierrc.yaml`. Exclusions live in `.csharpierignore` (legacy app, `src/Build`, Migrations, bin, obj). Before this was fixed, `csharpier format .` reformatted the legacy projects; they were restored with `git checkout`. Always check `git status src/Motorsports.Scaffolding.Core src/Build` is clean after formatting.
- .NET 10 SDK + xunit.v3 (Microsoft.Testing.Platform): VSTest mode is refused. `global.json` has `"test": {"runner": "Microsoft.Testing.Platform"}`; run `dotnet test --solution src/Motorsports.slnx`. Test projects need `<OutputType>Exe</OutputType>` and `<Using Include="Xunit" />`.
- Test projects live in `src/tests/` (not the repo-root `tests/`) so `Directory.Build.props` applies to them. `Motorsports.CharacteristicsTests` is created in Phase 1 (needs Testcontainers).
- The `create` tool and a `Remove-Item`/`New-Item` in the same parallel batch race; create directories first, in a separate call.
- MudBlazor 9: the `PickerActions` context is `MudPicker<DateTime?>`, not `MudDatePicker`.
- Docker: `src/Motorsports.Web/Dockerfile` (build context = repo root: `docker build -f src/Motorsports.Web/Dockerfile -t motorsports-web:latest .`), base `10.0-noble`, installs tzdata + curl, healthcheck on `/health`, listens on 8080. Verified: container starts against the dev DB, `/` 200, `/health` Healthy, second start logs "Database is up to date" (idempotent migrations), `/usr/share/zoneinfo/Europe/Brussels` exists. From a container, reach the host DB via `host.docker.internal`.
- The legacy csproj still builds (0 warnings, 0 errors) with the new `global.json`, `version.props` and `Directory.*.props`.
- Interactive behaviour of `FuzzySelect`/`FuzzyMultiSelect`/`DateField` is only verified by static prerender (HTTP 200, markup present). Click/typing behaviour needs a browser test (Playwright) or manual check in Phase 3/4.

## 10. Handover (rewrite at the end of every phase)

Date: 2026-10-24. Phases 0 and 1 are done. Changes remain in the working tree; no commit was created.

State:
- `Motorsports.CharacteristicsTests` now runs on Testcontainers MariaDB 11.7.2 using `Fixtures/LegacySchema.sql` (DDL only) and synthetic data; it covers typed reads and migration idempotency, concurrent startup, and pre-existing auth columns.
- EF Core's migration snapshot matches the model; the baseline remains empty and the auth migration is self-guarding.
- Projects: Common, Infrastructure, Web, `tests/Motorsports.UnitTests` (FuzzyMatcher, UserClock), `tests/Motorsports.ArchitectureTests` (layering), `tests/Motorsports.Benchmarks` (BenchmarkDotNet, run with `dotnet run -c Release --project src/tests/Motorsports.Benchmarks`), and `tests/Motorsports.CharacteristicsTests`.
- Shared UI in `Motorsports.Web/Components/Shared`: `FuzzySelect<T>`, `FuzzyMultiSelect<T>` (chips, selected items hidden from suggestions), `DateField` (editable, `yyyy-MM-dd` mask, Today button, MinDate/MaxDate), `SportAccent` (colour by sport name, subtle left border).
- Docker image, `.dockerignore` and `docker-compose/motorsports-web/docker-compose.yml` exist and are verified (see section 9).
- Dev DB `motorsports_web_dev` in the `mariadb` container; connection string in user-secrets of `Motorsports.Web`. Dev URL http://localhost:4100.

Known gaps / not done:
- No `docker-run.sh` (the compose file replaces it; add only if the user asks).
- Slice features and auth are still pending; characteristics coverage for business rules will be added as their handlers land.
- The nav menu only has Home; `Home` is a placeholder; no auth yet, so all pages are currently anonymous.
- Component interaction is not browser-tested (see section 9).

Next: Phase 2 (Auth), then Phase 3 (reference data slices), per section 6. No CI/CD work, and never touch `src/Db/*` dumps.

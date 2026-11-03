# Playwright full-regression plan

Status: scope approved; implementation not started.
Date: 2026-11-03.
Handoff: scope decisions are resolved; a new agent can implement this plan.

## 1. Approved decisions

- Add a real-browser Playwright regression suite for the complete current UI: happy paths, validation, boundaries,
  failure handling, concurrency, and recovery. Full coverage takes priority over runtime.
- Browser matrix: desktop Chromium, Firefox, and WebKit; Android Chromium and iPhone WebKit emulation.
- Primary targets: Pixel 10 Pro and a 1440p desktop browser. Retain the application's 360px minimum-width guard.
- Screenshots are optional diagnostics, not a per-test requirement; capture only when useful. No screenshot baselines
  or video. Store screenshots, traces, and other artifacts only under the system TEMP directory; delete artifacts for
  successful tests/runs and retain failure artifacts there for diagnosis. Disable screenshots in traces; a useful
  screenshot may be captured separately. Artifacts contain synthetic data only.
- Report unrelated existing defects for user approval before changing application behavior. Do not silently skip,
  weaken assertions, or mark broken behavior as correct to obtain a green suite.
- No CI/CD work, production access, real application data, or deployment changes.
- This task currently delivers this plan only, not the suite.

## 2. Source of truth and starting context

Read `..\AGENTS.md`, `PLAN.md`, and the current source before implementing. This plan uses the existing UI inventory,
business rules, follow-ups, Issues encountered, and Handover in `PLAN.md`, including the season-label and logout fixes
of 2026-11-03.

`PLAN.md` contains historical statements superseded by later corrections. Do not reintroduce legacy behavior:

- Legacy authentication is retired; seed Identity `PasswordHashV3`, not legacy password hashes.
- Production Data Protection keys require certificate encryption; old plaintext-key decisions are superseded.
- Home has no Latest Seasons block. Its populated query path currently uses four reads, not one.
- Live list search supersedes the older Enter/blur-only description.
- Status on Round create/edit is an ordered, non-editable select; searchable reference data uses fuzzy controls.
- Season Label is optional and clearable. Native constraint validation must not prevent inline server errors.
- Logout explicitly validates antiforgery; do not rely on middleware alone.
- Display dates use two-letter weekday plus English long date; editable fields retain the current DateField contract.
- Participant flags on round chips/options supersede earlier flag-free label descriptions; search keys stay flag-free.
- Home history has a ten-year window and a 50-record cap. Confirm the separate round-history contract in source.

Where current code and binding rules disagree, record the discrepancy and seek approval for an unrelated defect.
Do not treat every old inventory item or proposed cascade as an implemented feature.

Existing test infrastructure:

- `tests\Motorsports.CharacteristicsTests`: xunit.v3, Microsoft.Testing.Platform, bUnit, WebApplicationFactory,
  Testcontainers MariaDB, synthetic schema/data, auth helpers, and certificate fixtures.
- `tests\browser\reconnect.test.cjs`: dependency-free recovery lifecycle/contrast regressions; keep these.
- Unit and architecture suites remain complementary; browser tests do not replace them.
- `Directory.Packages.props` already includes a Playwright package version; inspect before adding dependencies.
- No full Playwright browser workflow suite was found during planning.

## 3. Harness and execution design

- [ ] Create `tests\Motorsports.PlaywrightTests` in `Motorsports.slnx`, following current .NET 10/xunit.v3/MTP patterns.
  Prefer C# Playwright and central package management rather than introducing a second package ecosystem.
- [ ] Run the actual application on loopback Kestrel. Browser access must include Blazor circuits, real cookies,
  antiforgery, JS, CSS, images, and navigation; an in-memory TestServer alone is insufficient.
- [ ] Use disposable Testcontainers MariaDB 12.3.3 and normal startup migrations, with schema-only legacy fixture
  setup if needed. No connections to the local restored database or production.
- [ ] Reuse suitable infrastructure patterns without coupling the new project to test classes. Extract a narrowly
  scoped shared test helper only if justified; do not duplicate large fixture frameworks.
- [ ] Use an xUnit class fixture as the default application/database lifecycle, with isolated data/accounts and
  browser contexts per test. Cookie contexts, throttle counters, caches, and destructive cases must not contaminate
  other tests. Database resets cannot race active circuits. Use a fully unique app/database fixture only for cases
  that require isolation beyond the class fixture (for example restart or destructive transport scenarios).
- [ ] Seed through test infrastructure, not public application-only test endpoints. Generate synthetic users,
  valid image samples, related histories, and enough rows for paging/history limits.
- [ ] Use a controllable server TimeProvider for date-boundary scenarios; browser clock alone does not control
  UserClock/server time. Configure browser time zones explicitly. Drive cooldown/expiry deterministically where safe.
- [ ] Keep normal startup/auth behavior. Use dedicated configurations for throttling, secure-cookie/HTTPS,
  production-certificate, restart, and circuit-expiry cases instead of weakening the whole suite.
- [ ] Authenticate shared setup once only where authentication is not under test; keep real UI login/logout flows.
  Isolate password-change and forced-change accounts. Never persist credentials/storage state into tracked files.
- [ ] Use role/label/text locators, scoped to the relevant row/card/dialog. Add stable test IDs only when semantic
  locators cannot reliably distinguish controls; never use positional selectors as the primary contract.
- [ ] Use Playwright assertions and explicit observable readiness, not fixed sleeps or network-idle assumptions.
  Blazor holds long-lived connections. Verify interactivity, not just SSR content.
- [ ] Fail on unexpected page errors, unhandled Blazor errors, relevant failed requests, and CSP/bootstrap failures.
  Narrowly classify expected negative-test failures; do not blanket-ignore console/network errors.
- [ ] On failure retain text diagnostics and, when useful, DOM/network traces or standalone screenshots; disable
  screenshots inside traces and disable video. Keep all artifacts in TEMP, delete successful-run artifacts, and
  retain failed-run artifacts for diagnosis. Only synthetic data may appear. Do not print image bodies, hashes, keys,
  or secrets.
- [ ] Document browser installation, Docker prerequisites, isolated ports, full and targeted commands, artifact
  locations, and resource cleanup. Use exact commands verified against the installed MTP runner.
- [ ] Dispose only suite-owned processes/containers. Do not stop the user's running Debug application.

### Browser and viewport projects

| Project | Configuration and required use |
|---|---|
| Desktop Chromium / Firefox / WebKit | Primary content viewport 2560 x 1440 CSS pixels, DPR 1. Full workflows in each engine. This specifies viewport, not outer-window size or monitor resolution. |
| Pixel 10 Pro, Chromium | Primary mobile target, touch/mobile behavior, portrait and key landscape flows. User-provided physical display: 1280 x 2856 pixels, 20:9 (~495 ppi). Resolve an official Playwright descriptor if available; otherwise define and document a custom profile, including CSS viewport, screen, DPR, touch, mobile flag, and user agent. Do not silently substitute another Pixel model. |
| iPhone, WebKit | Document the selected supported iPhone descriptor. Full applicable mobile workflows; this is emulation, not real iOS certification. |
| Narrow/short viewport guard | 360 x 640 CSS px portrait; navigation, grids, all forms, popovers, dialogs, validation and recovery controls remain usable. |

The Pixel resolution and density above are physical-display facts, not CSS viewport/DPR values. Verify the browser
profile against a reliable source before claiming an exact device emulation. If exact CSS viewport/DPR values are
unavailable, clearly label the custom emulation and record the limitation rather than blocking implementation.

Run the full application regression coverage on every centrally configured browser profile: desktop Chromium,
Firefox, and WebKit; Pixel 10 Pro Chromium; and iPhone WebKit. Also run the narrow/short viewport layout guard.
Centralize profile expansion in the test harness/fixture; do not repeat per-test InlineData declarations. Restrict a
case to a specific profile only when it reproduces a device-specific bug, and record that exception in the matrix.
Browser-specific limitations must be explicit, not disguised as passing coverage. Viewport-only Firefox coverage is
not Android emulation. Use semantic/layout assertions (visibility, clipping, element bounds, usable scroll
containers, focus and reachability), not pixel snapshots. Permit intentional grid scrolling; reject inaccessible
actions or unintended page-wide overflow. Check both themes for shared controls, auth forms, errors, dialogs, and
recovery.

## 4. Coverage matrix to implement

Before writing cases, enumerate every current `@page`, form, action, validator rule, shared control, and linked detail
surface. Assign stable case IDs to the groups below. Expand parameterized data into named cases where diagnosis benefits.
For each case record fixtures, expected outcome, browser/viewport coverage, implementation status, and test method.
Keep this living matrix in this file; source edits must not leave the inventory stale.

| IDs | Functionality | Happy paths and edge cases |
|---|---|---|
| AUTH | Login/session | Valid login, safe return URL including query, required/invalid credentials, generic failures for unknown/deleted users, forced password change, protected deep links, expiry/invalidation, refresh/back navigation without protected content disclosure. Reject absolute, protocol-relative, slash-backslash, and scheme return URLs. |
| AUTH-CHANGE | Password change | Account-menu navigation, MainLayout preserved, correct change and subsequent login, incorrect current password, required/length/policy/mismatch errors, forced-change completion, cookie/security-stamp behavior per current contract. |
| AUTH-POST | Logout/antiforgery | Real menu POST logout, session ended, GET cannot mutate; missing/empty/malformed tokens are rejected without ending the session. Login/change-password missing/invalid-token requests are rejected without ending the session. HTTP status assertions belong to characteristics tests. Use browser-context requests for protocol cases that have no visible UI. |
| AUTH-LIMIT | Abuse protection | Visible 429/retry behavior, account/source limits, protected device cookie, tampered cookie, isolated-account recovery after cooldown, successful login. Preserve deeper shared-budget/concurrency/fail-closed characteristics tests rather than reproducing every DB case through a browser. |
| SHELL | Navigation/layout | Every routed page and related link, Home brand link, drawer/account menu, desktop and touch navigation, footer/version, unknown route/record, empty views, loading completion, dark default, immediate theme switch and persisted choice across reload/login/password pages. |
| HOME | Dashboard | Very-next-up, per-sport earliest scheduled/ready selection, due acquisition cards, future countdown/no Finish action, empty sports/rounds/history, overdue rounds retained, same-date ties, non-championship candidates, links and Ready to watch changes reflected after reload. |
| SPORT | Sports | List/details/create/edit, required name, optional FullName, trim/max lengths, duplicate/self-unchanged/case-accent collisions, rename with and without dependents, case/accent-only rename, canonical keys, escaped name routes, image preservation/removal/replacement and concurrency. No delete UI. |
| IMAGE | Sport images | Valid PNG/JPEG/WebP, 2 MB boundary and over-limit, invalid signatures/spoofed MIME/extensions, missing/unrecognized image fallback, authorized endpoint, no inline base64, centered preview and image available after reload. Test actual current size semantics. |
| VENUE | Venues | Search/clear/Enter/debounce, no results, paging/deep links, list/details/history, create/edit, required country/name, trim/max lengths, duplicates, Unicode/escaped keys, rename propagation including case/accent-only, flags and canonical country selection. No delete UI. |
| TEAM | Teams | List/search/paging/details/history/create/edit, required sport/country/name, trim/max lengths, duplicate within sport but allowed across sports, self-unchanged, read-only sport, allowed delete/cancel and blocked delete with entries, updated country/history after reload. |
| PARTICIPANT | Participants | List/search/paging/order/details/win links, create/edit, required/optional fields as current validators define, trim/max lengths, identity uniqueness across Title/First/Last/Country, same name in different countries, flags, diacritics and no delete UI. |
| SEASON | Seasons | Grouped/per-sport lists, label/date fallback and ordering including no rounds, details/computed dates, create required sport/optional label, edit/clear/trim label, max length, immutable sport, winning team from own entries, winner replacement and clearing, unlimited season-winner selection per current contract. |
| SEASON-DELETE | Season deletion | Confirm related counts, cancel, delete with zero rounds including entries/winners, block with rounds, stale/deleted confirmation target and race with new dependencies; no partial deletion. |
| ENTRY | Season entries | List/order/details/create/edit/delete, sport-filtered teams, required/unique team and name rules, history panel, original/latest labels and fallback, newest-first tied/null dates, prefill only when empty, preserve custom name, fixed season/team on edit, duplicate entry rejection, winning-round/season deletion guards. |
| ROUND | Round list/create/edit | Date-ascending list and scroll to latest, details/related links, no-round/default latest-date and next-number create defaults, required date/venue/status, optional name/note, canonical FKs, number 0..99, repeated zero but unique positive numbers, unchanged older date allowed, new/changed date minimum 2026-01-01. |
| ROUND-RESULT | Round results/delete | Whole rating 0..10 including explicit zero/null and scheduled/ready clearing rule, defined rain values/null, notes 1024 boundary, own-season winning team, distinct/existing participants max four, replace/clear winners, delete allowed versus either winner type blocking, missing/stale records and no partial writes. |
| FINISH | Finish Round | Reachable from Home; schedule/season read-only, result fields/history/related editing links present. Finished requires rating/rain/team/1..4 winners; Stopped requires rating/rain; Postponed/Cancelled require nonblank note. Validate supplied optional fields, preserve schedule, distinguish missing rating from zero, save correct status and reflect on Home/details. |
| READY | Ready to watch | UI action uses antiforgery POST, eligible scheduled round updates, repeated action is idempotent, noneligible status unchanged, unknown/stale target handled gracefully and safe refresh/back behavior. |
| HISTORY | Event/detail histories | Home ten-year boundary and 50 cap, round history's own date/cap contract, same venue/sport, championship versus zero-number filtering, Step > 1 including postponed/cancelled, exclude self/future entries, newest first, full results beyond initial viewport, links, team season-name fallback, sorted winners and null formatting. |
| PICKER | Single fuzzy select | Full query matching, case/diacritic/typo/token-order/last-name-first matching and ranking, one Enter selection immediately after typing, click/touch, arrow/escape/tab/focus, clear/null, no match, grouped options, bounded result count, safe null ToStringFunc. |
| MULTI | Multi fuzzy select | Add once, no duplicate, chip remove/backspace/clear, four-round-winner cap and recovery after removal, season selection not incorrectly capped, same-name participant disambiguation, country flags with flag-free search keys, keyboard and touch. |
| CONTEXT | Picker context/freshness | Entry team picker alone searches all historical team/entry names; winning-team pickers match only own-season entry names, never unrelated aliases. New/renamed reference data appears after another-tab write and reopening the form; external synthetic DB changes become visible after cache expiry/revalidation. |
| DATE | Date control/time zones | Type accepted current formats, mask/incomplete/invalid input and correction, calendar/today/clear behavior, mobile dialog, minimum boundary, unchanged pre-2026 round, date-only roundtrip, browser-zone today/default/countdown around UTC midnight and fallback when zone detection is unavailable. |
| CONFLICT | Concurrency | Two contexts edit each of Sport, Venue, Team, Participant, Season, SeasonEntry, Round; stale save keeps input and exposes Reload/Show differences; winner/image-only edits participate. Delete/rename since load, dependency races, conflict without overwrite, reload then intentional resubmit. Assert 409 only on endpoints exposing HTTP results; interactive circuit validation need not produce a document-level 409. |
| RECOVERY | Circuit/network | Temporary offline/transport loss, visible-online retries, foreground/network return, single-flight recovery, retained unsaved edits only when the retained circuit reconnects, and expected draft loss after circuit expiry/eviction or restart. No replay/duplicate write; recovery dialog within viewport, readable themed controls, keyboard focus. Use actual circuit disruption, not only injected CSS classes. |
| ERRORS | Failure surfaces | Friendly missing-record and production error surfaces, request ID where applicable, no stack traces/secrets, unexpected runtime/CSP/script errors fail tests, form errors clear/correct and resubmit without losing unrelated input. Isolated DB failure behavior is characterized, not silently improved without approval. |

Across writable fields test empty, whitespace, exact maximum, maximum plus one, and trimming where meaningful.
For uniqueness include case/accent equivalence and literal `%`/`_` to guard against accidental LIKE semantics.
Verify persistence by reload/reopen; do not count a snackbar alone as proof of a successful write.
UI-unrepresentable tampering (invalid enum/FK/duplicate IDs/read-only fields) remains covered by characteristics or
endpoint tests; add missing companion cases rather than pretending a browser control can submit those values.

## 5. Historical bug regression ledger

These are explicit additions from `PLAN.md`, not optional exploratory checks. Link each row to concrete test IDs/methods.
Infrastructure-only regressions remain in their correct test layer, with a browser-visible smoke check where applicable.

| Source in PLAN.md | Regression guard |
|---|---|
| Season-label follow-up, 2026-11-03 | Blank Label create/clear succeeds; missing required Sport reaches inline validation instead of native browser submit blocking; winners still persist when clearing Label. |
| Logout follow-up, 2026-11-03 | Missing, empty, malformed antiforgery POSTs and GET are rejected without ending the session; real account-menu token-bearing logout ends the session. |
| 4h and theme Issues | Toggle updates both cookie and `html[data-theme]` immediately; links and embedded password form track dark/light without reload, then persist after reload. |
| Picker/list search follow-ups | Fast typing plus a single Enter selects the current match; live search works without blur, Enter applies immediately, clear resets results, query escaping works and search navigation does not flood browser history. |
| Static Home Virtualize issue | Seed more than five history items; all expected items through the cap remain reachable, including last item. No assertion limited to the first viewport. |
| Vogen team-history tie crash | Equal/null season dates render without error, newest-ID tie-breaker drives history/latest label/prefill, and an unrelated custom name is not overwritten. |
| EF translation issues on lists/Home/search | Populated Season lists, Home, venue/team/participant search, converted-key routes and contextual team lookup render and operate against real MariaDB. Keep translation/query-count characteristics guards. |
| UTF-8 restoration/presentation follow-ups | Synthetic accented venue/participant/team names survive create, edit, search, route navigation and reload without replacement characters. Assert browser text, not raw HTML encoding. Do not attempt to repair historical stored question marks. |
| Logo blob/endpoint follow-ups | Lists/forms/Home use authorized image URLs/fallbacks, not base64; bootstrap and page rendering remain healthy with images. Verify endpoint cache contract with context requests where useful. |
| Sport summary correction | Latest completed season selection excludes newer active seasons, handles no winner data and unlabeled year fallback; do not restore obsolete winner-required/roundless-season assumptions. |
| Presentation corrections | Rain uses enum labels, ratings integer `/ 10`, nullable values em dash, optional round names format cleanly, two-letter weekday dates, country flags in related winner/history surfaces. Do not assert OS-specific flag glyph rendering. |
| New-tab editing links and cache issues | Related edit links open the intended tab; after committed writes, original form refresh/revalidation sees lookup changes without cross-season search leakage. |
| Menu popover/logout form correction | Logout works after opening/closing account popover and across themes/mobile; antiforgery form does not depend on closed popover markup. |
| Blazor bootstrap authorization and Docker publish failures | Anonymous and signed-in framework/MudBlazor script requests return JavaScript, not login HTML/404; verify actual published-host browser interaction, not only development assets. |
| CSP nonce assertion issue | Browser bootstrap/import map executes under enforced CSP; companion header assertions HTML-decode nonce attributes when comparing encoded markup, including `+`. No weakening CSP. |
| Mobile retention/retry/scoped stylesheet defects | Retained circuit works within its configured retention window; already-visible exhausted retries recover, online/foreground/page restoration trigger recovery, scoped stylesheet loads, dialog state hides/shows correctly, and drafts are lost after expiry/eviction/restart. |
| Reconnect theme/position/contrast correction | Dialog centered and scrollable at primary/narrow/short viewports, controls and fallback dark/light text readable (4.5:1 where applicable), focus visible. Use computed styles/geometry, not screenshots. |
| Windows IANA-zone failure | Home starts and zone fallback works on Windows; Linux published smoke resolves intended IANA zones. Server/browser midnight tests must prove the user-date contract. |
| MariaDB connection loss after checkout | Preserve existing pool-expiry/killed-idle connection tests. Isolated browser disruption records actual read/circuit failure and subsequent recovery. Existing unhandled transport failure is a known limitation; report before altering retries/transactions or error behavior. |
| Auth/key-ring/backup follow-ups | Restart with preserved keys keeps session; empty-ring restore invalidates old cookie and permits fresh login; deleted users cannot retain sessions. Keep certificate encryption/missing-certificate and dump/restore tests at characteristics level; browser smoke uses synthetic keys/accounts only. |
| RESTRICT rename/delete/validation defects in 4f | Dependent renames and zero-round season deletion are atomic; blocked deletion preserves children; max-length/submitted-value errors are friendly; uniqueness uses literal equality. No cascades added. |
| Migration metadata/idempotency and secure-cookie issues | Retain migration discovery/repeated/concurrent/preexisting-schema tests and HTTPS secure-cookie assertions. Published-host smoke starts with migrated disposable DB and real assets; no need to drive DDL through UI. |

Other Issues entries describe build tooling, old provider decisions, benchmark methodology, configuration, or retired
legacy code. Do not invent browser cases for those. Maintain existing build, architecture, migration, query-count,
backup, and Node guards as the corresponding regression layer.

## 6. Implementation phases and exit gates

### A. Inventory and foundation

- [ ] Reconcile the route/action/validator inventory with source; expand section 4 into executable named cases.
- [ ] Link every section 5 regression to a planned case or named existing companion test.
- [ ] Build harness and primary browser profiles; verify real login, interactive picker, write/reload and disposal.
- [ ] Confirm Pixel descriptor provenance/limitations; do not ask the user to repeat already-approved scope.

### B. Functional workflows

- [ ] Auth, shell/theme, reference data and image workflows.
- [ ] Seasons, season entries, rounds, Finish outcomes, Home and related histories.
- [ ] Shared controls, boundary data, same-name/Unicode/escaped-route cases.
- [ ] Run each implemented group on every centrally configured browser profile while developing, not only at the end.

### C. Concurrency, recovery and published app

- [ ] Cross-tab conflicts/cache freshness and deletion/rename races.
- [ ] Real circuit loss, expiry/restart, user-zone date boundaries and themed recovery controls.
- [ ] Published-host asset/CSP/interactivity smoke; optional production-shaped HTTPS fixture for cookie/key behavior.
- [ ] Full approved engine matrix and 360px/landscape layout guards.

### D. Reliable regression guard and handover

- [ ] Full suite runs from one documented command with deterministic synthetic setup and no manual app preparation.
- [ ] Run the full browser matrix repeatedly from clean data; investigate intermittent failures, never hide them
  with automatic retries. Record genuine environmental prerequisites separately.
- [ ] Every current route/action, validator boundary, business rule, and historical UI defect has mapped coverage;
  document lower-layer companions and real-device limitations honestly. No finite suite guarantees every possible bug.
- [ ] Complete warning-as-error solution build, CSharpier format/check, complete .NET tests including Playwright,
  and existing Node reconnect tests. Do not accidentally run browser cases twice if the solution command includes them.
- [ ] Update README commands, this file's checklist/case mapping, and PLAN.md Issues/Handover as implementation proceeds.
  Do not bump product version merely for planning; follow repository release policy if a release is requested.
- [ ] Leave no suite-owned processes/containers/temp files running; retain only documented failure artifacts.

Commands from repository root (existing gates; add verified Playwright setup/filter commands during implementation):

```powershell
dotnet build src\Motorsports.slnx -warnaserror
dotnet csharpier format .
dotnet csharpier check .
dotnet test --solution src\Motorsports.slnx
node --test src\tests\browser\reconnect.test.cjs
```

## 7. Defect handling and agent handover

When an unrelated existing defect is found, record the case ID, expected/actual behavior, smallest reproduction,
affected targets, and supporting text diagnostics here. Report it to the user for approval before fixing it.
Continue independent coverage where possible. A blocked case remains visibly blocked/failing with a linked explanation;
do not quietly exclude it from the promised regression guard.

New defects caused by test-harness or narrowly related implementation changes must be corrected. Avoid unrelated
refactors, broad retries, public test-only endpoints, and behavior changes intended solely to make tests easier.

Initial handover:

- Done: user-approved scope captured; current PLAN.md business rules, Issues, and follow-ups reviewed and mapped.
- In progress: nothing.
- Next: implement phase A, then follow phases B-D.
- Existing limitation: browser emulation cannot prove real Pixel sleep, OS/network switching, or real iOS behavior.
- Existing known risk: connection loss after checkout can surface as a read error/circuit failure; no retry fix approved.
- Open implementation fact: Pixel 10 Pro physical display metrics are supplied; browser CSS viewport/DPR still need verification.
- No application code, dependencies, databases, tests, or production configuration changed by this planning task.

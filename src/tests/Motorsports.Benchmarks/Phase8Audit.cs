using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Motorsports.Common.Fuzzy;
using Motorsports.Home;
using Motorsports.Infrastructure;
using Motorsports.Infrastructure.Lookups;
using Motorsports.Participants;
using Motorsports.Rounds;
using Motorsports.Teams;
using Motorsports.Venues;
using MySqlConnector;

namespace Motorsports.Benchmarks;

internal static class Phase8Audit
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task RunAsync(string reportPath)
    {
        var connection = new MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ConnectionStrings__Motorsports")
                ?? throw new InvalidOperationException("Set ConnectionStrings__Motorsports before running the audit.")
        );
        if (connection.Database != "motorsports_phase8")
        {
            throw new InvalidOperationException("The destructive audit requires database motorsports_phase8.");
        }

        var probe = new QueryProbe();
        var options = new DbContextOptionsBuilder<MotorsportsDbContext>();
        DatabaseConfiguration.Configure(options, connection.ConnectionString);
        options.AddInterceptors(probe);
        var factory = new PooledDbContextFactory<MotorsportsDbContext>(options.Options);
        var results = new List<object>();
        foreach (var scale in new[] { 1, 10 })
        {
            await SeedAsync(factory, scale);
            try
            {
                await using var unindexed = await factory.CreateDbContextAsync();
                await unindexed.Database.ExecuteSqlRawAsync(
                    "DROP INDEX IF EXISTS `IX_Round_Season_Date_Number` ON `Round`"
                );
                var cache = new LookupCache(
                    factory,
                    TimeProvider.System,
                    Options.Create(new LookupCacheOptions { MaxAge = TimeSpan.FromHours(1) }),
                    NullLogger<LookupCache>.Instance
                );
                var home = new HomeService(factory);
                var rounds = new RoundService(factory, new RoundFormValidator());
                var venues = new VenueService(factory, new VenueFormValidator(), cache);
                var participants = new ParticipantService(factory, new ParticipantFormValidator(), cache);
                var teams = new TeamService(factory, new TeamFormValidator(), cache);
                var workloads = new Dictionary<string, Func<Task<object>>>
                {
                    ["Home"] = async () => await home.GetHomeOverviewAsync(new DateOnly(2026, 10, 26)),
                    ["RoundList"] = async () => await rounds.ListAsync(90),
                    ["VenueHistory"] = async () => (await venues.GetAsync("Circuit 0001"))!,
                    ["ParticipantList"] = async () => await participants.ListAsync(null, 1),
                    ["TeamList"] = async () => await teams.ListAsync(null, 1),
                    ["TeamPickerCold"] = async () =>
                    {
                        cache.Invalidate(LookupKind.Teams);
                        return await cache.GetTeamsAsync();
                    },
                    ["ParticipantPickerCold"] = async () =>
                    {
                        cache.Invalidate(LookupKind.Participants);
                        return await cache.GetParticipantsAsync();
                    },
                    ["TeamPickerHot"] = async () => await cache.GetTeamsAsync(),
                };
                var baseline = new Dictionary<string, Measurement>();
                foreach (var (name, run) in workloads)
                {
                    baseline[name] = await MeasureAsync(factory, probe, run);
                }

                var candidates = new[]
                {
                    new Candidate("Round", "Venue, Number, Date", ["VenueHistory", "Home"]),
                    new Candidate("Round", "Venue, Date, Number", ["VenueHistory", "Home"]),
                    new Candidate("Round", "Status, Date", ["Home"]),
                    new Candidate("Round", "Season, Date, Number", ["RoundList", "TeamPickerCold"]),
                    new Candidate("Participant", "LastName, FirstName", ["ParticipantList", "ParticipantPickerCold"]),
                    new Candidate("Team", "Name, Sport", ["TeamList"]),
                };
                var comparisons = new List<object>();
                foreach (var candidate in candidates)
                {
                    var before = new Dictionary<string, Measurement>();
                    var indexed = new Dictionary<string, Measurement>();
                    var restored = new Dictionary<string, Measurement>();
                    foreach (var name in candidate.Workloads)
                    {
                        before[name] = await MeasureAsync(factory, probe, workloads[name]);
                    }

                    await using var db = await factory.CreateDbContextAsync();
                    // DDL identifiers cannot be parameters; all candidates are fixed literals above.
#pragma warning disable EF1002
                    await db.Database.ExecuteSqlRawAsync(
                        $"CREATE INDEX `IX_Phase8_Audit` ON `{candidate.Table}` ({candidate.Columns})"
                    );
                    try
                    {
                        foreach (var name in candidate.Workloads)
                        {
                            indexed[name] = await MeasureAsync(factory, probe, workloads[name]);
                        }
                    }
                    finally
                    {
                        await db.Database.ExecuteSqlRawAsync($"DROP INDEX `IX_Phase8_Audit` ON `{candidate.Table}`");
                    }
#pragma warning restore EF1002

                    foreach (var name in candidate.Workloads)
                    {
                        restored[name] = await MeasureAsync(factory, probe, workloads[name]);
                        if (
                            before[name].Digest != indexed[name].Digest
                            || before[name].Digest != restored[name].Digest
                            || before[name].Commands != indexed[name].Commands
                            || before[name].Commands != restored[name].Commands
                        )
                        {
                            throw new InvalidOperationException(
                                $"Index changed the {name} result or SQL command count."
                            );
                        }
                    }

                    comparisons.Add(
                        new
                        {
                            candidate,
                            before,
                            indexed,
                            restored,
                        }
                    );
                }

                await RestoreSelectedIndexAsync(factory);
                var selected = new Dictionary<string, Measurement>();
                foreach (var (name, run) in workloads)
                {
                    selected[name] = await MeasureAsync(factory, probe, run);
                    if (
                        selected[name].Digest != baseline[name].Digest
                        || selected[name].Commands != baseline[name].Commands
                    )
                    {
                        throw new InvalidOperationException(
                            $"Selected index changed the {name} result or SQL command count."
                        );
                    }
                }

                var index = new FuzzyIndex<int>([
                    .. (await cache.GetParticipantsAsync()).Select(p =>
                        (p.Id.Value, new[] { FuzzyMatcher.Normalize(p.FirstName + " " + p.LastName) })
                    ),
                ]);
                var fuzzy = await MeasureAsync(factory, probe, () => Task.FromResult<object>(index.Search("racer")));
                await using var metadata = await factory.CreateDbContextAsync();
                results.Add(
                    new
                    {
                        scale,
                        rounds = await metadata.Rounds.CountAsync(),
                        seasons = await metadata.Seasons.CountAsync(),
                        participants = await metadata.Participants.CountAsync(),
                        teams = await metadata.Teams.CountAsync(),
                        entries = await metadata.SeasonEntries.CountAsync(),
                        winners = await metadata.RoundWinners.CountAsync(),
                        baseline,
                        comparisons,
                        selected,
                        fuzzy,
                    }
                );
                await File.WriteAllTextAsync(
                    reportPath,
                    JsonSerializer.Serialize(
                        new
                        {
                            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                            processors = Environment.ProcessorCount,
                            database = (
                                await metadata.Database.SqlQueryRaw<string>("SELECT VERSION() AS Value").ToArrayAsync()
                            ).Single(),
                            samples = 60,
                            warmups = 10,
                            today = "2026-10-26",
                            results,
                        },
                        JsonOptions
                    )
                );
            }
            finally
            {
                await RestoreSelectedIndexAsync(factory);
            }
        }
    }

    private static async Task RestoreSelectedIndexAsync(IDbContextFactory<MotorsportsDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS `IX_Round_Season_Date_Number` ON `Round` (`Season`, `Date`, `Number`)"
        );
    }

    private static async Task SeedAsync(IDbContextFactory<MotorsportsDbContext> factory, int scale)
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.OpenConnectionAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS=0");
            await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `__EFMigrationsHistory`, `DataProtectionKeys`");
            var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "LegacySchema.sql"));
            foreach (
                var sql in schema.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            )
            {
                await db.Database.ExecuteSqlRawAsync(sql);
            }
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS=1");
        }

        await new DatabaseMigrator(factory, NullLogger<DatabaseMigrator>.Instance).MigrateAsync(CancellationToken.None);
        var seed = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Phase8Seed.sql"));
        seed = seed.Replace(
            "__PARTICIPANTS__",
            (214 * scale).ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal
        );
        if (scale == 10)
        {
            seed = seed.Replace("seq_1_to_90", "seq_1_to_900", StringComparison.Ordinal)
                .Replace("seq_1_to_1800", "seq_1_to_18000", StringComparison.Ordinal)
                .Replace("seq_1_to_24", "seq_1_to_240", StringComparison.Ordinal)
                .Replace("seq_1_to_214", "seq_1_to_2140", StringComparison.Ordinal)
                .Replace("seq_1_to_78", "seq_1_to_780", StringComparison.Ordinal);
        }

        foreach (var sql in seed.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        await db.Database.ExecuteSqlRawAsync(
            "ANALYZE TABLE Round, Season, SeasonEntry, Team, Participant, RoundWinner"
        );
    }

    private static async Task<Measurement> MeasureAsync(
        IDbContextFactory<MotorsportsDbContext> factory,
        QueryProbe probe,
        Func<Task<object>> run
    )
    {
        for (var i = 0; i < 10; i++)
        {
            await run();
        }

        probe.Queries.Clear();
        probe.Capture = true;
        object result;
        try
        {
            result = await run();
        }
        finally
        {
            probe.Capture = false;
        }

        var queries = probe.Queries.ToArray();
        // SQL ties have no guaranteed order. Compare full result multisets, retaining duplicate counts.
        var canonical = Canonicalize(JsonSerializer.SerializeToNode(result));
        var digest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonical)));
        var times = new double[60];
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocated = GC.GetTotalAllocatedBytes(true);
        var commands = probe.Count;
        for (var i = 0; i < times.Length; i++)
        {
            var start = Stopwatch.GetTimestamp();
            await run();
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        allocated = GC.GetTotalAllocatedBytes(true) - allocated;
        var count = probe.Count - commands;
        if (count != queries.Length * times.Length)
        {
            throw new InvalidOperationException("SQL command count varied between measured calls.");
        }

        Array.Sort(times);
        var plans = new List<JsonElement>();
        await using var db = await factory.CreateDbContextAsync();
        foreach (var query in queries)
        {
#pragma warning disable EF1003 // query.Sql is EF-generated query text; its values are bound separately.
            var plan = await db
                .Database.SqlQueryRaw<string>("ANALYZE FORMAT=JSON " + query.Sql, query.Parameters)
                .ToArrayAsync();
#pragma warning restore EF1003
            using var json = JsonDocument.Parse(plan.Single());
            plans.Add(json.RootElement.Clone());
        }

        return new Measurement(
            times.Average(),
            times[29],
            times[56],
            allocated / times.Length,
            queries.Length,
            digest,
            plans
        );
    }

    private sealed record Candidate(string Table, string Columns, string[] Workloads);

    private static JsonNode? Canonicalize(JsonNode? node) =>
        node switch
        {
            JsonArray array => new JsonArray(
                array.Select(Canonicalize).OrderBy(item => item?.ToJsonString(), StringComparer.Ordinal).ToArray()
            ),
            JsonObject obj => new JsonObject(
                obj.Select(property => KeyValuePair.Create(property.Key, Canonicalize(property.Value)))
            ),
            _ => node?.DeepClone(),
        };

    // ReSharper disable NotAccessedPositionalProperty.Local -- Metrics are read by System.Text.Json.
    private sealed record Measurement(
        double MeanMs,
        double P50Ms,
        double P95Ms,
        long AllocatedBytes,
        int Commands,
        string Digest,
        List<JsonElement> Plans
    );

    // ReSharper restore NotAccessedPositionalProperty.Local

    private sealed class QueryProbe : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public bool Capture { get; set; }
        public List<Query> Queries { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default
        )
        {
            Count++;
            if (Capture)
            {
                Queries.Add(
                    new Query(
                        command.CommandText,
                        [.. command.Parameters.Cast<MySqlParameter>().Select(parameter => parameter.Clone())]
                    )
                );
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed record Query(string Sql, object[] Parameters);
}

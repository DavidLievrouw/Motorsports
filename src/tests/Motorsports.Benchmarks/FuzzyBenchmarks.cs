using BenchmarkDotNet.Attributes;
using Motorsports.Common.Fuzzy;

namespace Motorsports.Benchmarks;

[MemoryDiagnoser]
public class FuzzyBenchmarks
{
    private FuzzyIndex<int> _index = null!;

    [GlobalSetup]
    public void Setup()
    {
        var entries = new List<(int, string[])>();
        for (var i = 0; i < 3000; i++)
        {
            entries.Add((i, [FuzzyMatcher.Normalize($"Driver{i} Verstappen{i % 37}")]));
        }

        _index = new FuzzyIndex<int>(entries);
    }

    [Benchmark]
    public int SearchLastName() => _index.Search("verst", 50).Count;
}

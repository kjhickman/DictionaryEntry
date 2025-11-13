using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class TryGetEntryBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string MissingKey = "missing";

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 42 } };
    }

    private int? TryLookupTraditional(string key)
    {
        return _dictionary.TryGetValue(key, out var value) ? value : null;
    }

    private int? TryLookupEntry(string key)
    {
        if (_dictionary.Entry(key).TryGetOccupied(out var occupied))
        {
            return occupied.Value();
        }

        return null;
    }

    [Benchmark(Baseline = true)]
    public int? TryLookup_Traditional_Exists() => TryLookupTraditional(ExistingKey);

    [Benchmark]
    public int? TryLookup_Traditional_NotExists() => TryLookupTraditional(MissingKey);

    [Benchmark]
    public int? TryLookup_Entry_Exists() => TryLookupEntry(ExistingKey);

    [Benchmark]
    public int? TryLookup_Entry_NotExists() => TryLookupEntry(MissingKey);
}

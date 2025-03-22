using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("GetAndRemove")]
public class GetAndRemoveBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private int? GetAndRemoveTraditional(string key)
    {
        if (_dictionary.Remove(key, out var value))
        {
            return value;
        }

        return null;
    }

    private int? GetAndRemoveEntry(string key)
    {
        if (_dictionary.Entry(key).TryGetOccupied(out var occupied))
        {
            return occupied.Remove();
        }

        return null;
    }

    [Benchmark]
    public int? GetAndRemove_Traditional_Exists() => GetAndRemoveTraditional(ExistingKey);

    [Benchmark]
    public int? GetAndRemove_Traditional_NotExists() => GetAndRemoveTraditional(NewKey);

    [Benchmark]
    public int? GetAndRemove_Entry_Exists() => GetAndRemoveEntry(ExistingKey);

    [Benchmark]
    public int? GetAndRemove_Entry_NotExists() => GetAndRemoveEntry(NewKey);
}

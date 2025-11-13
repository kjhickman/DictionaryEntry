using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class RetrieveAndRemoveBenchmarks : BenchmarkBase
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
        return _dictionary.Remove(key, out var value) ? value : null;
    }

    private int? GetAndRemoveEntry(string key)
    {
        if (_dictionary.Entry(key).TryGetOccupied(out var occupied))
        {
            return occupied.Remove();
        }

        return null;
    }

    [Benchmark(Baseline = true)]
    public int? GetAndRemove_Traditional_Exists() => GetAndRemoveTraditional(ExistingKey);

    [Benchmark]
    public int? GetAndRemove_Traditional_NotExists() => GetAndRemoveTraditional(NewKey);

    [Benchmark]
    public int? GetAndRemove_Entry_Exists() => GetAndRemoveEntry(ExistingKey);

    [Benchmark]
    public int? GetAndRemove_Entry_NotExists() => GetAndRemoveEntry(NewKey);
}

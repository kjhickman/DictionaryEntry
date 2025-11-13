using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class InitializeAbsentBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private bool InitializeAbsentTraditional(string key)
    {
        return _dictionary.TryAdd(key, 1);
    }

    private bool InitializeAbsentEntryApi(string key)
    {
        if (_dictionary.Entry(key).TryGetVacant(out var vacant))
        {
            vacant.Insert(1);
            return true;
        }

        return false;
    }

    [Benchmark(Baseline = true)]
    public bool InitializeAbsent_Traditional_Exists() => InitializeAbsentTraditional(ExistingKey);

    [Benchmark]
    public bool InitializeAbsent_Traditional_NotExists() => InitializeAbsentTraditional(NewKey);

    [Benchmark]
    public bool InitializeAbsent_Entry_Exists() => InitializeAbsentEntryApi(ExistingKey);

    [Benchmark]
    public bool InitializeAbsent_Entry_NotExists() => InitializeAbsentEntryApi(NewKey);
}

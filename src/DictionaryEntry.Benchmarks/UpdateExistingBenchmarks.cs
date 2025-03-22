using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("UpdateExisting")]
public class UpdateExistingBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void UpdateTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var val))
        {
            val *= 2;
            _dictionary[key] = val;
        }
    }

    private void UpdateEntry(string key)
    {
        _dictionary.Entry(key).AndModify(x => x * 2);
    }

    [Benchmark]
    public void Update_Traditional_Exists() => UpdateTraditional(ExistingKey);

    [Benchmark]
    public void Update_Traditional_NotExists() => UpdateTraditional(NewKey);

    [Benchmark]
    public void Update_Entry_Exists() => UpdateEntry(ExistingKey);

    [Benchmark]
    public void Update_Entry_NotExists() => UpdateEntry(NewKey);
}

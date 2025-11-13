using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class InsertOrUpdateBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void InsertOrUpdateTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            value *= 2;
            _dictionary[key] = value;
        }
        else
        {
            _dictionary[key] = 1;
        }
    }

    private void InsertOrUpdateEntry(string key)
    {
        _dictionary.Entry(key).AndModify(x => x * 2).OrInsert(1);
    }

    [Benchmark(Baseline = true)]
    public void InsertOrUpdate_Traditional_Exists() => InsertOrUpdateTraditional(ExistingKey);

    [Benchmark]
    public void InsertOrUpdate_Traditional_NotExists() => InsertOrUpdateTraditional(NewKey);

    [Benchmark]
    public void InsertOrUpdate_Entry_Exists() => InsertOrUpdateEntry(ExistingKey);

    [Benchmark]
    public void InsertOrUpdate_Entry_NotExists() => InsertOrUpdateEntry(NewKey);
}

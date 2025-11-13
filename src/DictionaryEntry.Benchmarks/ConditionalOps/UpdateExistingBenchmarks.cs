using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class UpdateExistingBenchmarks : BenchmarkBase
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
        if (_dictionary.TryGetValue(key, out var value))
        {
            value *= 2;
            _dictionary[key] = value;
        }
    }

    private void UpdateEntry(string key)
    {
        _dictionary.Entry(key).AndModify(x => x * 2);
    }

    [Benchmark(Baseline = true)]
    public void Update_Traditional_Exists() => UpdateTraditional(ExistingKey);

    [Benchmark]
    public void Update_Traditional_NotExists() => UpdateTraditional(NewKey);

    [Benchmark]
    public void Update_Entry_Exists() => UpdateEntry(ExistingKey);

    [Benchmark]
    public void Update_Entry_NotExists() => UpdateEntry(NewKey);
}

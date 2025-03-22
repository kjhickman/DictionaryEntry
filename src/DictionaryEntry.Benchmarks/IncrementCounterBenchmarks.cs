using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("IncrementCounter")]
public class IncrementCounterBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void IncrementCounterTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var count))
        {
            _dictionary[key] = count + 1;
        }
        else
        {
            _dictionary[key] = 1;
        }
    }

    private void IncrementCounterEntry(string key)
    {
        _dictionary.Entry(key).AndModify(count => count + 1).OrInsert(1);
    }

    private void IncrementByAmountTraditional(string key, int amount)
    {
        if (_dictionary.TryGetValue(key, out var count))
        {
            _dictionary[key] = count + amount;
        }
        else
        {
            _dictionary[key] = amount;
        }
    }

    private void IncrementByAmountEntry(string key, int amount)
    {
        _dictionary.Entry(key).AndModify(count => count + amount).OrInsert(amount);
    }

    [Benchmark(Baseline = true)]
    public void IncrementCounter_Traditional_Exists() => IncrementCounterTraditional(ExistingKey);

    [Benchmark]
    public void IncrementCounter_Traditional_NotExists() => IncrementCounterTraditional(NewKey);

    [Benchmark]
    public void IncrementCounter_Entry_Exists() => IncrementCounterEntry(ExistingKey);

    [Benchmark]
    public void IncrementCounter_Entry_NotExists() => IncrementCounterEntry(NewKey);

    [Benchmark]
    public void IncrementByAmount_Traditional_Exists() => IncrementByAmountTraditional(ExistingKey, 5);

    [Benchmark]
    public void IncrementByAmount_Traditional_NotExists() => IncrementByAmountTraditional(NewKey, 5);

    [Benchmark]
    public void IncrementByAmount_Entry_Exists() => IncrementByAmountEntry(ExistingKey, 5);

    [Benchmark]
    public void IncrementByAmount_Entry_NotExists() => IncrementByAmountEntry(NewKey, 5);
}

using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("BatchOperations")]
public class BatchOperationsBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void BatchOperationsTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            value *= 2;
            value = Math.Max(0, value);
            value += 5;
            value = Math.Min(100, value);
            _dictionary[key] = value;
        }
        else
        {
            _dictionary[key] = 5;
        }
    }

    private void BatchOperationsEntry(string key)
    {
        _dictionary.Entry(key).Match(
            occupied =>
            {
                var value = occupied.Value();
                value *= 2;
                value = Math.Max(0, value);
                value += 5;
                value = Math.Min(100, value);
                occupied.Insert(value);
            },
            vacant => vacant.Insert(5)
        );
    }

    [Benchmark(Baseline = true)]
    public void BatchOperations_Traditional_Exists() => BatchOperationsTraditional(ExistingKey);

    [Benchmark]
    public void BatchOperations_Entry_Exists() => BatchOperationsEntry(ExistingKey);

    [Benchmark]
    public void BatchOperations_Traditional_NotExists() => BatchOperationsTraditional(NewKey);

    [Benchmark]
    public void BatchOperations_Entry_NotExists() => BatchOperationsEntry(NewKey);
}

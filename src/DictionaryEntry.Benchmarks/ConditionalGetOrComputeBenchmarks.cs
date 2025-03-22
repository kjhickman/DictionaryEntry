using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("ConditionalGetOrCompute")]
public class ConditionalGetOrCompute
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private int ComputeValue()
    {
        return DateTime.UtcNow.Millisecond % 100;
    }

    private int ComputeValueFromString(string key)
    {
        return key.Length;
    }

    private int GetOrComputeTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var val))
        {
            return val;
        }

        val = ComputeValue();
        _dictionary[key] = val;
        return val;
    }

    private int GetOrComputeTraditionalUsingKey(string key)
    {
        if (_dictionary.TryGetValue(key, out var val))
        {
            return val;
        }

        val = ComputeValueFromString(key);
        _dictionary[key] = val;
        return val;
    }

    private int GetOrComputeEntry(string key)
    {
        return _dictionary.Entry(key).OrInsertWith(ComputeValue);
    }

    private int GetOrComputeEntryUsingKey(string key)
    {
        return _dictionary.Entry(key).OrInsertWith(() => ComputeValueFromString(key));
    }

    [Benchmark(Baseline = true)]
    public int GetOrCompute_Traditional_Exists() => GetOrComputeTraditional(ExistingKey);

    [Benchmark]
    public int GetOrCompute_Traditional_NotExists() => GetOrComputeTraditional(NewKey);

    [Benchmark]
    public int GetOrCompute_Entry_Exists() => GetOrComputeEntry(ExistingKey);

    [Benchmark]
    public int GetOrCompute_Entry_NotExists() => GetOrComputeEntry(NewKey);

    [Benchmark]
    public int GetOrCompute_Extra_Traditional_Exists() => GetOrComputeTraditionalUsingKey(ExistingKey);

    [Benchmark]
    public int GetOrCompute_Extra_Traditional_NotExists() => GetOrComputeTraditionalUsingKey(NewKey);

    [Benchmark]
    public int GetOrCompute_Extra_Entry_Exists() => GetOrComputeEntryUsingKey(ExistingKey);

    [Benchmark]
    public int GetOrCompute_Extra_Entry_NotExists() => GetOrComputeEntryUsingKey(NewKey);
}

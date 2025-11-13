using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class ConditionalGetOrComputeBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private static int ComputeValue()
    {
        return DateTime.UtcNow.Millisecond % 100;
    }

    private static int ComputeValueFromString(string key)
    {
        return key.Length;
    }

    private int GetOrComputeTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        value = ComputeValue();
        _dictionary[key] = value;
        return value;
    }

    private int GetOrComputeTraditionalUsingKey(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        value = ComputeValueFromString(key);
        _dictionary[key] = value;
        return value;
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

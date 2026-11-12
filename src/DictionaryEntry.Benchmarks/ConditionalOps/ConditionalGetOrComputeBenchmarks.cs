using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class ConditionalGetOrComputeBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;

    [GlobalSetup]
    public void Setup()
    {
        _existingDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _keys = new string[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            var key = $"key_{i}";
            _keys[i] = key;
            _existingDictionary[key] = 10;
        }
    }

    private static int ComputeValue()
    {
        return DateTime.UtcNow.Millisecond % 100;
    }

    private static int ComputeValueFromString(string key)
    {
        return key.Length;
    }

    private static int GetOrComputeTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }

        value = ComputeValue();
        dict[key] = value;
        return value;
    }

    private static int GetOrComputeTraditionalUsingKey(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }

        value = ComputeValueFromString(key);
        dict[key] = value;
        return value;
    }

    private static int GetOrComputeEntry(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key).OrInsertWith(ComputeValue);
    }

    private static int GetOrComputeEntryUsingKey(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key).OrInsertWithKey(ComputeValueFromString);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeTraditional(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeTraditional(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Entry_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeEntry(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeEntry(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Extra_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeTraditionalUsingKey(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Extra_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeTraditionalUsingKey(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Extra_Entry_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeEntryUsingKey(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrCompute_Extra_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrComputeEntryUsingKey(_emptyDictionary, _keys[i]);
        }

        return sum;
    }
}

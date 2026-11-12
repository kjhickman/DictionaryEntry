using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.AdvancedOps;

[BenchmarkCategory("AdvancedOps")]
public class BatchOperationsBenchmarks : BenchmarkBase
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

    private static void BatchOperationsTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            value *= 2;
            value = Math.Max(0, value);
            value += 5;
            value = Math.Min(100, value);
            dict[key] = value;
        }
        else
        {
            dict[key] = 5;
        }
    }

    private static void BatchOperationsEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).Match(
            occupied =>
            {
                var value = occupied.Value;
                value *= 2;
                value = Math.Max(0, value);
                value += 5;
                value = Math.Min(100, value);
                occupied.Insert(value);
            },
            vacant => vacant.Insert(5));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void BatchOperations_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            BatchOperationsTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void BatchOperations_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            BatchOperationsTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void BatchOperations_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            BatchOperationsEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void BatchOperations_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            BatchOperationsEntry(_emptyDictionary, _keys[i]);
        }
    }
}

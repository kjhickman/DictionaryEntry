using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class UpdateExistingBenchmarks : BenchmarkBase
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

    private static void UpdateTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            value *= 2;
            dict[key] = value;
        }
    }

    private static void UpdateEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).AndModify(x => x * 2);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void Update_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpdateTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Update_Traditional_NotExists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpdateTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Update_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpdateEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Update_Entry_NotExists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpdateEntry(_emptyDictionary, _keys[i]);
        }
    }
}

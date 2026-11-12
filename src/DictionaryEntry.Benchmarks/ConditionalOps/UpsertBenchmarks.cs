using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class UpsertBenchmarks : BenchmarkBase
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

    private static void UpsertTraditional(Dictionary<string, int> dict, string key)
    {
        dict[key] = 15;
    }

    private static void UpsertEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).InsertEntry(15);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void Upsert_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Upsert_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Upsert_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Upsert_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertEntry(_emptyDictionary, _keys[i]);
        }
    }
}

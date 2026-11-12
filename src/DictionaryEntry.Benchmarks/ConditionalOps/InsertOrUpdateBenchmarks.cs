using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class InsertOrUpdateBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;

    [GlobalSetup]
    public void GlobalSetup()
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

    [IterationSetup]
    public void IterationSetup()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[_keys[i]] = 10;
        }
    }

    private static void InsertOrUpdateTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            value += 3;
            dict[key] = value;
        }
        else
        {
            dict[key] = 1;
        }
    }

    private static void InsertOrUpdateEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).AndModify(x => x + 3).OrInsert(1);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateEntry(_emptyDictionary, _keys[i]);
        }
    }
}
